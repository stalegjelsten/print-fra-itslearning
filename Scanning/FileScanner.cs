using System.IO;

namespace PrintFraItslearning.Scanning;

public enum FileKind { Word, Pdf, Html, Excel, Image, Text }

public sealed record ScannedFile(FileInfo File, FileKind Kind)
{
    public string FolderName => File.Directory?.Name ?? "";
    public string FullName => File.FullName;
    public string Name => File.Name;
    public string Extension => File.Extension.ToLowerInvariant();
}

public sealed class ScanResult
{
    public List<ScannedFile> Word { get; } = new();
    public List<ScannedFile> Pdf { get; } = new();
    public List<ScannedFile> Html { get; } = new();
    public List<ScannedFile> Excel { get; } = new();
    public List<ScannedFile> Image { get; } = new();
    public List<ScannedFile> Text { get; } = new();

    public int TotalCount => Word.Count + Pdf.Count + Html.Count + Excel.Count + Image.Count + Text.Count;
}

public static class FileScanner
{
    private static readonly HashSet<string> WordExts = new(StringComparer.OrdinalIgnoreCase) { ".docx" };
    private static readonly HashSet<string> PdfExts = new(StringComparer.OrdinalIgnoreCase) { ".pdf" };
    private static readonly HashSet<string> HtmlExts = new(StringComparer.OrdinalIgnoreCase) { ".html", ".htm" };
    private static readonly HashSet<string> ExcelExts = new(StringComparer.OrdinalIgnoreCase)
        { ".xlsx", ".xlsm", ".xls" };
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    private static readonly HashSet<string> TextExts = new(StringComparer.OrdinalIgnoreCase) { ".txt" };

    public static ScanResult Scan(string root)
    {
        var result = new ScanResult();
        var dir = new DirectoryInfo(root);
        if (!dir.Exists) return result;

        foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            if (file.Name.StartsWith(".", StringComparison.Ordinal)) continue;
            var ext = file.Extension;
            if (WordExts.Contains(ext)) result.Word.Add(new ScannedFile(file, FileKind.Word));
            else if (PdfExts.Contains(ext)) result.Pdf.Add(new ScannedFile(file, FileKind.Pdf));
            else if (HtmlExts.Contains(ext)) result.Html.Add(new ScannedFile(file, FileKind.Html));
            else if (ExcelExts.Contains(ext)) result.Excel.Add(new ScannedFile(file, FileKind.Excel));
            else if (ImageExts.Contains(ext)) result.Image.Add(new ScannedFile(file, FileKind.Image));
            else if (TextExts.Contains(ext)) result.Text.Add(new ScannedFile(file, FileKind.Text));
        }

        return result;
    }
}
