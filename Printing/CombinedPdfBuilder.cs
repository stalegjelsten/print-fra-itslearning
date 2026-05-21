using PdfSharp.Pdf;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning.Printing;

public sealed record CombinedBuildProgress(int Counter, int Total, string CurrentFile, string FolderName);

public sealed record CombinedBuildResult(string OutputPdfPath, List<PrintResultRow> Results);

// Bygger én samlet PDF for hele utskriftsjobben:
//   - Word og HTML konverteres til PDF via Word.ExportAsFixedFormat
//   - Excel eksporteres til PDF via Excel.ExportAsFixedFormat (allerede landscape +
//     rutenett + overskrifter + evt. formelvisning kombinert)
//   - PDF-filer stemples med topptekst/bunntekst hvis innstillingen er på
//   - Hvis duplex: tom side padding mellom elever (mappenavn) slik at hver elev
//     starter på en odde-side (forside av et nytt ark)
public sealed class CombinedPdfBuilder
{
    private readonly PrintJob _job;
    private readonly Action<CombinedBuildProgress> _onProgress;
    private readonly Action<string> _onLog;
    private readonly Func<bool> _isCancelled;

    public CombinedPdfBuilder(
        PrintJob job,
        Action<CombinedBuildProgress> onProgress,
        Action<string> onLog,
        Func<bool> isCancelled)
    {
        _job = job;
        _onProgress = onProgress;
        _onLog = onLog;
        _isCancelled = isCancelled;
    }

    public CombinedBuildResult Build()
    {
        var results = new List<PrintResultRow>();
        var output = new PdfDocument();
        var tempPdfs = new List<string>();

        // Word/HTML krever Word; Excel krever Excel
        var needsWord = _job.Files.Any(f => f.Kind == FileKind.Word || f.Kind == FileKind.Html);
        var needsExcel = _job.Files.Any(f => f.Kind == FileKind.Excel);

        WordPrinter? word = null;
        ExcelPrinter? excel = null;

        try
        {
            if (needsWord)
            {
                if (!WordPrinter.IsWordAvailable())
                    _onLog("ADVARSEL: Microsoft Word ikke installert — Word/HTML-filer hoppes over.");
                else
                    word = new WordPrinter(_job.Printer, _job.Config.MarginCm);
            }

            if (needsExcel)
            {
                if (!ExcelPrinter.IsExcelAvailable())
                    _onLog("ADVARSEL: Microsoft Excel ikke installert — Excel-filer hoppes over.");
                else
                    excel = new ExcelPrinter(_job.Config.MarginCm);
            }

            // Filer er allerede sortert (Word/HTML → Excel → PDF, innen elev etter navn).
            // Vi grupperer per mappe (=elev) i den rekkefølgen elevene først dukker opp.
            var groups = _job.Files
                .Select((f, idx) => (f, idx))
                .GroupBy(t => t.f.FolderName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Min(x => x.idx))
                .ToList();

            int counter = 0;
            foreach (var group in groups)
            {
                if (_isCancelled()) break;

                var elevPagesBefore = output.PageCount;

                foreach (var (file, _) in group)
                {
                    if (_isCancelled()) break;
                    counter++;
                    var folderName = FolderNameFor(file);
                    _onProgress(new CombinedBuildProgress(counter, _job.Files.Count, file.Name, folderName));

                    string? pdfPath = null;
                    try
                    {
                        pdfPath = ConvertToPdf(file, folderName, word, excel);
                        if (pdfPath != null)
                        {
                            tempPdfs.Add(pdfPath);
                            PdfMerger.ImportPagesInto(output, pdfPath);
                            results.Add(new PrintResultRow(file.Name, folderName, true, KindLabel(file.Kind)));
                            _onLog($"OK ({counter}/{_job.Files.Count}): {file.Name}  [{folderName}]");
                        }
                    }
                    catch (IOException ioex) when (IsLockedMessage(ioex.Message))
                    {
                        results.Add(new PrintResultRow(file.Name, folderName, false, "Filen er åpen i annet program"));
                        _onLog($"FEIL ({counter}/{_job.Files.Count}): {file.Name}  – åpen i annet program");
                    }
                    catch (Exception ex)
                    {
                        results.Add(new PrintResultRow(file.Name, folderName, false, ex.Message));
                        _onLog($"FEIL ({counter}/{_job.Files.Count}): {file.Name}  – {ex.Message}");
                    }
                }

                // Duplex-padding: legg til tom side hvis elevens del har odde antall sider
                if (_job.DuplexPadBlankPages && output.PageCount > elevPagesBefore)
                {
                    var elevPageCount = output.PageCount - elevPagesBefore;
                    if (elevPageCount % 2 == 1)
                    {
                        var last = output.Pages[output.PageCount - 1];
                        var blank = output.AddPage();
                        blank.Width = last.Width;
                        blank.Height = last.Height;
                        blank.Orientation = last.Orientation;
                    }
                }
            }

            var outputPath = Path.Combine(Path.GetTempPath(), $"pfi_combined_{Guid.NewGuid():N}.pdf");
            output.Save(outputPath);
            output.Dispose();

            return new CombinedBuildResult(outputPath, results);
        }
        finally
        {
            word?.Dispose();
            excel?.Dispose();

            // Rydd opp temp-PDF-fragmenter (vi har allerede importert sidene)
            foreach (var p in tempPdfs)
            {
                try { File.Delete(p); } catch { }
            }
        }
    }

    private string? ConvertToPdf(ScannedFile file, string folderName, WordPrinter? word, ExcelPrinter? excel)
    {
        switch (file.Kind)
        {
            case FileKind.Word:
            case FileKind.Html:
                if (word == null)
                    throw new InvalidOperationException("Word ikke tilgjengelig");
                return word.ExportToPdf(file.FullName, folderName, _job.AddHeaderFooter,
                    file.Kind == FileKind.Word && _job.PrintWordComments);

            case FileKind.Excel:
                if (excel == null)
                    throw new InvalidOperationException("Excel ikke tilgjengelig");
                return excel.ExportToPdf(file.FullName, folderName, _job.AddHeaderFooter,
                    _job.PrintExcelFormulas);

            case FileKind.Pdf:
                if (_job.AddHeaderFooter)
                {
                    try
                    {
                        return PdfStamper.StampHeaderFooter(file.FullName, folderName);
                    }
                    catch (Exception ex)
                    {
                        _onLog($"  Advarsel: kunne ikke stample PDF — bruker original ({ex.Message})");
                    }
                }
                // Ingen stempling — kopier PDF til temp slik at den blir ryddet opp likt
                var copy = Path.Combine(Path.GetTempPath(), $"pfi_pdfcopy_{Guid.NewGuid():N}.pdf");
                File.Copy(file.FullName, copy, overwrite: true);
                return copy;

            default:
                return null;
        }
    }

    private string FolderNameFor(ScannedFile file)
    {
        var folderName = file.FolderName;
        if (_job.SortByFirstName)
        {
            var student = StudentName.TryParse(folderName);
            if (student != null)
                folderName = $"{student.AlleFornavnRaw} {student.Etternavn} {student.Epost}";
        }
        return folderName;
    }

    private static string KindLabel(FileKind k) => k switch
    {
        FileKind.Word => "Word",
        FileKind.Html => "HTML",
        FileKind.Excel => "Excel",
        FileKind.Pdf => "PDF",
        _ => k.ToString()
    };

    private static bool IsLockedMessage(string msg) =>
        msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("låst", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("being used", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("annen bruker", StringComparison.OrdinalIgnoreCase);
}
