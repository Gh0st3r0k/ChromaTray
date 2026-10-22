using System.Drawing.Drawing2D;

namespace ChromaTray.Forms.Controls;

/// <summary>
/// A rounded panel that paints its own fill inside the curve and preserves the parent background outside it.
/// </summary>
internal class SurfacePanel : Panel
{
    public Color FillColor { get; set; } = StudioTheme.Surface;

    public Color EdgeColor { get; set; } = StudioTheme.Border;

    public int Radius { get; set; } = 14;

    public SurfacePanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.Transparent;
        Padding = new Padding(20);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = StudioTheme.Dpi(this) / 96f;
        using var path = StudioTheme.Round(new RectangleF(.5f, .5f, Width - 1f, Height - 1f), Radius * scale);
        using var fill = new SolidBrush(FillColor);
        using var border = new Pen(EdgeColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        base.OnPaint(e);
    }
}
