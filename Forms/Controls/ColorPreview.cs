using ChromaTray.Models;
using System.Drawing.Drawing2D;

namespace ChromaTray.Forms.Controls;

/// <summary>
/// Displays the selected color, HEX value, and pixel share with text contrast chosen from background brightness.
/// </summary>
internal sealed class ColorPreview : Control
{
    private ColorDetails? _color;

    /// <summary>Whether to display capture statistics below the color value.</summary>
    public bool ShowPixelShare { get; set; } = true;

    /// <summary>Whether to draw text inside the swatch, in addition to its fill.</summary>
    public bool ShowDetails { get; set; } = true;

    public ColorDetails? ColorDetails
    {
        get => _color; set
        {
            _color = value;
            Invalidate();
        }
    }

    public ColorPreview()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_color is null)
        {
            return;
        }
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int Scale(int value) => (int)Math.Round(value * StudioTheme.Dpi(this) / 96f);
        using var path = StudioTheme.Round(new RectangleF(0, 0, Width - 1, Height - 1), Scale(12));
        using var brush = new SolidBrush(_color.RawColor);
        graphics.FillPath(brush, path);
        if (!ShowDetails)
        {
            return;
        }
        var c = _color.RawColor;
        Color ink = .2126 * c.R + .7152 * c.G + .0722 * c.B > 155 ? Color.FromArgb(20, 24, 34) : Color.White;
        TextRenderer.DrawText(graphics, "CURRENT COLOR", StudioTheme.Small, new Rectangle(Scale(18), Scale(14), Width - Scale(36), Scale(26)), ink);
        TextRenderer.DrawText(graphics, _color.Hex, StudioTheme.MonoLarge,
            new Rectangle(Scale(16), Height - Scale(74), Width - Scale(32), Scale(46)), ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        if (ShowPixelShare)
        {
            TextRenderer.DrawText(graphics, $"{_color.Percentage:F1}% of captured pixels", StudioTheme.Small,
                new Rectangle(Scale(18), Height - Scale(29), Width - Scale(36), Scale(24)), ink);
        }
    }
}
