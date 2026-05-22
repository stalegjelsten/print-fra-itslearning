using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PrintFraItslearning.Forms;

public sealed class SourceForm : Form
{
    private readonly Config _config;
    private readonly Label _updateLabel;
    private readonly ToolTip _toolTip = new();
    private UpdateInfo? _updateInfo;

    public SourceForm(Config config)
    {
        _config = config;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AppIcon.Apply(this);

        Text = "Skriv ut elevbesvarelser";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 304);

        var introLabel = new Label
        {
            Text = "Velg ZIP-filen du lastet ned fra itslearning, eller en allerede utpakket mappe.",
            Location = new Point(24, 18),
            Size = new Size(472, 36)
        };
        Controls.Add(introLabel);

        var zipBtn = new Button
        {
            Text = "Velg ZIP-fil fra itslearning",
            Location = new Point(24, 62),
            Size = new Size(472, 60)
        };
        zipBtn.Click += (_, _) => ChooseZip();
        _toolTip.SetToolTip(zipBtn,
            "Pakk ut ZIP-en du har lastet ned fra itslearning og " +
            "skriv ut innholdet.");
        Controls.Add(zipBtn);

        var folderBtn = new Button
        {
            Text = "Velg mappe",
            Location = new Point(24, 134),
            Size = new Size(472, 60)
        };
        folderBtn.Click += (_, _) => ChooseFolder();
        _toolTip.SetToolTip(folderBtn,
            "Velg en mappe direkte (f.eks. allerede utpakket).");
        Controls.Add(folderBtn);

        _updateLabel = new Label
        {
            Location = new Point(24, 210),
            Size = new Size(472, 36),
            ForeColor = Color.DarkOrange,
            Cursor = Cursors.Hand,
            Visible = false
        };
        _updateLabel.Click += (_, _) =>
        {
            if (_updateInfo != null) OpenUrl(_updateInfo.ReleaseUrl);
        };
        Controls.Add(_updateLabel);

        var cancelBtn = new Button
        {
            Text = "Avslutt",
            Location = new Point(412, 258),
            Size = new Size(84, 32),
            DialogResult = DialogResult.Cancel
        };
        cancelBtn.Click += (_, _) => Close();
        Controls.Add(cancelBtn);
        CancelButton = cancelBtn;

        ResumeLayout(performLayout: true);

        Shown += async (_, _) => await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        if (!_config.CheckForUpdates) return;

        try
        {
            var info = await UpdateChecker.CheckAsync();
            if (info == null) return;
            _updateInfo = info;
            _updateLabel.Text = $"Ny versjon tilgjengelig ({info.LatestVersion}). Klikk for å åpne.";
            _updateLabel.Visible = true;
        }
        catch
        {
            // Stille feil
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }

    private void ChooseZip()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Zip-filer (*.zip)|*.zip",
            Title = "Velg zip-fil fra itslearning"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        ZipExtractor? zip = null;
        try
        {
            UseWaitCursor = true;
            zip = ZipExtractor.Extract(dlg.FileName);
        }
        catch (Exception ex)
        {
            UseWaitCursor = false;
            MessageBox.Show(this, $"Kunne ikke pakke ut zip-filen:\n{ex.Message}",
                "Feil", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        finally
        {
            UseWaitCursor = false;
        }

        OpenSelection(zip.ExtractedPath, zip);
    }

    private void ChooseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Velg mappen med filer du vil skrive ut",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        OpenSelection(dlg.SelectedPath, null);
    }

    private void OpenSelection(string path, ZipExtractor? zip)
    {
        Hide();
        try
        {
            using var form = new SelectionForm(_config, path, zip);
            form.ShowDialog();
        }
        finally
        {
            zip?.Dispose();
            Show();
        }
    }
}
