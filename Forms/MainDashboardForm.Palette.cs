using System.Runtime.InteropServices;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

// Color selection, regrouping, inspector updates, and clipboard operations.
public partial class MainDashboardForm
{
    private readonly Dictionary<ColorFormat, TextBox> _formatTextBoxes = new();

    private List<ColorDetails> _currentPalette = new();

    private void OnGroupingChanged(object? sender, EventArgs e)
    {

        if (_settings.GroupSimilarColors != _groupSimilarCheckBox.Checked)
        {
            _settings.GroupSimilarColors = _groupSimilarCheckBox.Checked;
            try
            {
                _settings.Save();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"Could not save settings: {ex.Message}", "ChromaTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        if (_lastRegionBitmap != null)
        {
            RunRegionAnalysis(_lastRegionBitmap);
        }
    }

    private void OnPaletteSelectionChanged(object? sender, EventArgs e)
    {
        if (_paletteGrid.SelectedColor is { } color)
        {
            DisplaySelectedColor(color);
        }
    }

    private void OnPaletteKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C)
        {
            CopyPrimaryColor();
            e.SuppressKeyPress = true;
        }
    }

    internal void UpdatePaletteList(List<ColorDetails> palette, string statusMessage)
    {
        _currentPalette = palette;
        _paletteGrid.SetPalette(palette, _settings.ActiveFormats);
        _paletteCountLabel.Text = $"YOUR PALETTE   /   {palette.Count:N0} {(palette.Count == 1 ? "COLOR" : "COLORS")}";
        _paletteStrip.Colors = palette;
        _paletteStrip.Invalidate();
        if (palette.Count > 0)
        {
            DisplaySelectedColor(palette[0]);
        }
        _statusLabel.Text = statusMessage;
    }

    private void DisplaySelectedColor(ColorDetails color)
    {
        _colorPreview.ColorDetails = color;
        _colorPreview.AccessibleName = $"Selected color {color.Hex}, {color.Percentage:F1} percent";
        foreach (var format in _settings.ActiveFormats)
        {
            if (_formatTextBoxes.TryGetValue(format, out var box))
            {
                box.Text = color.GetValue(format);
                _toolTips.SetToolTip(box, box.Text);
            }
        }
    }

    private void CopyPrimaryColor()
    {
        if (_paletteGrid.SelectedColor is not { } color)
        {
            return;
        }
        var format = _settings.ActiveFormats[0];
        CopyTextToClipboard(color.GetValue(format), $"Copied {ColorDetails.GetFormatLabel(format)}: {color.GetValue(format)}");
    }

    private void CopySingleColorSummary()
    {
        if (_currentPalette.Count == 0)
        {
            return;
        }
        int selectedIndex = Math.Max(0, _paletteGrid.SelectedIndex);
        var color = _currentPalette[selectedIndex];

        string summary = PaletteExporter.FormatSingleColorSummary(color, _settings.ActiveFormats);
        CopyTextToClipboard(summary, $"Copied {_settings.ActiveFormats.Count} active format(s) to clipboard!");
    }

    private void CopyEntirePalette()
    {
        if (_currentPalette.Count == 0)
        {
            return;
        }
        var exportMode = (PaletteExportMode)_exportFormatSelect.SelectedIndex;
        string exportedText = PaletteExporter.FormatEntirePalette(_currentPalette, _settings.ActiveFormats, exportMode);

        CopyTextToClipboard(exportedText, $"Copied {_currentPalette.Count} colors to clipboard!");
    }

    private void CopyTextToClipboard(string text, string statusNotification)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        try
        {
            Clipboard.SetText(text);
            _statusLabel.Text = $"✔ {statusNotification}";
        }
        catch (ExternalException)
        {
            _statusLabel.Text = "Clipboard is busy. Please try copying again.";
        }
    }
}
