using System.IO.Compression;

namespace PrintFraItslearning;

public sealed class ZipExtractor : IDisposable
{
    public string ExtractedPath { get; }

    private ZipExtractor(string path) { ExtractedPath = path; }

    public static ZipExtractor Extract(string zipPath)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var target = Path.Combine(Path.GetTempPath(), $"PrintFraItslearning_{stamp}");
        ZipFile.ExtractToDirectory(zipPath, target);
        return new ZipExtractor(target);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(ExtractedPath))
                Directory.Delete(ExtractedPath, recursive: true);
        }
        catch
        {
            // Stille opprydding
        }
    }
}
