using System.IO.Compression;

namespace PrintFraItslearning;

public sealed class ZipExtractor : IDisposable
{
    public string ExtractedPath { get; }

    private ZipExtractor(string path) { ExtractedPath = path; }

    public static ZipExtractor Extract(string zipPath)
    {
        var target = AppTemp.CreateDirectory("zip");
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
