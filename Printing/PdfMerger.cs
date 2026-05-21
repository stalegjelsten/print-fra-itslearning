using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PrintFraItslearning.Printing;

public static class PdfMerger
{
    public static void Combine(IEnumerable<string> pdfPaths, string outputPath)
    {
        using var output = new PdfDocument();
        foreach (var path in pdfPaths)
            ImportPagesInto(output, path);
        output.Save(outputPath);
    }

    public static void ImportPagesInto(PdfDocument target, string sourcePdfPath)
    {
        using var input = PdfReader.Open(sourcePdfPath, PdfDocumentOpenMode.Import);
        for (int i = 0; i < input.PageCount; i++)
            target.AddPage(input.Pages[i]);
    }

    /// <summary>
    /// Legger til en tom side med samme dimensjon og orientering som siste side
    /// hvis dokumentet har odde sidetall. Brukes mellom elever for å sikre at
    /// neste elev alltid begynner på en ny ark når man printer tosidig (duplex).
    /// </summary>
    public static void PadToEven(PdfDocument doc)
    {
        if (doc.PageCount == 0) return;
        if (doc.PageCount % 2 == 0) return;

        var last = doc.Pages[doc.PageCount - 1];
        var blank = doc.AddPage();
        blank.Width = last.Width;
        blank.Height = last.Height;
        blank.Orientation = last.Orientation;
    }
}
