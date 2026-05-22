using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using PrintFraItslearning.Printing;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning.Forms;

public sealed class SelectionForm : Form
{
    private sealed record FileGroup(string Title);

    private readonly Config _config;
    private readonly string _rootPath;
    private readonly ZipExtractor? _zip;

    private readonly TreeView _tree;
    private readonly SplitContainer _split;
    private readonly PreviewControl _preview;
    private readonly ComboBox _printerCombo;
    private readonly Label _printerCountLabel;
    private readonly CheckBox _headerFooterCheck;
    private readonly CheckBox _commentsCheck;
    private readonly CheckBox _excelFormulasCheck;
    private readonly CheckBox _combineCheck;
    private readonly CheckBox _duplexCheck;
    private readonly CheckBox _combinePrintCheck;
    private readonly CheckBox _combineSaveCheck;
    private readonly CheckBox _sortFirstNameCheck;
    private readonly Label _statusLabel;
    private readonly Button _printButton;
    private readonly Button _cancelButton;
    private readonly ToolTip _toolTip = new();

    private ScanResult? _scan;
    private CombinedHtmlResult? _combined;
    private List<ScannedFile> _printableFiles = new();
    private bool _hasSortableNames;

    public SelectionForm(Config config, string rootPath, ZipExtractor? zip)
    {
        _config = config;
        _rootPath = rootPath;
        _zip = zip;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AppIcon.Apply(this);

        Text = "Velg hva som skal skrives ut";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1000, 640);
        MinimumSize = new Size(860, 580);

