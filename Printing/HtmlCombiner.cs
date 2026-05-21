using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning.Printing;

public sealed record CombinedHtmlResult(
    List<FileInfo> GeneratedHtml,
    HashSet<string> FoldersWithCombinedHtml);

public static class HtmlCombiner
{
    private const int MaxWidthPx = 650;

    private static readonly Regex BodyRegex =
        new(@"<body[^>]*>(?<body>.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex SrcRegex =
        new(@"src=""(?<src>[^""]+)""", RegexOptions.IgnoreCase);

    public static CombinedHtmlResult CombineForFolders(ScanResult scan, double marginCm)
    {
        var generated = new List<FileInfo>();
        var foldersWithCombined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var imagesByFolder = scan.Image.GroupBy(f => f.File.DirectoryName ?? "",
            StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(),
            StringComparer.OrdinalIgnoreCase);
        var textsByFolder = scan.Text.GroupBy(f => f.File.DirectoryName ?? "",
            StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(),
            StringComparer.OrdinalIgnoreCase);
        var htmlByFolder = scan.Html.GroupBy(f => f.File.DirectoryName ?? "",
            StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(),
            StringComparer.OrdinalIgnoreCase);

        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in imagesByFolder.Keys) folders.Add(k);
        foreach (var k in textsByFolder.Keys) folders.Add(k);

        foreach (var folderPath in folders)
        {
            if (string.IsNullOrEmpty(folderPath)) continue;
            var folderName = Path.GetFileName(folderPath);
            var images = imagesByFolder.GetValueOrDefault(folderPath) ?? new List<ScannedFile>();
            var texts = textsByFolder.GetValueOrDefault(folderPath) ?? new List<ScannedFile>();
            htmlByFolder.TryGetValue(folderPath, out var existingHtml);

            var html = BuildHtml(folderName, marginCm, texts, existingHtml, images);
            var htmlFileName = Path.Combine(folderPath, $"{folderName}_kombinert.html");
            File.WriteAllText(htmlFileName, html, new UTF8Encoding(false));

            generated.Add(new FileInfo(htmlFileName));
            if (existingHtml != null && existingHtml.Count > 0)
                foldersWithCombined.Add(folderPath);
        }

        return new CombinedHtmlResult(generated, foldersWithCombined);
    }

    private static string BuildHtml(string folderName, double marginCm,
        List<ScannedFile> texts, List<ScannedFile>? existingHtml, List<ScannedFile> images)
    {
        var margin = marginCm.ToString("0.##", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html>\n<head>\n");
        sb.Append("    <meta charset=\"UTF-8\">\n");
        sb.Append($"    <title>{HtmlEncode(folderName)}</title>\n");
        sb.Append("    <style>\n");
        sb.Append($"        @page {{ size: A4; margin: {margin}cm; }}\n");
        sb.Append($"        body {{ font-family: Arial, sans-serif; margin: 0; padding: {margin}cm; }}\n");
        sb.Append("        h1 { text-align: center; color: #333; font-size: 24pt; margin: 0 0 1cm 0; page-break-after: avoid; }\n");
        sb.Append("        .image-container { width: 100%; }\n");
        sb.Append("        .image-wrapper { page-break-inside: avoid; page-break-after: always; text-align: center; margin-bottom: 1cm; }\n");
        sb.Append("        .image-wrapper:last-child { page-break-after: auto; }\n");
        sb.Append("        .image-wrapper img { display: block; margin: 0 auto; }\n");
        sb.Append("        .image-caption { margin-top: 0.5cm; font-size: 10pt; color: #666; page-break-before: avoid; }\n");
        sb.Append("        @media print { body { margin: 0; padding: 0; } .image-wrapper { page-break-inside: avoid; page-break-after: always; } .image-wrapper:last-child { page-break-after: auto; } }\n");
        sb.Append("    </style>\n</head>\n<body>\n");
        sb.Append($"    <h1>{HtmlEncode(folderName)}</h1>\n    <div class=\"image-container\">\n");

        foreach (var t in texts)
        {
            var content = File.ReadAllText(t.FullName, Encoding.UTF8);
            sb.Append($"\n<!-- Innhold fra: {t.Name} -->\n");
            sb.Append("<div style='white-space: pre-wrap; font-family: monospace; margin-bottom: 1cm; padding: 0.5cm; border: 1px solid #ccc; background-color: #f9f9f9;'>");
            sb.Append(HtmlEncode(content));
            sb.Append("</div>\n");
        }

        var referencedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (existingHtml != null)
        {
            foreach (var html in existingHtml)
            {
                var existingContent = File.ReadAllText(html.FullName, Encoding.UTF8);
                var m = BodyRegex.Match(existingContent);
                if (m.Success)
                {
                    sb.Append($"\n<!-- Innhold fra: {html.Name} -->\n");
                    sb.Append(m.Groups["body"].Value);
                    sb.Append('\n');
                }
                foreach (Match sm in SrcRegex.Matches(existingContent))
                {
                    referencedImages.Add(Path.GetFileName(sm.Groups["src"].Value));
                }
            }
        }

        foreach (var img in images)
        {
            if (referencedImages.Contains(img.Name)) continue;
            var (width, height) = GetScaledDimensions(img.FullName);
            sb.Append("\n        <div class=\"image-wrapper\">\n");
            sb.Append($"            <img src=\"{HtmlEncode(img.Name)}\" alt=\"{HtmlEncode(Path.GetFileNameWithoutExtension(img.Name))}\" width=\"{width}\" height=\"{height}\" style=\"display: block; margin: 0 auto;\">\n");
            sb.Append($"            <div class=\"image-caption\">{HtmlEncode(img.Name)}</div>\n");
            sb.Append("        </div>\n");
        }

        sb.Append("\n    </div>\n</body>\n</html>\n");
        return sb.ToString();
    }

    private static (int width, int height) GetScaledDimensions(string imagePath)
    {
        try
        {
            using var img = System.Drawing.Image.FromFile(imagePath);
            int originalWidth = img.Width;
            int originalHeight = img.Height;
            if (originalWidth > MaxWidthPx)
            {
                var scale = (double)MaxWidthPx / originalWidth;
                return (MaxWidthPx, (int)Math.Round(originalHeight * scale));
            }
            return (originalWidth, originalHeight);
        }
        catch
        {
            return (415, 227);
        }
    }

    private static string HtmlEncode(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
         .Replace("\"", "&quot;").Replace("'", "&#39;");
}
