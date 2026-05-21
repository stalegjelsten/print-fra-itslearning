using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PrintFraItslearning.Printing;

public static class PdfStamper
{
    public static string StampHeaderFooter(string srcPdfPath, string folderName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(),
            $"pfi_{Guid.NewGuid():N}.pdf");

        using (var doc = PdfReader.Open(srcPdfPath, PdfDocumentOpenMode.Modify))
        {
            var totalPages = doc.PageCount;
            for (int i = 0; i < totalPages; i++)
            {
                var page = doc.Pages[i];
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
                var footerFont = new XFont("Arial", 10, XFontStyleEx.Regular);

                gfx.DrawString(folderName, headerFont, XBrushes.Black,
                    new XRect(0, 12, page.Width.Point, 16), XStringFormats.TopCenter);

                var footer = $"Side {i + 1} av {totalPages}";
                gfx.DrawString(footer, footerFont, XBrushes.Black,
                    new XRect(0, page.Height.Point - 24, page.Width.Point, 16),
                    XStringFormats.TopCenter);
            }
            doc.Save(tempPath);
        }
        return tempPath;
    }
}
