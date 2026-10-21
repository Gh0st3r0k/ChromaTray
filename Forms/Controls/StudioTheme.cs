using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChromaTray.Forms.Controls;

internal static class StudioTheme
{
    // Reference palette: #161D6F / #0B2F9F / #98DED9 / #C7FFD8.
    public static readonly Color Canvas = Color.FromArgb(22, 29, 111);
    public static readonly Color Surface = Color.FromArgb(25, 40, 121);
    public static readonly Color Raised = Color.FromArgb(11, 47, 159);
    public static readonly Color Border = Color.FromArgb(65, 91, 168);
    public static readonly Color Text = Color.FromArgb(240, 255, 249);
    public static readonly Color Muted = Color.FromArgb(165, 196, 222);
    public static readonly Color Accent = Color.FromArgb(152, 222, 217);
    public static readonly Color Mint = Color.FromArgb(199, 255, 216);
    public static readonly Color Danger = Color.FromArgb(255, 184, 184);
    public static readonly Font Body = new("Segoe UI", 10f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font Strong = new("Segoe UI Semibold", 10f);
    public static readonly Font Heading = new("Segoe UI Semibold", 16f);
    public static readonly Font Title = new("Segoe UI Semibold", 24f);
    public static readonly Font Mono = new("Consolas", 10f);
    public static readonly Font MonoLarge = new("Consolas", 24f, FontStyle.Bold);

    // A nested control can retain its previous cached DPI during handle creation
    // or WM_DPICHANGED. Layout and drawing must share the owning window's DPI.
    public static int Dpi(Control control) => control.FindForm()?.DeviceDpi ?? control.DeviceDpi;

    public static GraphicsPath Round(RectangleF rect, float radius = 12)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        if (diameter <= 0) return path;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Label Label(string text, Font? font = null, Color? color = null) => new()
    {
        Text = text, Font = font ?? Body, ForeColor = color ?? Text,
        AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent, AutoEllipsis = true, Margin = Padding.Empty
    };

    public static DpiStackPanel Stack(params (Control Control, int Height)[] rows)
    {
        var stack = new DpiStackPanel(rows.Select(row => row.Height).ToArray())
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows.Length,
            Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.Transparent
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < rows.Length; i++)
        {
            var (control, height) = rows[i];
            stack.RowStyles.Add(height < 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, height));
            control.Dock = DockStyle.Fill;
            stack.Controls.Add(control, 0, i);
        }
        return stack;
    }

    public static void ApplyWindowChrome(Form form)
    {
        if (!OperatingSystem.IsWindows()) return;
        int dark = 1;
        _ = DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int));
        int corners = 2;
        _ = DwmSetWindowAttribute(form.Handle, 33, ref corners, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}

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

internal enum StudioGlyph { None, Point, Region, Settings, Copy }

internal sealed class StudioButton : Button
{
    public Color AccentColor { get; set; } = StudioTheme.Accent;
    public bool Primary { get; set; }
    public StudioGlyph Glyph { get; set; }
    public string Shortcut { get; set; } = "";
    public string Description { get; set; } = "";
    private bool _hover;
    private bool _pressed;

