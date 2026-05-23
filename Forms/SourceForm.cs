using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PrintFraItslearning.Forms;

public sealed class SourceForm : Form
{
    private const string DocumentationUrl =
        "https://github.com/stalegjelsten/print-fra-itslearning/raw/main/docs/dokumentasjon.pdf";

    private readonly Config _config;
    private readonly Label _updateLabel;
    private readonly Button _updateBtn;
    private readonly ToolTip _toolTip = new();
    private UpdateInfo? _updateInfo;
    private bool _isCheckingForUpdates;

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
        ClientSize = new Size(520, 340);

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

        var helpBtn = new Button
        {
            Text = "Instruksjoner",
            Location = new Point(24, 294),
            Size = new Size(120, 32)
        };
        helpBtn.Click += (_, _) => OpenUrl(DocumentationUrl);
        _toolTip.SetToolTip(helpBtn, "Åpne dokumentasjonen (PDF).");
        Controls.Add(helpBtn);

        _updateBtn = new Button
        {
            Text = "Oppdater",
            Location = new Point(152, 294),
            Size = new Size(120, 32)
        };
        _updateBtn.Click += async (_, _) => await ManualCheckForUpdatesAsync();
        _toolTip.SetToolTip(_updateBtn,
            $"Sjekk etter ny versjon. Gjeldende versjon: {UpdateChecker.CurrentVersion}");
        Controls.Add(_updateBtn);

        var cancelBtn = new Button
        {
            Text = "Avslutt",
            Location = new Point(412, 294),
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
            var result = await UpdateChecker.CheckAsync();
            if (result.Status != UpdateStatus.UpdateAvailable || result.Info == null) return;
            ShowUpdateBanner(result.Info);
        }
        catch
        {
            // Stille feil
        }
    }

    private async Task ManualCheckForUpdatesAsync()
    {
        if (_isCheckingForUpdates) return;
        _isCheckingForUpdates = true;
        _updateBtn.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var result = await UpdateChecker.CheckAsync();
            switch (result.Status)
            {
                case UpdateStatus.UpdateAvailable when result.Info != null:
                    ShowUpdateBanner(result.Info);
                    var open = MessageBox.Show(this,
                        $"Ny versjon {result.Info.LatestVersion} er tilgjengelig. " +
                        $"Du har versjon {UpdateChecker.CurrentVersion}.\n\n" +
                        "Vil du åpne nedlastingssiden nå?",
                        "Oppdatering tilgjengelig",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    if (open == DialogResult.Yes) OpenUrl(result.Info.ReleaseUrl);
                    break;
                case UpdateStatus.UpToDate:
                    MessageBox.Show(this,
                        $"Du har siste versjon ({UpdateChecker.CurrentVersion}).",
                        "Ingen oppdatering",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;
                default:
                    MessageBox.Show(this,
                        "Kunne ikke kontakte GitHub. Sjekk nettforbindelsen og prøv igjen.",
                        "Oppdateringssjekk feilet",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    break;
            }
        }
        finally
        {
            UseWaitCursor = false;
            _updateBtn.Enabled = true;
            _isCheckingForUpdates = false;
        }
    }

    private void ShowUpdateBanner(UpdateInfo info)
    {
        _updateInfo = info;
        _updateLabel.Text = $"Ny versjon tilgjengelig ({info.LatestVersion}). Klikk for å åpne.";
        _updateLabel.Visible = true;
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
