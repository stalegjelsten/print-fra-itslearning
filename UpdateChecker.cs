using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace PrintFraItslearning;

public sealed class UpdateInfo
{
    public required string LatestVersion { get; init; }
    public required string ReleaseUrl { get; init; }
}

public enum UpdateStatus { UpToDate, UpdateAvailable, Failed }

public sealed class UpdateCheckResult
{
    public required UpdateStatus Status { get; init; }
    public UpdateInfo? Info { get; init; }
}

public static class UpdateChecker
{
    private const string ApiUrl =
        "https://api.github.com/repos/stalegjelsten/print-fra-itslearning/releases/latest";

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PrintFraItslearning/2.0");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var json = await http.GetStringAsync(ApiUrl, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var url = root.GetProperty("html_url").GetString() ?? "";

            var cleanTag = tag.TrimStart('v', 'V');
            if (!Version.TryParse(cleanTag, out var latest))
                return new UpdateCheckResult { Status = UpdateStatus.Failed };

            return latest > CurrentVersion
                ? new UpdateCheckResult
                {
                    Status = UpdateStatus.UpdateAvailable,
                    Info = new UpdateInfo { LatestVersion = cleanTag, ReleaseUrl = url }
                }
                : new UpdateCheckResult { Status = UpdateStatus.UpToDate };
        }
        catch
        {
            return new UpdateCheckResult { Status = UpdateStatus.Failed };
        }
    }
}
