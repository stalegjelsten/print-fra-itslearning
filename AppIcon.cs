using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace PrintFraItslearning;

internal static class AppIcon
{
    private static Icon? _icon;

    public static Icon? Get()
    {
        if (_icon != null) return _icon;
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("PrintFraItslearning.app.ico");
            if (stream == null) return null;
            _icon = new Icon(stream);
            return _icon;
        }
        catch
        {
            return null;
        }
    }

    public static void Apply(Form form)
    {
        var icon = Get();
        if (icon != null)
            form.Icon = icon;
    }
}
