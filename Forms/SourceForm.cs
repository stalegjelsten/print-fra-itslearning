using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace PrintFraItslearning.Forms;

public sealed class SourceForm : Form
{
    private readonly Config _config;
    private readonly ComboBox _printerCombo;
    private readonly Label _updateLabel;
    private readonly Label _printerCountLabel;
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
        ClientSize = new Size(520, 360);

        var zipBtn = new Button
        {
            Text = "Velg ZIP-fil fra itslearning",
            Location = new Point(24, 24),
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
            Location = new Point(24, 96),
            Size = new Size(472, 60)
        };
        folderBtn.Click += (_, _) => ChooseFolder();
        _toolTip.SetToolTip(folderBtn,
            "Velg en mappe direkte (f.eks. allerede utpakket).");
        Controls.Add(folderBtn);

        var printerLabel = new Label
        {
            Text = "Printer:",
            Location = new Point(24, 195),
            AutoSize = true
        };
        Controls.Add(printerLabel);

        _printerCombo = new ComboBox
        {
            Location = new Point(80, 192),
            Size = new Size(338, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownHeight = 240
        };
        PopulatePrinters();
        _printerCombo.SelectedIndexChanged += (_, _) => SavePrinter(silent: true);
        Controls.Add(_printerCombo);

        var refreshBtn = new Button
        {
            Text = "↻",
            Location = new Point(424, 190),
            Size = new Size(32, 28)
        };
        refreshBtn.Click += (_, _) => RefreshPrinters();
        _toolTip.SetToolTip(refreshBtn, "Hent installerte printere på nytt");
        Controls.Add(refreshBtn);

        _printerCountLabel = new Label
        {
            Location = new Point(80, 220),
            Size = new Size(420, 18),
            ForeColor = Color.Gray,
            Text = ""
        };
        Controls.Add(_printerCountLabel);

        _updateLabel = new Label
        {
            Location = new Point(24, 248),
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
            Text = "Avbryt",
            Location = new Point(412, 314),
            Size = new Size(84, 32),
            DialogResult = DialogResult.Cancel
        };
        cancelBtn.Click += (_, _) => Close();
        Controls.Add(cancelBtn);
        CancelButton = cancelBtn;

        ResumeLayout(performLayout: true);

        Shown += async (_, _) => await CheckForUpdatesAsync();
    }

    private void PopulatePrinters()
    {
        _printerCombo.BeginUpdate();
        try
        {
            _printerCombo.Items.Clear();
            string? defaultPrinter = null;
            try { defaultPrinter = new PrinterSettings().PrinterName; } catch { }

            var found = new List<string>();
            try
            {
                foreach (string name in PrinterSettings.InstalledPrinters)
                    found.Add(name);
            }
            catch { }

            foreach (var name in found.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                _printerCombo.Items.Add(name);

            var preferred = _config.Printer;
            if (!string.IsNullOrWhiteSpace(preferred) && !_printerCombo.Items.Contains(preferred))
                _printerCombo.Items.Insert(0, preferred);

            if (!string.IsNullOrWhiteSpace(preferred))
                _printerCombo.SelectedItem = preferred;
            else if (defaultPrinter != null && _printerCombo.Items.Contains(defaultPrinter))
                _printerCombo.SelectedItem = defaultPrinter;
            else if (_printerCombo.Items.Count > 0)
                _printerCombo.SelectedIndex = 0;

            if (_printerCountLabel != null)
                _printerCountLabel.Text = $"{found.Count} installerte printere funnet" +
                    (defaultPrinter != null ? $" (standard: {defaultPrinter})" : "");
        }
        finally
        {
            _printerCombo.EndUpdate();
        }
    }

    private void RefreshPrinters()
    {
        PopulatePrinters();
    }

    private void SavePrinter(bool silent)
    {
        var selected = _printerCombo.SelectedItem?.ToString() ?? _printerCombo.Text;
        if (string.IsNullOrWhiteSpace(selected)) return;
        _config.Printer = selected.Trim();
        _config.Save();
        if (!silent)
            MessageBox.Show(this, $"Lagret printer:\n{_config.Printer}",
                "Printer lagret", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CheckForUpdatesAsync()
    {
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

        SavePrinter(silent: true);

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

        SavePrinter(silent: true);
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
