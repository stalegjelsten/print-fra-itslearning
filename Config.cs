using System.Globalization;
using System.Text;

namespace PrintFraItslearning;

public sealed class Config
{
    public string Printer { get; set; } = @"\\TDCSPRN30\Sikker_UtskriftCS";
    public double MarginCm { get; set; } = 2.0;
    public double ImageWidthCm { get; set; } = 17.0;
    public bool CheckForUpdates { get; set; } = false;

    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrintFraItslearning");

    private static string ConfigPath => Path.Combine(ConfigDir, "config.ini");

    public static Config Load()
    {
        var cfg = new Config();
        try
        {
            if (!File.Exists(ConfigPath)) return cfg;
            foreach (var raw in File.ReadAllLines(ConfigPath, Encoding.UTF8))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;
                var key = line[..idx].Trim();
                var val = line[(idx + 1)..].Trim();
                switch (key.ToLowerInvariant())
                {
                    case "printer": cfg.Printer = val; break;
                    case "margin_cm":
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var m))
                            cfg.MarginCm = m;
                        break;
                    case "image_width_cm":
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                            cfg.ImageWidthCm = w;
                        break;
                    case "check_for_updates":
                        if (TryParseBool(val, out var enabled))
                            cfg.CheckForUpdates = enabled;
                        break;
                }
            }
        }
        catch
        {
            // Stille feilhåndtering — bruk defaults
        }
        return cfg;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var sb = new StringBuilder();
            sb.AppendLine($"printer={Printer}");
            sb.AppendLine($"margin_cm={MarginCm.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"image_width_cm={ImageWidthCm.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"check_for_updates={CheckForUpdates.ToString().ToLowerInvariant()}");
            File.WriteAllText(ConfigPath, sb.ToString(), new UTF8Encoding(false));
        }
        catch
        {
            // ignorer skriv-feil
        }
    }

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "yes":
            case "ja":
            case "on":
                result = true;
                return true;
            case "0":
            case "false":
            case "no":
            case "nei":
            case "off":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }
}