    public StudioButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = StudioTheme.Strong;
        ForeColor = StudioTheme.Text;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Height = 40;
        Margin = Padding.Empty;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } base.OnKeyDown(e); }
    protected override void OnKeyUp(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { _pressed = false; Invalidate(); } base.OnKeyUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = StudioTheme.Dpi(this) / 96f;
        int S(int value) => (int)Math.Round(value * scale);
        bool card = Description.Length > 0;
        Color fill = Primary ? AccentColor : _hover ? StudioTheme.Raised : StudioTheme.Surface;
        if (_pressed) fill = ControlPaint.Dark(fill, .1f);
        Color foreground = !Enabled ? StudioTheme.Muted : Primary ? StudioTheme.Canvas : StudioTheme.Text;
        using var path = StudioTheme.Round(new RectangleF(1, 1, Width - 2, Height - 2), S(card ? 14 : 9));
        using var brush = new SolidBrush(fill);
        using var border = new Pen(Focused || _hover ? AccentColor : StudioTheme.Border, Focused ? 2 : 1);
        g.FillPath(brush, path);
        g.DrawPath(border, path);

        int left = S(card ? 18 : 10);
        if (Glyph != StudioGlyph.None)
        {
            int glyphSize = S(card ? 22 : 16);
            var icon = new Rectangle(left + S(card ? 4 : 0), card ? S(20) : (Height - glyphSize) / 2, glyphSize, glyphSize);
            DrawGlyph(g, icon, Glyph, Primary ? StudioTheme.Canvas : AccentColor, scale);
            left += glyphSize + S(card ? 18 : 9);
        }
        int chipWidth = Shortcut.Length == 0 ? 0 : TextRenderer.MeasureText(Shortcut, StudioTheme.Small).Width + S(18);
        var textRect = new Rectangle(left, card ? S(16) : 0, Math.Max(0, Width - left - chipWidth - S(card ? 22 : 10)), card ? S(28) : Height);
        TextRenderer.DrawText(g, Text, Font, textRect, foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (card)
            TextRenderer.DrawText(g, Description, StudioTheme.Small,
                new Rectangle(left, S(45), Math.Max(0, Width - left - S(16)), S(25)),
                Primary ? StudioTheme.Canvas : StudioTheme.Muted,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        if (chipWidth > 0)
        {
            var chip = new Rectangle(Width - chipWidth - S(16), card ? S(19) : (Height - S(24)) / 2, chipWidth, S(24));
            using var chipPath = StudioTheme.Round(chip, S(5));
            using var chipFill = new SolidBrush(Color.FromArgb(Primary ? 24 : 12, Primary ? Color.Black : Color.White));
            g.FillPath(chipFill, chipPath);
            TextRenderer.DrawText(g, Shortcut, StudioTheme.Small, chip, foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        if (Focused && ShowFocusCues)
        {
            var focus = ClientRectangle;
            focus.Inflate(-S(5), -S(5));
            ControlPaint.DrawFocusRectangle(g, focus, foreground, fill);
        }
    }

    internal static void DrawGlyph(Graphics g, Rectangle rect, StudioGlyph glyph, Color color, float scale)
    {
        using var pen = new Pen(color, Math.Max(1.4f, 1.6f * scale)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
        switch (glyph)
        {
            case StudioGlyph.Point:
                g.DrawEllipse(pen, x + w * .2f, y + h * .2f, w * .6f, h * .6f);
                g.DrawLine(pen, x + w / 2, y, x + w / 2, y + h * .3f);
                g.DrawLine(pen, x + w / 2, y + h * .7f, x + w / 2, y + h);
                g.DrawLine(pen, x, y + h / 2, x + w * .3f, y + h / 2);
                g.DrawLine(pen, x + w * .7f, y + h / 2, x + w, y + h / 2);
                break;
            case StudioGlyph.Region:
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, x + 1, y + 1, w - 2, h - 2);
                break;
            case StudioGlyph.Copy:
                g.DrawRectangle(pen, x + w * .3f, y + h * .1f, w * .6f, h * .6f);
                g.DrawLines(pen, new PointF[] { new(x + w * .1f, y + h * .3f), new(x + w * .1f, y + h * .9f), new(x + w * .7f, y + h * .9f) });
                break;
            case StudioGlyph.Settings:
                g.DrawEllipse(pen, x + w * .2f, y + h * .2f, w * .6f, h * .6f);
                g.DrawEllipse(pen, x + w * .43f, y + h * .43f, w * .14f, h * .14f);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    g.DrawLine(pen, x + w * (.5f + .33f * (float)Math.Cos(a)), y + h * (.5f + .33f * (float)Math.Sin(a)),
                        x + w * (.5f + .46f * (float)Math.Cos(a)), y + h * (.5f + .46f * (float)Math.Sin(a)));
                }
                break;
        }
    }
}

internal sealed class ColorPreview : Control
{
    private ChromaTray.Models.ColorDetails? _color;
    public ChromaTray.Models.ColorDetails? ColorDetails { get => _color; set { _color = value; Invalidate(); } }

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
        if (_color is null) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int S(int value) => (int)Math.Round(value * StudioTheme.Dpi(this) / 96f);
        using var path = StudioTheme.Round(new RectangleF(0, 0, Width - 1, Height - 1), S(12));
        using var brush = new SolidBrush(_color.RawColor);
        g.FillPath(brush, path);
        var c = _color.RawColor;
        Color ink = .2126 * c.R + .7152 * c.G + .0722 * c.B > 155 ? Color.FromArgb(20, 24, 34) : Color.White;
        TextRenderer.DrawText(g, "CURRENT COLOR", StudioTheme.Small, new Rectangle(S(18), S(14), Width - S(36), S(26)), ink);
        TextRenderer.DrawText(g, _color.Hex, StudioTheme.MonoLarge,
            new Rectangle(S(16), Height - S(74), Width - S(32), S(46)), ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, $"{_color.Percentage:F1}% of captured pixels", StudioTheme.Small,
            new Rectangle(S(18), Height - S(29), Width - S(36), S(24)), ink);
    }
}

internal sealed class PaletteStrip : Control
{
    public IReadOnlyList<ChromaTray.Models.ColorDetails> Colors { get; set; } = [];
    public PaletteStrip() { DoubleBuffered = true; ResizeRedraw = true; BackColor = StudioTheme.Surface; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        double total = Colors.Sum(color => (double)color.PixelCount);
        if (total <= 0) return;
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
