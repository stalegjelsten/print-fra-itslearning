using System.Drawing.Printing;
using System.IO;
using PdfiumViewer;

namespace PrintFraItslearning.Printing;

public static class PdfPrinter
{
    public static void Print(string pdfPath, string printerName)
    {
        PdfiumLoader.EnsureLoaded();
        using var document = PdfDocument.Load(pdfPath);
        using var printDoc = document.CreatePrintDocument();
        printDoc.PrinterSettings = new PrinterSettings
        {
            PrinterName = printerName,
            Copies = 1
        };
        printDoc.PrintController = new StandardPrintController();
        printDoc.DocumentName = Path.GetFileNameWithoutExtension(pdfPath);
        printDoc.Print();
    }
}