        var printerLabel = new Label
        {
            Text = "Printer:",
            Location = new Point(16, 15),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        Controls.Add(printerLabel);

        _printerCombo = new ComboBox
        {
            Location = new Point(72, 12),
            Size = new Size(804, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownHeight = 240
        };
        _printerCombo.SelectedIndexChanged += (_, _) => SavePrinter();
        Controls.Add(_printerCombo);

        var refreshBtn = new Button
        {
            Text = "↻",
            Location = new Point(884, 10),
            Size = new Size(32, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        refreshBtn.Click += (_, _) => PopulatePrinters();
        _toolTip.SetToolTip(refreshBtn, "Hent installerte printere på nytt");
        Controls.Add(refreshBtn);

        _printerCountLabel = new Label
        {
            Location = new Point(72, 40),
            Size = new Size(844, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = Color.Gray,
            Text = ""
        };
        Controls.Add(_printerCountLabel);

        PopulatePrinters();

        _statusLabel = new Label
        {
            Text = "Skanner…",
            Location = new Point(16, 66),
            Size = new Size(968, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(_statusLabel);

        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            CheckBoxes = true,
            HideSelection = false
        };
        _tree.AfterCheck += Tree_AfterCheck;
        _tree.AfterSelect += Tree_AfterSelect;
        _tree.NodeMouseDoubleClick += Tree_NodeMouseDoubleClick;

        _preview = new PreviewControl();

        var fileListHeader = new Label
        {
            Text = "Filer",
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(4, 5, 0, 0),
            Font = new Font(Font, FontStyle.Bold)
        };

        _split = new SplitContainer
        {
            Location = new Point(16, 94),
            Size = new Size(968, 260),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            Orientation = Orientation.Vertical,
            SplitterDistance = 590,
            FixedPanel = FixedPanel.None
        };
        _split.Panel1.Controls.Add(_tree);
        _split.Panel1.Controls.Add(fileListHeader);
        _split.Panel2.Controls.Add(_preview);
        Controls.Add(_split);

        var selectAll = new Button
        {
            Text = "Velg alle",
            Location = new Point(16, 366),
            Size = new Size(100, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        selectAll.Click += (_, _) => SetAllChecks(true);
        Controls.Add(selectAll);

        var selectNone = new Button
        {
            Text = "Velg ingen",
            Location = new Point(124, 366),
            Size = new Size(100, 30),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        selectNone.Click += (_, _) => SetAllChecks(false);
        Controls.Add(selectNone);

        var settingsGroup = new GroupBox
        {
            Text = "Utskriftsvalg",
            Location = new Point(16, 404),
            Size = new Size(968, 170),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        Controls.Add(settingsGroup);

        _headerFooterCheck = new CheckBox
        {
            Text = "Legg til topptekst (mappenavn) og bunntekst (sidenummer)",
            Location = new Point(14, 26),
            Size = new Size(444, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Checked = true
        };
        settingsGroup.Controls.Add(_headerFooterCheck);

        _commentsCheck = new CheckBox
        {
            Text = "Skriv ut kommentarer i Word-dokumenter",
            Location = new Point(14, 52),
            Size = new Size(444, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Checked = false
        };
        settingsGroup.Controls.Add(_commentsCheck);

        _excelFormulasCheck = new CheckBox
        {
            Text = "Skriv også ut formelvisning av Excel-filer (dobler antall sider)",
            Location = new Point(14, 78),
            Size = new Size(444, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Checked = true,
            Visible = false
        };
        settingsGroup.Controls.Add(_excelFormulasCheck);

        _combineCheck = new CheckBox
        {
            Text = "Kombiner alle filer til én PDF og skriv ut som én jobb",
            Location = new Point(490, 26),
            Size = new Size(444, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Checked = false
        };
        settingsGroup.Controls.Add(_combineCheck);

        _duplexCheck = new CheckBox
        {
            Text = "Tosidig utskrift: legg inn tomme sider mellom elever",
            Location = new Point(514, 52),
            Size = new Size(420, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Checked = true,
            Visible = false
        };
        settingsGroup.Controls.Add(_duplexCheck);

        _combinePrintCheck = new CheckBox
        {
            Text = "Send kombinert PDF til skriveren",
            Location = new Point(514, 78),
            Size = new Size(420, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Checked = true,
            Visible = false
        };
        settingsGroup.Controls.Add(_combinePrintCheck);

        _combineSaveCheck = new CheckBox
        {
            Text = "Lagre kombinert PDF som fil",
            Location = new Point(514, 104),
            Size = new Size(420, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Checked = false,
            Visible = false
        };
        settingsGroup.Controls.Add(_combineSaveCheck);

        _combineCheck.CheckedChanged += (_, _) =>
        {
            _duplexCheck.Visible = _combineCheck.Checked;
            _combinePrintCheck.Visible = _combineCheck.Checked;
            _combineSaveCheck.Visible = _combineCheck.Checked;
            UpdateSelectionUi();
        };
        _combinePrintCheck.CheckedChanged += (_, _) => UpdateSelectionUi();
        _combineSaveCheck.CheckedChanged += (_, _) => UpdateSelectionUi();

        _sortFirstNameCheck = new CheckBox
        {
            Text = "Sorter etter fornavn",
            Location = new Point(14, 104),
            Size = new Size(444, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Checked = false,
            Visible = false
        };
        settingsGroup.Controls.Add(_sortFirstNameCheck);

        _cancelButton = new Button
        {
            Text = "Tilbake",
            Location = new Point(666, 590),
            Size = new Size(104, 34),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            DialogResult = DialogResult.Cancel
        };
        _cancelButton.Click += (_, _) => Close();
        Controls.Add(_cancelButton);
        CancelButton = _cancelButton;

        _printButton = new Button
        {
            Text = "Start",
            Location = new Point(778, 590),
            Size = new Size(210, 34),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Enabled = false
        };
        _printButton.Click += (_, _) => StartPrinting();
        Controls.Add(_printButton);
        AcceptButton = _printButton;

        _toolTip.AutoPopDelay = 12000;
        _toolTip.SetToolTip(_headerFooterCheck,
            "Stempler mappenavn (=elev) som topptekst og «Side X av Y» som bunntekst " +
            "på Word, HTML, Excel og PDF.");
        _toolTip.SetToolTip(_commentsCheck,
            "Tar med kommentarer og endringsmerker fra Word-dokumentet i utskriften.");
        _toolTip.SetToolTip(_excelFormulasCheck,
            "Skriver ut Excel-filene to ganger: én gang med beregnede verdier og " +
            "én gang med formlene synlige.");
        _toolTip.SetToolTip(_combineCheck,
            "Konverterer alt til PDF, slår sammen til én samlet PDF, og sender den som " +
            "én utskriftsjobb (eller lagrer den).");
        _toolTip.SetToolTip(_duplexCheck,
            "Legger en tom side mellom elever som har odde antall sider, slik at " +
            "neste elev alltid starter på forsiden av et nytt ark ved tosidig utskrift.");
        _toolTip.SetToolTip(_combinePrintCheck,
            "Sender den kombinerte PDF-en til skriveren.");
        _toolTip.SetToolTip(_combineSaveCheck,
            "Lagrer den kombinerte PDF-en til en fil du velger.");
        _toolTip.SetToolTip(_sortFirstNameCheck,
            "Sortér elevene etter fornavn i stedet for etternavn.");

        ResumeLayout(performLayout: true);

        Shown += async (_, _) => await Task.Run(ScanAndPopulate);
    }

    private void ScanAndPopulate()
    {
        try
        {
            var scan = FileScanner.Scan(_rootPath);
            var combined = HtmlCombiner.CombineForFolders(scan, _config.MarginCm);

            BeginInvoke(new Action(() =>
            {
                _scan = scan;
                _combined = combined;
                BuildPrintableList();
                PopulateTree();
            }));
        }
        catch (Exception ex)
        {
            BeginInvoke(new Action(() =>
            {
                MessageBox.Show(this, $"Feil under skanning:\n{ex.Message}",
                    "Feil", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }));
        }
    }

    private void BuildPrintableList()
    {
        if (_scan == null || _combined == null) return;

        var foldersWithCombined = _combined.FoldersWithCombinedHtml;

        var htmlToPrint = _scan.Html
            .Where(h => !foldersWithCombined.Contains(h.File.DirectoryName ?? ""))
            .ToList();

        foreach (var gen in _combined.GeneratedHtml)
            htmlToPrint.Add(new ScannedFile(gen, FileKind.Html));

        _printableFiles = new List<ScannedFile>();
        _printableFiles.AddRange(_scan.Word);
        _printableFiles.AddRange(_scan.Excel);
        _printableFiles.AddRange(_scan.Pdf);
        _printableFiles.AddRange(htmlToPrint);

        _hasSortableNames = _printableFiles.Any(f =>
            StudentName.TryParse(f.FolderName) != null);
    }

    private void PopulateTree()
    {
        if (_scan == null) return;

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        var wordFiles = _printableFiles.Where(f => f.Kind == FileKind.Word).ToList();
        var excelFiles = _printableFiles.Where(f => f.Kind == FileKind.Excel).ToList();
        var pdfFiles = _printableFiles.Where(f => f.Kind == FileKind.Pdf).ToList();
        var htmlFiles = _printableFiles.Where(f => f.Kind == FileKind.Html).ToList();

        AddGroup($"Word-filer ({wordFiles.Count})", wordFiles);
        AddGroup($"Excel-filer ({excelFiles.Count})", excelFiles);
        AddGroup($"PDF-filer ({pdfFiles.Count})", pdfFiles);
        AddGroup($"HTML/bilder ({htmlFiles.Count})", htmlFiles);

        foreach (TreeNode node in _tree.Nodes)
            node.Expand();

        _tree.EndUpdate();

        _sortFirstNameCheck.Visible = _hasSortableNames;
        _commentsCheck.Visible = wordFiles.Count > 0;
        _excelFormulasCheck.Visible = excelFiles.Count > 0;
        _headerFooterCheck.Visible = _printableFiles.Count > 0;
        UpdateSelectionUi();
    }

    private void AddGroup(string title, List<ScannedFile> files)
    {
        if (files.Count == 0) return;
        var groupNode = new TreeNode(title) { Checked = true, Tag = new FileGroup(title) };
        foreach (var f in files)
        {
            var label = $"{f.Name}   [{f.FolderName}]";
            var node = new TreeNode(label) { Checked = true, Tag = f };
            groupNode.Nodes.Add(node);
        }
        _tree.Nodes.Add(groupNode);
    }

    private bool _suppressCheckEvent;

    private void Tree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        var file = e.Node?.Tag as ScannedFile;
        _preview.Show(file);
    }

    private void Tree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is ScannedFile)
        {
            // Default checkbox-toggle som første klikk utløste — nullstill.
            _suppressCheckEvent = true;
            try { e.Node.Checked = !e.Node.Checked; }
            finally { _suppressCheckEvent = false; }

            _tree.SelectedNode = e.Node;
            _preview.OpenCurrentExternally();
        }
    }

    private void Tree_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_suppressCheckEvent || e.Node == null) return;
        _suppressCheckEvent = true;
        try
        {
            if (e.Node.Tag is FileGroup)
            {
                foreach (TreeNode child in e.Node.Nodes)
                    child.Checked = e.Node.Checked;
            }
            else if (e.Node.Parent != null)
            {
                // Hvis alle barna er på, hak gruppenoden; ellers ikke
                var parent = e.Node.Parent;
                bool allChecked = parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                parent.Checked = allChecked;
            }
        }
        finally
        {
            _suppressCheckEvent = false;
        }
        UpdateGroupLabels();
        UpdateSelectionUi();
    }

    private void SetAllChecks(bool value)
    {
        _suppressCheckEvent = true;
        try
        {
            foreach (TreeNode group in _tree.Nodes)
            {
                group.Checked = value;
                foreach (TreeNode child in group.Nodes)
                    child.Checked = value;
            }
        }
        finally
        {
            _suppressCheckEvent = false;
        }
        UpdateGroupLabels();
        UpdateSelectionUi();
    }

    private void UpdateSelectionUi()
    {
        var total = _printableFiles.Count;
        var selected = CountSelectedFiles();

        if (total == 0)
        {
            _statusLabel.Text = "Ingen utskrivbare filer funnet i valgt kilde.";
            _printButton.Text = "Start";
            _printButton.Enabled = false;
            return;
        }

        _statusLabel.Text = $"{selected} av {total} filer valgt";
        var hasPdfAction = !_combineCheck.Checked || _combinePrintCheck.Checked || _combineSaveCheck.Checked;
        _printButton.Enabled = selected > 0 && hasPdfAction;

        if (_combineCheck.Checked && !hasPdfAction)
            _statusLabel.Text += " - velg om PDF-en skal skrives ut eller lagres";

        _printButton.Text = PrintButtonText(selected);
    }

    private string PrintButtonText(int selected)
    {
        if (!_combineCheck.Checked)
            return $"Skriv ut {selected} {FileText(selected)}";

        if (_combinePrintCheck.Checked && _combineSaveCheck.Checked)
            return $"Lag og skriv ut PDF ({selected})";
        if (_combinePrintCheck.Checked)
            return $"Skriv ut samlet PDF ({selected})";
        if (_combineSaveCheck.Checked)
            return $"Lagre samlet PDF ({selected})";

        return "Velg PDF-handling";
    }

    private void UpdateGroupLabels()
    {
        foreach (TreeNode group in _tree.Nodes)
        {
            if (group.Tag is not FileGroup fileGroup) continue;

            var total = group.Nodes.Count;
            var selected = group.Nodes.Cast<TreeNode>().Count(n => n.Checked);
            group.Text = selected == total
                ? fileGroup.Title
                : $"{fileGroup.Title} - {selected} av {total} valgt";
        }
    }

    private int CountSelectedFiles()
    {
        var selected = 0;
        foreach (TreeNode group in _tree.Nodes)
        {
            foreach (TreeNode child in group.Nodes)
            {
                if (child.Checked && child.Tag is ScannedFile)
                    selected++;
            }
        }

        return selected;
    }

    private static string FileText(int count) => count == 1 ? "fil" : "filer";

    private void StartPrinting()
    {
        var selected = new List<ScannedFile>();
        foreach (TreeNode group in _tree.Nodes)
        {
            foreach (TreeNode child in group.Nodes)
            {
                if (child.Checked && child.Tag is ScannedFile sf)
                    selected.Add(sf);
            }
        }

        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Ingen filer er valgt.", "Ingen valg",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        bool sortByFirstName = _sortFirstNameCheck.Visible && _sortFirstNameCheck.Checked;
        var sorted = SortFiles(selected, sortByFirstName);

        bool combine = _combineCheck.Checked;
        bool combinePrint = !combine || _combinePrintCheck.Checked;
        bool combineSave = combine && _combineSaveCheck.Checked;

        if (combine && !combinePrint && !combineSave)
        {
            MessageBox.Show(this,
                "Velg minst ett av «Send kombinert PDF til skriveren» eller «Lagre kombinert PDF som fil».",
                "Manglende handling", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string? savePath = null;
        if (combineSave)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Lagre kombinert PDF",
                Filter = "PDF-filer (*.pdf)|*.pdf",
                FileName = $"utskrift_{DateTime.Now:yyyy-MM-dd_HHmm}.pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            if (IsPotentiallySharedPath(dlg.FileName))
            {
                var confirm = MessageBox.Show(this,
                    "Den valgte plasseringen ser ut til å være en nettverks- eller sky-synkronisert mappe. " +
                    "Den kombinerte PDF-en kan inneholde personopplysninger for mange elever.\n\n" +
                    "Vil du lagre der likevel?",
                    "Mulig delt lagringssted",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (confirm != DialogResult.Yes)
                    return;
            }

            savePath = dlg.FileName;
        }

        var job = new PrintJob
        {
            Config = _config,
            Printer = _config.Printer,
            Files = sorted,
            AddHeaderFooter = _headerFooterCheck.Checked,
            PrintWordComments = _commentsCheck.Checked,
            PrintExcelFormulas = _excelFormulasCheck.Visible && _excelFormulasCheck.Checked,
            CombineToOnePdf = combine,
            DuplexPadBlankPages = combine && _duplexCheck.Checked,
            PrintCombinedPdf = combinePrint,
            SaveCombinedPdf = combineSave,
            SortByFirstName = sortByFirstName,
            GeneratedHtmlFiles = _combined?.GeneratedHtml ?? new List<FileInfo>(),
            TempExtraction = _zip,
            SaveCombinedPdfPath = savePath
        };

        Hide();
        try
        {
            using var progress = new ProgressForm(job);
            progress.ShowDialog();
        }
        finally
        {
            Close();
        }
    }

    private static List<ScannedFile> SortFiles(List<ScannedFile> files, bool sortByFirstName)
    {
        // Word/HTML først (raskt via COM), så Excel (via PDF-eksport), så PDF til slutt
        return files
            .OrderBy(f => SortBucket(f.Kind))
            .ThenBy(f => NameKey(f, sortByFirstName), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int SortBucket(FileKind kind) => kind switch
    {
        FileKind.Word => 0,
        FileKind.Html => 0,
        FileKind.Excel => 1,
        FileKind.Pdf => 2,
        _ => 3
    };

    private static string NameKey(ScannedFile f, bool byFirstName)
    {
        if (!byFirstName) return f.FolderName;
        var student = StudentName.TryParse(f.FolderName);
        return student != null ? $"{student.Fornavn} {student.Etternavn}" : f.FolderName;
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

            _printerCountLabel.Text = $"{found.Count} installerte printere funnet" +
                (defaultPrinter != null ? $" (standard: {defaultPrinter})" : "");
        }
        finally
        {
            _printerCombo.EndUpdate();
        }
    }

    private void SavePrinter()
    {
        var selected = _printerCombo.SelectedItem?.ToString() ?? _printerCombo.Text;
        if (string.IsNullOrWhiteSpace(selected)) return;
        _config.Printer = selected.Trim();
        _config.Save();
    }

    private static bool IsPotentiallySharedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(@"\\", StringComparison.Ordinal)) return true;

            var segments = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var segment in segments)
            {
                if (segment.Contains("OneDrive", StringComparison.OrdinalIgnoreCase) ||
                    segment.Contains("SharePoint", StringComparison.OrdinalIgnoreCase) ||
                    segment.Contains("Dropbox", StringComparison.OrdinalIgnoreCase) ||
                    segment.Contains("Google Drive", StringComparison.OrdinalIgnoreCase) ||
                    segment.Contains("iCloudDrive", StringComparison.OrdinalIgnoreCase) ||
                    segment.Contains("iCloud Drive", StringComparison.OrdinalIgnoreCase) ||
                    segment.Contains("Teams", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
