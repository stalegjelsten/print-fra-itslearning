using System.Windows.Forms;
using PrintFraItslearning.Forms;

namespace PrintFraItslearning;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppTemp.CleanupOldFiles();

        var config = Config.Load();

        try
        {
            Application.Run(new SourceForm(config));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"En uventet feil oppstod:\n{ex.Message}",
                "Feil", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
