using System.Runtime.InteropServices;

namespace PrintFraItslearning.Printing;

// Excel-utskrift går gjennom PDF: vi eksporterer normal- og evt. formelvisning
// til to PDF-er og kombinerer dem til én. Det gir én utskriftsjobb per elev,
// slik at duplex/tosidig utskrift blir korrekt.
public sealed class ExcelPrinter : IDisposable
{
    // Excel-konstanter
    private const int XlPortrait = 1;
    private const int XlLandscape = 2;
    private const int XlPaperA4 = 9;
    private const int XlSheetVisible = -1;
    private const int XlTypePDF = 0;
    private const int XlQualityStandard = 0;
    private const int MsoAutomationSecurityForceDisable = 3;

    private dynamic? _excelApp;
    private readonly double _marginCm;

    public ExcelPrinter(double marginCm)
    {
        _marginCm = marginCm;
    }

    public static bool IsExcelAvailable()
    {
        var type = Type.GetTypeFromProgID("Excel.Application");
        return type != null;
    }

    private dynamic GetOrCreateApp()
    {
        if (_excelApp != null) return _excelApp;
        var type = Type.GetTypeFromProgID("Excel.Application")
            ?? throw new InvalidOperationException("Microsoft Excel er ikke installert.");
        _excelApp = Activator.CreateInstance(type)!;
        _excelApp!.Visible = false;
        _excelApp.DisplayAlerts = false;
        _excelApp.ScreenUpdating = false;
        try { _excelApp.AskToUpdateLinks = false; } catch { }
        try { _excelApp.AutomationSecurity = MsoAutomationSecurityForceDisable; } catch { }
        return _excelApp;
    }

    public string ExportToPdf(string filePath, string folderName, bool addHeaderFooter, bool includeFormulaView)
    {
        var app = GetOrCreateApp();
        EnsureFileNotLocked(filePath);

        dynamic? workbook = null;
        var tempFiles = new List<string>();
        try
        {
            workbook = app.Workbooks.Open(filePath, /*UpdateLinks*/ 0, /*ReadOnly*/ true,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                /*AddToMru*/ false);
            ConfigurePageSetup(workbook, folderName, addHeaderFooter);

            var normalPdf = TempPdfPath("xl_normal");
            tempFiles.Add(normalPdf);
            ExportWorkbookToPdf(workbook, normalPdf);

            if (!includeFormulaView)
                return normalPdf;

            ToggleFormulaView(workbook, show: true);
            // Når formler vises blir kolonnene ofte for smale — gjør autofit
            AutofitColumns(workbook);
            // PageSetup må re-kalles fordi DisplayFormulas påvirker bredden på sidene
            ConfigurePageSetup(workbook, folderName, addHeaderFooter);

            var formulaPdf = TempPdfPath("xl_formula");
            tempFiles.Add(formulaPdf);
            ExportWorkbookToPdf(workbook, formulaPdf);

            ToggleFormulaView(workbook, show: false);

            var combined = TempPdfPath("xl_combined");
            PdfMerger.Combine(new[] { normalPdf, formulaPdf }, combined);

            // Rydd opp delfilene — vi beholder bare den kombinerte
            foreach (var tmp in tempFiles)
            {
                try { File.Delete(tmp); } catch { }
            }

            return combined;
        }
        finally
        {
            if (workbook != null)
            {
                try { workbook.Close(SaveChanges: false); } catch { }
                try { Marshal.FinalReleaseComObject(workbook); } catch { }
            }
        }
    }

    private void ConfigurePageSetup(dynamic workbook, string folderName, bool addHeaderFooter)
    {
        double marginPoints = _marginCm * 28.35;

        foreach (dynamic sheet in workbook.Worksheets)
        {
            try
            {
                if (sheet.Visible != XlSheetVisible) continue;
            }
            catch { }

            try
            {
                dynamic ps = sheet.PageSetup;

                ps.Orientation = XlLandscape;
                try { ps.PaperSize = XlPaperA4; } catch { }

                ps.TopMargin = marginPoints;
                ps.BottomMargin = marginPoints;
                ps.LeftMargin = marginPoints;
                ps.RightMargin = marginPoints;
                ps.HeaderMargin = marginPoints / 2;
                ps.FooterMargin = marginPoints / 2;

                // Rad/kolonneoverskrifter (A,B,C... og 1,2,3...)
                ps.PrintHeadings = true;
                // Rutenett
                ps.PrintGridlines = true;

                // Tilpass i bredde, la høyden være automatisk
                try
                {
                    ps.Zoom = false;
                    ps.FitToPagesWide = 1;
                    ps.FitToPagesTall = false;
                }
                catch { }

                if (addHeaderFooter)
                {
                    // &"font,style" + &10 (10pt) + tekst. && = literal &.
                    ps.CenterHeader = "&\"Arial,Bold\"&10" + EscapeHeader(folderName);
                    ps.LeftHeader = "";
                    ps.RightHeader = "";
                    ps.CenterFooter = "&\"Arial,Regular\"&10Side &P av &N";
                    ps.LeftFooter = "";
                    ps.RightFooter = "";
                }
            }
            catch
            {
                // Beskyttet ark eller rare egenskaper — hopp videre
            }
            finally
            {
                try { Marshal.FinalReleaseComObject(sheet); } catch { }
            }
        }
    }

    private static void AutofitColumns(dynamic workbook)
    {
        foreach (dynamic sheet in workbook.Worksheets)
        {
            try
            {
                if (sheet.Visible != XlSheetVisible) continue;
            }
            catch { }

            try
            {
                sheet.Cells.EntireColumn.AutoFit();
            }
            catch { }
            finally
            {
                try { Marshal.FinalReleaseComObject(sheet); } catch { }
            }
        }
    }

    private static void ToggleFormulaView(dynamic workbook, bool show)
    {
        try
        {
            foreach (dynamic win in workbook.Windows)
            {
                try { win.DisplayFormulas = show; } catch { }
                try { Marshal.FinalReleaseComObject(win); } catch { }
            }
        }
        catch { }
    }

    private static void ExportWorkbookToPdf(dynamic workbook, string outPath)
    {
        // ExportAsFixedFormat(Type, Filename, Quality, IncludeDocProperties,
        //                    IgnorePrintAreas, From, To, OpenAfterPublish)
        workbook.ExportAsFixedFormat(
            XlTypePDF,
            outPath,
            XlQualityStandard,
            /*IncludeDocProperties*/ false,
            /*IgnorePrintAreas*/ false,
            Type.Missing,
            Type.Missing,
            /*OpenAfterPublish*/ false);
    }

    private static string TempPdfPath(string tag) =>
        AppTemp.FilePath(tag, ".pdf");

    private static string EscapeHeader(string text) => text.Replace("&", "&&");

    private static void EnsureFileNotLocked(string path)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public void Dispose()
    {
        if (_excelApp != null)
        {
            try { _excelApp.DisplayAlerts = false; } catch { }
            try { _excelApp.Quit(); } catch { }
            try { Marshal.FinalReleaseComObject(_excelApp); } catch { }
            _excelApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
