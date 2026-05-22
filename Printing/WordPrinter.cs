using System.Runtime.InteropServices;

namespace PrintFraItslearning.Printing;

public sealed class WordPrinter : IDisposable
{
    // Word-konstanter
    private const int WdPrintDocumentContent = 0;
    private const int WdPrintMarkup = 7;
    private const int WdFieldPage = 33;
    private const int WdFieldNumPages = 26;
    private const int WdAlignParagraphCenter = 1;
    private const int WdExportFormatPDF = 17;
    private const int WdExportOptimizeForPrint = 0;
    private const int WdExportAllDocument = 0;
    private const int WdExportDocumentContent = 0;
    private const int WdExportDocumentWithMarkup = 7;
    private const int WdExportCreateNoBookmarks = 0;
    private const int MsoAutomationSecurityForceDisable = 3;
    private const int WdAlertsNone = 0;

    private dynamic? _wordApp;
    private readonly string _printerName;
    private readonly double _marginCm;

    public WordPrinter(string printerName, double marginCm)
    {
        _printerName = printerName;
        _marginCm = marginCm;
    }

    public static bool IsWordAvailable()
    {
        var type = Type.GetTypeFromProgID("Word.Application");
        return type != null;
    }

    private dynamic GetOrCreateApp()
    {
        if (_wordApp != null) return _wordApp;
        var type = Type.GetTypeFromProgID("Word.Application")
            ?? throw new InvalidOperationException("Microsoft Word er ikke installert.");
        _wordApp = Activator.CreateInstance(type)!;
        _wordApp!.Visible = false;
        try { _wordApp.DisplayAlerts = WdAlertsNone; } catch { }
        try { _wordApp.AutomationSecurity = MsoAutomationSecurityForceDisable; } catch { }
        try { _wordApp.ActivePrinter = _printerName; } catch { }
        return _wordApp;
    }

    public string ExportToPdf(string filePath, string folderName, bool addHeaderFooter, bool printWithComments)
    {
        var app = GetOrCreateApp();
        EnsureFileNotLocked(filePath);

        var pdfPath = AppTemp.FilePath("word", ".pdf");

        dynamic? doc = null;
        try
        {
            doc = app.Documents.Open(filePath, ReadOnly: true, AddToRecentFiles: false);
            if (addHeaderFooter)
                ApplyHeaderFooter(doc, folderName);

            int item = printWithComments ? WdExportDocumentWithMarkup : WdExportDocumentContent;
            // ExportAsFixedFormat(OutputFileName, ExportFormat, OpenAfterExport, OptimizeFor,
            //                    Range, From, To, Item, IncludeDocProps, KeepIRM, CreateBookmarks,
            //                    DocStructureTags, BitmapMissingFonts, UseISO19005_1)
            doc.ExportAsFixedFormat(
                pdfPath,
                WdExportFormatPDF,
                false,
                WdExportOptimizeForPrint,
                WdExportAllDocument,
                1, 1,
                item,
                false,
                true,
                WdExportCreateNoBookmarks,
                true,
                true,
                false);

            return pdfPath;
        }
        finally
        {
            if (doc != null)
            {
                try { doc.Close(false); } catch { }
                try { Marshal.FinalReleaseComObject(doc); } catch { }
            }
        }
    }

    public void Print(string filePath, string folderName, bool addHeaderFooter, bool printWithComments)
    {
        var app = GetOrCreateApp();

        // Word's Open kan låse seg om filen er åpen i annet program — sjekk først.
        EnsureFileNotLocked(filePath);

        dynamic? doc = null;
        try
        {
            doc = app.Documents.Open(filePath, ReadOnly: true, AddToRecentFiles: false);

            if (addHeaderFooter)
                ApplyHeaderFooter(doc, folderName);

            // PrintOut med posisjonelle parametre via late binding
            // (Background=false, Append=false, Range=0, OutputFileName="", From="", To="", Item=item, Copies=1)
            int item = printWithComments ? WdPrintMarkup : WdPrintDocumentContent;
            doc.PrintOut(false, false, 0, "", "", "", item, 1);
        }
        finally
        {
            if (doc != null)
            {
                try { doc.Close(false); } catch { }
                try { Marshal.FinalReleaseComObject(doc); } catch { }
            }
        }
    }

    private void ApplyHeaderFooter(dynamic doc, string folderName)
    {
        double marginPoints = _marginCm * 28.35;
        foreach (dynamic section in doc.Sections)
        {
            section.PageSetup.TopMargin = marginPoints;
            section.PageSetup.BottomMargin = marginPoints;
            section.PageSetup.LeftMargin = marginPoints;
            section.PageSetup.RightMargin = marginPoints;

            dynamic header = section.Headers[1];
            string existingText = ((string)header.Range.Text).Trim();
            if (existingText.Length > 0)
            {
                header.Range.InsertBefore(folderName + "\n");
                dynamic firstPar = header.Range.Paragraphs[1].Range;
                firstPar.Font.Size = 10;
                firstPar.Font.Bold = true;
                firstPar.ParagraphFormat.Alignment = WdAlignParagraphCenter;
            }
            else
            {
                header.Range.Text = folderName;
                header.Range.Font.Size = 10;
                header.Range.Font.Bold = true;
                header.Range.ParagraphFormat.Alignment = WdAlignParagraphCenter;
            }

            dynamic footer = section.Footers[1];
            footer.Range.Text = "";
            footer.Range.ParagraphFormat.Alignment = WdAlignParagraphCenter;
            footer.Range.Font.Size = 10;
            footer.Range.InsertBefore("Side ");

            dynamic tmp = footer.Range.Duplicate;
            tmp.Collapse(0);
            tmp.Fields.Add(tmp, WdFieldPage, "", false);

            tmp = footer.Range.Duplicate;
            tmp.Collapse(0);
            tmp.InsertAfter(" av ");

            tmp = footer.Range.Duplicate;
            tmp.Collapse(0);
            tmp.Fields.Add(tmp, WdFieldNumPages, "", false);

            footer.Range.Fields.Update();
        }
    }

    private static void EnsureFileNotLocked(string path)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    public void Dispose()
    {
        if (_wordApp != null)
        {
            try { _wordApp.Quit(); } catch { }
            try { Marshal.FinalReleaseComObject(_wordApp); } catch { }
            _wordApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
