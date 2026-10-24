using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

/// <summary>A five-color workspace with explicit regeneration and exact local color locks.</summary>
internal sealed class PaletteGeneratorForm : Form
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };
    private readonly ColormindClient _client;
    private readonly Color[] _colors = [StudioTheme.Accent, StudioTheme.Mint, StudioTheme.Raised, StudioTheme.Surface, StudioTheme.Canvas];
    private readonly CheckBox[] _locks = new CheckBox[5];
    private readonly ColorPreview[] _previews = new ColorPreview[5];
    private readonly Label[] _values = new Label[5];
    private readonly StudioButton _generate = new() { Text = "Generate", Name = "GeneratePaletteButton", Primary = true, Dock = DockStyle.Fill };
    private readonly Label _status = StudioTheme.Label("", StudioTheme.Small, StudioTheme.Muted);
    private readonly List<Control> _editors = [];
    private readonly CancellationTokenSource _lifetime = new();
    private bool _busy;
    private bool _hasPalette;
    private bool _initialRequested;
    private bool _resourcesDisposed;

    internal PaletteGeneratorForm(Color? seed = null, ColormindClient? client = null)
    {
        _client = client ?? new ColormindClient(Http);
        Text = "ChromaTray · Palette generator";
        BackColor = StudioTheme.Canvas;
        ForeColor = StudioTheme.Text;
        Font = StudioTheme.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(1080, 540);
        MinimumSize = new Size(900, 500);
        StartPosition = FormStartPosition.CenterParent;
        DoubleBuffered = true;
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = Padding.Empty };
        for (int index = 0; index < 5; index++)
        {
            int slot = index;
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            var card = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(12), Margin = new Padding(4) };
            var preview = new ColorPreview { Dock = DockStyle.Fill, ShowPixelShare = false, ShowDetails = false };
            var value = StudioTheme.Label("", StudioTheme.Mono);
            value.TextAlign = ContentAlignment.MiddleCenter;
            value.Name = $"PaletteHex{index}";
            var locked = new CheckBox { Text = "Locked", Dock = DockStyle.Fill, ForeColor = StudioTheme.Text, AccessibleName = $"Lock color {index + 1}" };
            var pick = new StudioButton { Text = "Pick pixel", Dock = DockStyle.Fill, Name = $"PickPalettePixel{index}" };
            var copy = new StudioButton { Text = "Copy HEX", Dock = DockStyle.Fill };
            pick.Click += async (_, _) => await PickPixelAsync(slot);
            copy.Click += (_, _) => CopyText(ColorDetails.FromColor(_colors[slot]).Hex);
            var stack = StudioTheme.Stack((preview, -1), (value, 32), (locked, 32), (pick, 42), (copy, 42));
            stack.BackColor = StudioTheme.Surface;
            card.Controls.Add(stack);
            columns.Controls.Add(card, index, 0);
            _locks[index] = locked;
            _previews[index] = preview;
            _values[index] = value;
            _editors.Add(locked);
            _editors.Add(pick);
            _editors.Add(copy);
        }
        var copyCss = new StudioButton { Text = "Copy CSS", Dock = DockStyle.Fill };
        copyCss.Click += (_, _) => CopyText(":root {\n" + string.Join("\n", _colors.Select((color, i) => $"  --color-{i + 1}: {ColorDetails.FromColor(color).Hex};")) + "\n}");
        _editors.Add(copyCss);
        _generate.Click += async (_, _) => await GenerateAsync();
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        actions.Controls.Add(_generate, 0, 0);
        actions.Controls.Add(copyCss, 1, 0);
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        var websitePreview = new StudioButton { Text = "Website preview", Name = "WebsitePreviewButton", Dock = DockStyle.Fill };
        websitePreview.Click += (_, _) => new WebsitePreviewForm(_colors).Show(this);
        actions.Controls.Add(websitePreview, 2, 0);
        _editors.Add(websitePreview);
        var notice = StudioTheme.Label("Colormind · Online · Sends five RGB positions only. Free for personal / non-commercial use.", StudioTheme.Small, StudioTheme.Muted);
        var root = StudioTheme.Stack((StudioTheme.Label("PALETTE GENERATOR", StudioTheme.Heading), 48), (columns, -1), (actions, 48), (_status, 44), (notice, 32));
        root.BackColor = StudioTheme.Canvas;
        root.LogicalPadding = new Padding(16);
        Controls.Add(root);
        if (seed is { } color)
        {
            _colors[0] = color;
            _locks[0].Checked = true;
        }
        UpdateColors();
        SetBusy(false);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StudioTheme.ApplyWindowChrome(this);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_initialRequested)
        {
            _initialRequested = true;
            await GenerateAsync();
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _generate.Enabled = !busy;
        foreach (var editor in _editors)
        {
            editor.Enabled = !busy && (_hasPalette || editor is CheckBox || editor.Name.StartsWith("PickPalettePixel", StringComparison.Ordinal));
        }
        UseWaitCursor = busy;
    }

    private void UpdateColors()
    {
        for (int index = 0; index < 5; index++)
        {
            _previews[index].ColorDetails = ColorDetails.FromColor(_colors[index]);
            _values[index].Text = ColorDetails.FromColor(_colors[index]).Hex;
        }
    }

    private async Task GenerateAsync()
    {
        if (_busy)
            return;
        if (_locks.All(locked => locked.Checked))
        {
            _status.Text = "Unlock at least one color to generate suggestions.";
            return;
        }
        SetBusy(true);
        _status.Text = "Generating with Colormind…";
        try
        {
            var colors = await _client.GenerateAsync(_colors.Select((color, i) => _locks[i].Checked ? (Color?)color : null).ToArray(), _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested)
                return;
            colors.CopyTo(_colors, 0);
            _hasPalette = true;
            UpdateColors();
            _status.Text = "Palette ready. Lock colors you like, or pick a pixel, then press Generate.";
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
                _status.Text = "Request timed out. Try Generate again.";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidDataException or NotSupportedException)
        {
            if (!IsDisposed)
                _status.Text = "Could not get a palette. Check your connection and retry. Existing colors are preserved.";
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task PickPixelAsync(int slot)
    {
        if (_busy)
            return;
        SetBusy(true);
        bool ownerVisible = Owner?.Visible == true;
        try
        {
            Owner?.Hide();
            var overlay = await DesktopPicker.CreateOverlayAsync(this, CaptureMode.Point, _lifetime.Token);
            using (overlay)
            {
                if (overlay.ShowDialog() == DialogResult.OK && overlay.PickedPointColor is { } color)
                {
                    ApplyPickedColor(slot, color);
                }
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // The generator was closed while the desktop was being prepared.
        }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException or ArgumentException)
        {
            if (!IsDisposed)
                _status.Text = "Could not capture the pixel. Please try again.";
        }
        finally
        {
            if (ownerVisible && Owner is { IsDisposed: false } owner)
                owner.Show();
            if (!IsDisposed && !_lifetime.IsCancellationRequested)
            {
                Show();
                Activate();
                SetBusy(false);
            }
        }
    }

    internal void ApplyPickedColor(int slot, Color color)
    {
        _colors[slot] = color;
        _locks[slot].Checked = true;
        _hasPalette = true;
        UpdateColors();
        _status.Text = "Pixel selected and locked. Press Generate to update the other colors.";
    }

    private void CopyText(string text)
    {
        try
        {
            Clipboard.SetText(text);
            _status.Text = "Copied to clipboard.";
        }
        catch (ExternalException) { _status.Text = "Clipboard is busy. Try again."; }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_resourcesDisposed)
        {
            _resourcesDisposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
