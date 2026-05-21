using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PdfiumViewer;
using PrintFraItslearning.Printing;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning.Forms;

public sealed class PreviewControl : UserControl
{
    private readonly Label _header;
    private readonly LinkLabel _openLink;
    private readonly Label _message;
    private readonly PictureBox _picture;
    private readonly TextBox _text;
    private readonly WebBrowser _web;
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

        _web = new WebBrowser
        {
            Dock = DockStyle.Fill,
            AllowNavigation = true,
            ScriptErrorsSuppressed = true,
            Visible = false
        };

        _content.Controls.Add(_picture);
        _content.Controls.Add(_text);
        _content.Controls.Add(_web);
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
                LoadHtmlPreview(file);
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
        _web.Visible = false;
        try { _web.Navigate("about:blank"); } catch { }
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
            var bmp = await Task.Run(() => RenderPdfFirstPage(file.FullName), token);
            if (token.IsCancellationRequested || _current != file) { bmp?.Dispose(); return; }
            if (bmp == null)
            {
                _message.Text = "Kunne ikke vise PDF.";
                _message.Visible = true;
            }
            else
            {
                _picture.Image = bmp;
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
            _text.Text = sb.ToString();
            _text.Visible = true;
        }
        catch (Exception ex)
        {
            _message.Text = $"Feil ved lesing: {ex.Message}";
            _message.Visible = true;
        }
    }

    private void LoadHtmlPreview(ScannedFile file)
    {
        try
        {
            _web.Navigate(new Uri(file.FullName));
            _web.Visible = true;
        }
        catch (Exception ex)
        {
            _message.Text = $"Feil ved forhåndsvisning: {ex.Message}";
            _message.Visible = true;
        }
    }

    private static Bitmap? RenderPdfFirstPage(string pdfPath)
    {
        PdfiumLoader.EnsureLoaded();
        using var doc = PdfDocument.Load(pdfPath);
        if (doc.PageCount == 0) return null;
        const float dpi = 110f;
        var img = doc.Render(0, dpi, dpi, false);
        return new Bitmap(img);
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
}
