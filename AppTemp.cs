namespace PrintFraItslearning;

public static class AppTemp
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(2);

    public static string RootPath =>
        Path.Combine(Path.GetTempPath(), "PrintFraItslearning");

    public static void CleanupOldFiles()
    {
        try
        {
            if (!Directory.Exists(RootPath)) return;

            var cutoff = DateTime.UtcNow - MaxAge;
            foreach (var path in Directory.EnumerateFileSystemEntries(RootPath))
            {
                try
                {
                    var lastWrite = Directory.Exists(path)
                        ? Directory.GetLastWriteTimeUtc(path)
                        : File.GetLastWriteTimeUtc(path);
                    if (lastWrite >= cutoff) continue;

                    if (Directory.Exists(path))
                        Directory.Delete(path, recursive: true);
                    else
                        File.Delete(path);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    public static string CreateDirectory(string prefix)
    {
        Directory.CreateDirectory(RootPath);
        var path = Path.Combine(RootPath, $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static string FilePath(string prefix, string extension)
    {
        Directory.CreateDirectory(RootPath);
        if (!extension.StartsWith(".", StringComparison.Ordinal))
            extension = "." + extension;
        return Path.Combine(RootPath, $"{prefix}_{Guid.NewGuid():N}{extension}");
    }
}
