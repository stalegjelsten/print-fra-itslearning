using System.Drawing;
using System.Windows.Forms;
using PrintFraItslearning.Printing;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning.Forms;

public sealed class ProgressForm : Form
{
    private readonly PrintJob _job;
    private readonly Label _currentLabel;
    private readonly ProgressBar _progress;
    private readonly ListBox _log;
    private readonly Button _actionButton;
    private readonly Label _summaryLabel;
    private readonly Label _resultSummaryLabel;

    private CancellationTokenSource? _cts;
    private bool _printingDone;
    private readonly List<PrintResultRow> _results = new();

    public ProgressForm(PrintJob job)
    {
        _job = job;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AppIcon.Apply(this);

        Text = "Skriver ut…";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(680, 560);
        MinimumSize = new Size(540, 420);
        FormClosing += ProgressForm_FormClosing;

        _summaryLabel = new Label
        {
            Text = $"Skriver ut {_job.Files.Count} filer til {_job.Printer}",
            Location = new Point(16, 12),
            Size = new Size(648, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font(Font, FontStyle.Bold)
        };
        Controls.Add(_summaryLabel);

        _currentLabel = new Label
        {
            Text = "",
            Location = new Point(16, 40),
            Size = new Size(648, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(_currentLabel);

        _progress = new ProgressBar
        {
            Location = new Point(16, 68),
            Size = new Size(648, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = 0,
            Maximum = Math.Max(1, _job.Files.Count)
        };
        Controls.Add(_progress);

        var logLabel = new Label
        {
            Text = "Logg:",
            Location = new Point(16, 132),
            AutoSize = true
        };
        Controls.Add(logLabel);

        _resultSummaryLabel = new Label
        {
            Text = "",
            Location = new Point(16, 104),
            Size = new Size(648, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = SystemColors.GrayText
        };
        Controls.Add(_resultSummaryLabel);

        _log = new ListBox
        {
            Location = new Point(16, 156),
            Size = new Size(648, 346),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            IntegralHeight = false,
            HorizontalScrollbar = true,
            Font = new Font("Consolas", 9F)
        };
        Controls.Add(_log);

        _actionButton = new Button
        {
            Text = "Avbryt utskrift",
            Location = new Point(524, 516),
            Size = new Size(140, 32),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _actionButton.Click += ActionButton_Click;
        Controls.Add(_actionButton);

        ResumeLayout(performLayout: true);

        Shown += async (_, _) => await RunAsync();
    }

    private void ActionButton_Click(object? sender, EventArgs e)
    {
        if (!_printingDone)
        {
            _cts?.Cancel();
            _actionButton.Enabled = false;
            _actionButton.Text = "Avbryter…";
        }
        else
        {
            Close();
        }
    }

    private void ProgressForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_printingDone)
        {
            var r = MessageBox.Show(this,
                "Utskriften pågår. Avbryt og lukk?",
                "Bekreft", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            _cts?.Cancel();
        }
    }

    private async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        if (_job.CombineToOnePdf)
            await Task.Run(() => RunCombined(token));
        else
            await Task.Run(() => PrintAllFiles(token));

        _printingDone = true;
        _actionButton.Text = "Lukk";
        _actionButton.Enabled = true;

        CleanupTempFiles();
        ShowFinalSummary();
    }

    private void RunCombined(CancellationToken ct)
    {
        var builder = new CombinedPdfBuilder(
            _job,
            p =>
            {
                if (IsDisposed) return;
                BeginInvoke(new Action(() =>
                {
                    _currentLabel.Text = $"Fil {p.Counter} av {p.Total}:  {p.CurrentFile}";
                    _progress.Value = Math.Min(p.Counter, _progress.Maximum);
                }));
            },
            AppendLog,
            () => ct.IsCancellationRequested);

        CombinedBuildResult result;
        try
        {
            result = builder.Build();
        }
        catch (Exception ex)
        {
            AppendLog($"FEIL ved kombinering: {ex.Message}");
            return;
        }

        _results.AddRange(result.Results);

        if (ct.IsCancellationRequested)
        {
            AppendLog("AVBRUTT av bruker — kombinert PDF blir ikke distribuert.");
            try { File.Delete(result.OutputPdfPath); } catch { }
            return;
        }

        try
        {
            if (_job.SaveCombinedPdf && !string.IsNullOrWhiteSpace(_job.SaveCombinedPdfPath))
            {
                UpdateCurrent(_job.Files.Count, "Lagrer PDF…");
                try
                {
                    File.Copy(result.OutputPdfPath, _job.SaveCombinedPdfPath, overwrite: true);
                    AppendLog($"OK: kombinert PDF lagret til {_job.SaveCombinedPdfPath}");
                }
                catch (Exception ex)
                {
                    AppendLog($"FEIL ved lagring av PDF: {ex.Message}");
                }
            }

            if (_job.PrintCombinedPdf)
            {
                UpdateCurrent(_job.Files.Count, "Sender til skriver…");
                AppendLog($"Sender kombinert PDF til {_job.Printer}…");
                try
                {
                    PdfPrinter.Print(result.OutputPdfPath, _job.Printer);
                    AppendLog("OK: kombinert PDF sendt til printerkøen.");
                }
                catch (Exception ex)
                {
                    AppendLog($"FEIL ved utskrift av kombinert PDF: {ex.Message}");
                }
            }
        }
        finally
        {
            try { File.Delete(result.OutputPdfPath); } catch { }
        }
    }

    private void PrintAllFiles(CancellationToken ct)
    {
        var needsWord = _job.Files.Any(f => f.Kind == FileKind.Word || f.Kind == FileKind.Html);
        var needsExcel = _job.Files.Any(f => f.Kind == FileKind.Excel);

        WordPrinter? word = null;
        ExcelPrinter? excel = null;
        try
        {
            if (needsWord)
            {
                if (!WordPrinter.IsWordAvailable())
                    AppendLog("ADVARSEL: Microsoft Word ikke installert — Word/HTML-filer hoppes over.");
                else
                    word = new WordPrinter(_job.Printer, _job.Config.MarginCm);
            }

            if (needsExcel)
            {
                if (!ExcelPrinter.IsExcelAvailable())
                    AppendLog("ADVARSEL: Microsoft Excel ikke installert — Excel-filer hoppes over.");
                else
                    excel = new ExcelPrinter(_job.Config.MarginCm);
            }

            int counter = 0;
            foreach (var file in _job.Files)
            {
                if (ct.IsCancellationRequested)
                {
                    AppendLog("AVBRUTT av bruker.");
                    break;
                }

                counter++;
                var folderName = FolderNameFor(file);
                UpdateCurrent(counter, file.Name);

                try
                {
                    switch (file.Kind)
                    {
                        case FileKind.Word:
                            if (word == null)
                            {
                                Record(file, folderName, false, "Word ikke tilgjengelig");
                                AppendLog($"HOPPET OVER ({counter}/{_job.Files.Count}): {file.Name}");
                            }
                            else
                            {
                                word.Print(file.FullName, folderName, _job.AddHeaderFooter, _job.PrintWordComments);
                                Record(file, folderName, true, "Word");
                                AppendLog($"OK ({counter}/{_job.Files.Count}): {file.Name}  [{folderName}]");
                            }
                            break;

                        case FileKind.Html:
                            if (word == null)
                            {
                                Record(file, folderName, false, "Word ikke tilgjengelig");
                                AppendLog($"HOPPET OVER ({counter}/{_job.Files.Count}): {file.Name}");
                            }
                            else
                            {
                                word.Print(file.FullName, folderName, _job.AddHeaderFooter, printWithComments: false);
                                Record(file, folderName, true, "HTML");
                                AppendLog($"OK ({counter}/{_job.Files.Count}): {file.Name}  [{folderName}]");
                            }
                            break;

                        case FileKind.Excel:
                            if (excel == null)
                            {
                                Record(file, folderName, false, "Excel ikke tilgjengelig");
                                AppendLog($"HOPPET OVER ({counter}/{_job.Files.Count}): {file.Name}");
                            }
                            else
                            {
                                PrintExcelFile(excel, file, folderName, counter);
                            }
                            break;

                        case FileKind.Pdf:
                            PrintPdfFile(file, folderName, counter);
                            break;
                    }
                }
                catch (IOException ioex) when (IsLockedMessage(ioex.Message))
                {
                    Record(file, folderName, false, "Filen er åpen i annet program");
                    AppendLog($"FEIL ({counter}/{_job.Files.Count}): {file.Name}  – åpen i annet program");
                }
                catch (Exception ex)
                {
                    Record(file, folderName, false, ex.Message);
                    AppendLog($"FEIL ({counter}/{_job.Files.Count}): {file.Name}  – {ex.Message}");
                }

                // La printerkøen tømmes mellom dokumenter (kort polling)
                try
                {
                    PrintQueueWatcher.WaitForQueueDrained(_job.Printer, maxWaitMs: 30_000, pollMs: 400, ct: ct);
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }
        finally
        {
            word?.Dispose();
            excel?.Dispose();
        }
    }

    private void PrintExcelFile(ExcelPrinter excel, ScannedFile file, string folderName, int counter)
    {
        string? exportedPdf = null;
        try
        {
            exportedPdf = excel.ExportToPdf(file.FullName, folderName,
                _job.AddHeaderFooter, _job.PrintExcelFormulas);
            PdfPrinter.Print(exportedPdf, _job.Printer);
            Record(file, folderName, true, _job.PrintExcelFormulas ? "Excel (normal+formel)" : "Excel");
            AppendLog($"OK ({counter}/{_job.Files.Count}): {file.Name}  [{folderName}]");
        }
        finally
        {
            if (exportedPdf != null)
            {
                try { File.Delete(exportedPdf); } catch { }
            }
        }
    }

    private void PrintPdfFile(ScannedFile file, string folderName, int counter)
    {
        string? stamped = null;
        try
        {
            string toPrint = file.FullName;
            if (_job.AddHeaderFooter)
            {
                try
                {
                    stamped = PdfStamper.StampHeaderFooter(file.FullName, folderName);
                    toPrint = stamped;
                }
                catch (Exception ex)
                {
                    AppendLog($"  Advarsel: kunne ikke stample PDF — skriver ut uten topptekst ({ex.Message})");
                }
            }

            PdfPrinter.Print(toPrint, _job.Printer);
            Record(file, folderName, true, "PDF");
            AppendLog($"OK ({counter}/{_job.Files.Count}): {file.Name}  [{folderName}]");
        }
        finally
        {
            if (stamped != null)
            {
                try { File.Delete(stamped); } catch { }
            }
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

    private void Record(ScannedFile file, string folderName, bool success, string detail) =>
        _results.Add(new PrintResultRow(file.Name, folderName, success, detail));

    private static bool IsLockedMessage(string msg) =>
        msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("låst", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("being used", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("annen bruker", StringComparison.OrdinalIgnoreCase);

    private void UpdateCurrent(int counter, string name)
    {
        if (IsDisposed) return;
        BeginInvoke(new Action(() =>
        {
            _currentLabel.Text = $"Fil {counter} av {_job.Files.Count}:  {name}";
            _progress.Value = Math.Min(counter, _progress.Maximum);
        }));
    }

    private void AppendLog(string text)
    {
        if (IsDisposed) return;
        BeginInvoke(new Action(() =>
        {
            _log.Items.Add(text);
            _log.TopIndex = _log.Items.Count - 1;
        }));
    }

    private void ShowFinalSummary()
    {
        var ok = _results.Count(r => r.Success);
        var skipped = _results.Count(r => !r.Success && IsSkipped(r));
        var failed = _results.Count - ok - skipped;
        var msg = failed == 0 && skipped == 0
            ? $"Ferdig: {ok} av {_job.Files.Count} skrevet ut."
            : failed == 0
                ? $"Ferdig: {ok} av {_job.Files.Count} skrevet ut, {skipped} hoppet over."
            : $"Ferdig med feil: {ok} av {_job.Files.Count} skrevet ut, {failed} feilet.";
        _summaryLabel.Text = msg;
        _summaryLabel.ForeColor = failed == 0 ? Color.DarkGreen : Color.DarkRed;
        _resultSummaryLabel.Text = $"OK: {ok}    Hoppet over: {skipped}    Feilet: {failed}";
        _resultSummaryLabel.ForeColor = failed == 0 ? Color.DarkGreen : Color.DarkRed;
        _currentLabel.Text = "";

        if (failed > 0 || skipped > 0)
        {
            AppendLog("");
            AppendLog("Filer som ikke ble skrevet ut:");
            foreach (var r in _results.Where(r => !r.Success))
                AppendLog($"  - {r.Name} [{r.Folder}]: {r.Detail}");
        }
    }

    private static bool IsSkipped(PrintResultRow row) =>
        row.Detail.Contains("ikke tilgjengelig", StringComparison.OrdinalIgnoreCase);

    private void CleanupTempFiles()
    {
        foreach (var html in _job.GeneratedHtmlFiles)
        {
            try { if (html.Exists) html.Delete(); } catch { }
        }
    }
}
