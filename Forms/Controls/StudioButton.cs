using System.Drawing.Drawing2D;

namespace ChromaTray.Forms.Controls;
internal enum StudioGlyph
{
    None,
    Point,
    Region,
    Settings,
    Copy
}

/// <summary>
/// Paints a rounded button and its interaction states while retaining WinForms keyboard behavior.
/// </summary>
internal sealed class StudioButton : Button
{
    public Color AccentColor { get; set; } = StudioTheme.Accent;

    public bool Primary
    {
        get; set;
    }

    public StudioGlyph Glyph
    {
        get; set;
    }

    public string Shortcut { get; set; } = "";

    public string Description { get; set; } = "";

    private bool _isHovered;
    private bool _isPressed;

    public StudioButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        // ButtonBase ожидает непрозрачный прямоугольник. Для скруглений нужно сначала
        // нарисовать фон родителя, иначе в углах останутся посторонние цвета.
        SetStyle(ControlStyles.Opaque, false);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = StudioTheme.Strong;
        ForeColor = StudioTheme.Text;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Height = 40;
        Margin = Padding.Empty;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = _isPressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _isPressed = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isPressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _isPressed = true;
            Invalidate();
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _isPressed = false;
            Invalidate();
        }

        base.OnKeyUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = StudioTheme.Dpi(this) / 96f;
        int Scale(int value) => (int)Math.Round(value * scale);
        bool card = Description.Length > 0;
        Color fill = Primary ? AccentColor : _isHovered ? StudioTheme.Raised : StudioTheme.Surface;
        if (_isPressed)
        {
            fill = ControlPaint.Dark(fill, .1f);
        }

        Color foreground = !Enabled ? StudioTheme.Muted : Primary ? StudioTheme.Canvas : StudioTheme.Text;
        using var path = StudioTheme.Round(new RectangleF(1, 1, Width - 2, Height - 2), Scale(card ? 14 : 9));
        using var brush = new SolidBrush(fill);
        using var border = new Pen(Focused || _isHovered ? AccentColor : StudioTheme.Border, Focused ? 2 : 1);
        graphics.FillPath(brush, path);
        graphics.DrawPath(border, path);
        int left = Scale(card ? 18 : 10);
        if (Glyph != StudioGlyph.None)
        {
            int glyphSize = Scale(card ? 22 : 16);
            var icon = new Rectangle(left + Scale(card ? 4 : 0), card ? Scale(20) : (Height - glyphSize) / 2, glyphSize, glyphSize);
            DrawGlyph(graphics, icon, Glyph, Primary ? StudioTheme.Canvas : AccentColor, scale);
            left += glyphSize + Scale(card ? 18 : 9);
        }

        int chipWidth = Shortcut.Length == 0 ? 0 : TextRenderer.MeasureText(Shortcut, StudioTheme.Small).Width + Scale(18);
        var textRect = new Rectangle(left, card ? Scale(16) : 0, Math.Max(0, Width - left - chipWidth - Scale(card ? 22 : 10)), card ? Scale(28) : Height);
        TextRenderer.DrawText(
graphics,
Text,
Font,
textRect,
foreground,
TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
);
        if (card)
        {
            TextRenderer.DrawText(
graphics,
Description,
StudioTheme.Small,
new Rectangle(left, Scale(45), Math.Max(0, Width - left - Scale(16)), Scale(25)),
Primary ? StudioTheme.Canvas : StudioTheme.Muted,
TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
);
        }

        if (chipWidth > 0)
        {
            var chip = new Rectangle(Width - chipWidth - Scale(16), card ? Scale(19) : (Height - Scale(24)) / 2, chipWidth, Scale(24));
            using var chipPath = StudioTheme.Round(chip, Scale(5));
            using var chipFill = new SolidBrush(Color.FromArgb(Primary ? 24 : 12, Primary ? Color.Black : Color.White));
            graphics.FillPath(chipFill, chipPath);
            TextRenderer.DrawText(
graphics,
Shortcut,
StudioTheme.Small,
chip,
foreground,
TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
);
        }

        if (Focused && ShowFocusCues)
        {
            var focus = ClientRectangle;
            focus.Inflate(-Scale(5), -Scale(5));
            ControlPaint.DrawFocusRectangle(graphics, focus, foreground, fill);
        }
    }

    internal static void DrawGlyph(Graphics graphics, Rectangle rect, StudioGlyph glyph, Color color, float scale)
    {
        using var pen = new Pen(color, Math.Max(1.4f, 1.6f * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        float x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
        switch (glyph)
        {
            case StudioGlyph.Point:
                graphics.DrawEllipse(pen, x + w * .2f, y + h * .2f, w * .6f, h * .6f);
                graphics.DrawLine(pen, x + w / 2, y, x + w / 2, y + h * .3f);
                graphics.DrawLine(pen, x + w / 2, y + h * .7f, x + w / 2, y + h);
                graphics.DrawLine(pen, x, y + h / 2, x + w * .3f, y + h / 2);
                graphics.DrawLine(pen, x + w * .7f, y + h / 2, x + w, y + h / 2);
                break;
            case StudioGlyph.Region:
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(pen, x + 1, y + 1, w - 2, h - 2);
                break;
            case StudioGlyph.Copy:
                graphics.DrawRectangle(pen, x + w * .3f, y + h * .1f, w * .6f, h * .6f);
                graphics.DrawLines(pen, new PointF[] { new(x + w * .1f, y + h * .3f), new(x + w * .1f, y + h * .9f), new(x + w * .7f, y + h * .9f) });
                break;
            case StudioGlyph.Settings:
                graphics.DrawEllipse(pen, x + w * .2f, y + h * .2f, w * .6f, h * .6f);
                graphics.DrawEllipse(pen, x + w * .43f, y + h * .43f, w * .14f, h * .14f);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    graphics.DrawLine(
pen,
x + w * (.5f + .33f * (float)Math.Cos(a)),
y + h * (.5f + .33f * (float)Math.Sin(a)),
x + w * (.5f + .46f * (float)Math.Cos(a)),
y + h * (.5f + .46f * (float)Math.Sin(a))
);
                }

                break;
        }
    }
}
