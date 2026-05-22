using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using HtmlAgilityPack;
using PdfiumViewer;
using PrintFraItslearning.Printing;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning.Forms;

public sealed class PreviewControl : UserControl
{
    private sealed record PdfPreview(Bitmap? Image, int PageCount);

    private readonly Label _header;
    private readonly LinkLabel _openLink;
    private readonly Label _message;
    private readonly PictureBox _picture;
    private readonly TextBox _text;
    private readonly ProgressBar _spinner;
    private readonly Panel _content;

    private ScannedFile? _current;
    private CancellationTokenSource? _cts;

    public PreviewControl()
    {
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Window;

        _header = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(6, 4, 6, 0),
            Font = new Font(Font, FontStyle.Bold),
            Text = "Forhåndsvisning",
            AutoEllipsis = true
        };

        _spinner = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 4,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 0,
            Visible = false
        };

        _openLink = new LinkLabel
        {
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(6, 2, 6, 2),
            Text = "Åpne i ekstern app",
            Visible = false
        };
        _openLink.LinkClicked += (_, _) => OpenCurrentExternally();

        _content = new Panel { Dock = DockStyle.Fill };

        _message = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
            Text = "Velg en fil for å se forhåndsvisning."
        };

        _picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = SystemColors.AppWorkspace,
            Visible = false
        };

        _text = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9F),
            Visible = false
        };

        _content.Controls.Add(_picture);
        _content.Controls.Add(_text);
        _content.Controls.Add(_message);

        Controls.Add(_content);
        Controls.Add(_openLink);
        Controls.Add(_spinner);
        Controls.Add(_header);
    }

    public void Show(ScannedFile? file)
    {
        _cts?.Cancel();
        _current = file;
        ClearViews();

        if (file == null)
        {
            _header.Text = "Forhåndsvisning";
            _message.Text = "Velg en fil for å se forhåndsvisning.";
            _message.Visible = true;
            return;
        }

        _header.Text = $"{file.Name}   [{file.FolderName}]";
        _openLink.Text = ExternalOpenText(file.Kind);
        _openLink.Visible = true;

        switch (file.Kind)
        {
            case FileKind.Pdf:
                LoadPdfPreviewAsync(file);
                break;
            case FileKind.Image:
                LoadImagePreviewAsync(file);
                break;
            case FileKind.Text:
                LoadTextPreview(file);
                break;
            case FileKind.Html:
                LoadHtmlAsTextPreview(file);
                break;
            case FileKind.Word:
                _message.Text = "Forhåndsvisning av Word-filer er ikke tilgjengelig.\nDobbeltklikk for å åpne i Word.";
                _message.Visible = true;
                break;
            case FileKind.Excel:
                _message.Text = "Forhåndsvisning av Excel-filer er ikke tilgjengelig.\nDobbeltklikk for å åpne i Excel.";
                _message.Visible = true;
                break;
            default:
                _message.Text = "Forhåndsvisning ikke tilgjengelig for denne filtypen.";
                _message.Visible = true;
                break;
        }
    }

    public void OpenCurrentExternally()
    {
        if (_current == null) return;
        try
        {
            Process.Start(new ProcessStartInfo(_current.FullName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Kunne ikke åpne filen:\n{ex.Message}", "Feil",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearViews()
    {
        _openLink.Visible = false;
        _message.Visible = false;
        _picture.Visible = false;
        if (_picture.Image is { } old)
        {
            _picture.Image = null;
            old.Dispose();
        }
        _text.Visible = false;
        _text.Text = "";
        SetBusy(false);
    }

    private void SetBusy(bool busy)
    {
        _spinner.Visible = busy;
        _spinner.MarqueeAnimationSpeed = busy ? 30 : 0;
    }

    private async void LoadPdfPreviewAsync(ScannedFile file)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        SetBusy(true);
        try
        {
            var preview = await Task.Run(() => RenderPdfFirstPage(file.FullName), token);
            if (token.IsCancellationRequested || _current != file)
            {
                preview.Image?.Dispose();
                return;
            }

            if (preview.PageCount > 0)
                _header.Text = $"{file.Name}   [{file.FolderName}] - Side 1 av {preview.PageCount}";

            if (preview.Image == null)
            {
                _message.Text = "Kunne ikke vise PDF.";
                _message.Visible = true;
            }
            else
            {
                _picture.Image = preview.Image;
                _picture.Visible = true;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _message.Text = $"Feil ved forhåndsvisning: {ex.Message}";
            _message.Visible = true;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void LoadImagePreviewAsync(ScannedFile file)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        SetBusy(true);
        try
        {
            var bmp = await Task.Run(() =>
            {
                using var src = Image.FromFile(file.FullName);
                return new Bitmap(src);
            }, token);
            if (token.IsCancellationRequested || _current != file) { bmp.Dispose(); return; }
            _picture.Image = bmp;
            _picture.Visible = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _message.Text = $"Feil ved forhåndsvisning: {ex.Message}";
            _message.Visible = true;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void LoadTextPreview(ScannedFile file)
    {
        try
        {
            using var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var sb = new StringBuilder();
            for (int i = 0; i < 500 && !reader.EndOfStream; i++)
            {
                sb.AppendLine(reader.ReadLine());
            }
            if (!reader.EndOfStream)
                sb.AppendLine("…  (mer innhold ikke vist)");
            _text.WordWrap = false;
            _text.Text = sb.ToString();
            _text.Visible = true;
        }
        catch (Exception ex)
        {
            _message.Text = $"Feil ved lesing: {ex.Message}";
            _message.Visible = true;
        }
    }

    private void LoadHtmlAsTextPreview(ScannedFile file)
    {
        try
        {
            using var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            _text.WordWrap = true;
            _text.Text = HtmlPreviewText.FromHtml(reader.ReadToEnd());
            _text.Visible = true;
        }
        catch (Exception ex)
        {
            _message.Text = $"Feil ved lesing: {ex.Message}";
            _message.Visible = true;
        }
    }

    private static string ExternalOpenText(FileKind kind) => kind switch
    {
        FileKind.Word => "Åpne i Word",
        FileKind.Excel => "Åpne i Excel",
        FileKind.Pdf => "Åpne PDF eksternt",
        FileKind.Image => "Åpne bilde eksternt",
        FileKind.Html => "Åpne HTML eksternt",
        FileKind.Text => "Åpne tekstfil eksternt",
        _ => "Åpne i ekstern app"
    };

    private static PdfPreview RenderPdfFirstPage(string pdfPath)
    {
        PdfiumLoader.EnsureLoaded();
        using var doc = PdfDocument.Load(pdfPath);
        if (doc.PageCount == 0) return new PdfPreview(null, 0);
        const float dpi = 110f;
        using var img = doc.Render(0, dpi, dpi, false);
        return new PdfPreview(new Bitmap(img), doc.PageCount);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            if (_picture.Image is { } img)
            {
                _picture.Image = null;
                img.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    private static class HtmlPreviewText
    {
        private const int MaxChars = 20000;

        private static readonly HashSet<string> RemovedTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "script", "style", "noscript", "iframe", "object", "embed", "link", "meta",
            "base", "form", "input", "button", "textarea", "select", "svg", "canvas"
        };

        private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "address", "article", "aside", "blockquote", "dd", "div", "dl", "dt",
            "fieldset", "figcaption", "figure", "footer", "h1", "h2", "h3", "h4",
            "h5", "h6", "header", "hr", "li", "main", "nav", "ol", "p", "pre",
            "section", "table", "tbody", "td", "tfoot", "th", "thead", "tr", "ul"
        };

        public static string FromHtml(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.OptionFixNestedTags = true;
            doc.LoadHtml(html);

            RemoveUnsafeNodes(doc.DocumentNode);

            var root = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
            var sb = new StringBuilder();
            AppendNode(root, sb);

            var text = CleanWhitespace(sb.ToString());
            if (text.Length == 0)
                return "HTML-filen har ikke lesbart tekstinnhold.";

            if (text.Length > MaxChars)
                text = text[..MaxChars].TrimEnd() + Environment.NewLine + Environment.NewLine + "... (mer innhold ikke vist)";

            return text;
        }

        private static void RemoveUnsafeNodes(HtmlNode root)
        {
            foreach (var node in root.Descendants().Where(n => RemovedTags.Contains(n.Name)).ToList())
                node.Remove();
        }

        private static void AppendNode(HtmlNode node, StringBuilder sb)
        {
            if (sb.Length >= MaxChars) return;

            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    AppendText(sb, HtmlEntity.DeEntitize(node.InnerText));
                    return;

                case HtmlNodeType.Element when node.Name.Equals("br", StringComparison.OrdinalIgnoreCase):
                    AppendLineBreak(sb);
                    return;

                case HtmlNodeType.Element when node.Name.Equals("img", StringComparison.OrdinalIgnoreCase):
                    AppendImagePlaceholder(node, sb);
                    return;
            }

            var isBlock = node.NodeType == HtmlNodeType.Element && BlockTags.Contains(node.Name);
            if (isBlock) AppendLineBreak(sb);

            foreach (var child in node.ChildNodes)
                AppendNode(child, sb);

            if (isBlock) AppendLineBreak(sb);
        }

        private static void AppendImagePlaceholder(HtmlNode node, StringBuilder sb)
        {
            var alt = node.GetAttributeValue("alt", "");
            var src = node.GetAttributeValue("src", "");
            var label = !string.IsNullOrWhiteSpace(alt)
                ? alt
                : !string.IsNullOrWhiteSpace(src) ? Path.GetFileName(src) : "bilde";

            AppendText(sb, $"[Bilde: {label}]");
            AppendLineBreak(sb);
        }

        private static void AppendText(StringBuilder sb, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (sb.Length > 0 && !char.IsWhiteSpace(sb[^1]))
                sb.Append(' ');
            sb.Append(text.Trim());
        }

        private static void AppendLineBreak(StringBuilder sb)
        {
            if (sb.Length == 0) return;
            if (sb[^1] != '\n')
                sb.AppendLine();
        }

        private static string CleanWhitespace(string text)
        {
            var lines = text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => CollapseSpaces(line).Trim())
                .Where(line => line.Length > 0);

            return string.Join(Environment.NewLine, lines);
        }

        private static string CollapseSpaces(string text)
        {
            var sb = new StringBuilder(text.Length);
            var lastWasSpace = false;

            foreach (var c in text)
            {
                var isSpace = char.IsWhiteSpace(c);
                if (isSpace && lastWasSpace) continue;
                sb.Append(isSpace ? ' ' : c);
                lastWasSpace = isSpace;
            }

            return sb.ToString();
        }
    }
}
