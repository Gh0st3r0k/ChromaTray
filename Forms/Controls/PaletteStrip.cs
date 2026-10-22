using ChromaTray.Models;

namespace ChromaTray.Forms.Controls;

/// <summary>
/// Полоса частот: ширина каждого сегмента пропорциональна числу пикселей цвета.
/// </summary>
internal sealed class PaletteStrip : Control
{
    public IReadOnlyList<ColorDetails> Colors { get; set; } = [];

    public PaletteStrip()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = StudioTheme.Surface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        double total = Colors.Sum(color => (double)color.PixelCount);
        if (total <= 0)
        {
            return;
        }
        double x = 0;
        foreach (var color in Colors)
        {
            double next = x + Width * color.PixelCount / total;
            using var brush = new SolidBrush(color.RawColor);
            e.Graphics.FillRectangle(brush, (int)x, 0, Math.Max(1, (int)Math.Ceiling(next) - (int)x), Height);
            x = next;
        }
    }
}
