using System.Drawing.Drawing2D;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms.Controls;

// Рисуем строки и прокрутку самостоятельно, чтобы Windows не добавляла белую рамку ListView.
/// <summary>
/// Таблица палитры с собственной прокруткой. Рисует только видимые строки, а не сотни дочерних контролов.
/// </summary>
internal sealed class PaletteGrid : Control
{
    private readonly StudioScrollBar _scrollBar = new();
    private readonly ToolTip _toolTips = new();
    private IReadOnlyList<ColorDetails> _colors = [];
    private ColorFormat[] _formats = [ColorFormat.Hex];
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private int _wheelDelta;

    public event EventHandler? SelectedIndexChanged;

    public event EventHandler? ColorActivated;

    public IReadOnlyList<ColorDetails> Colors => _colors;

    public ColorDetails? SelectedColor => _selectedIndex >= 0 ? _colors[_selectedIndex] : null;

    internal int ScrollOffset => _scrollBar.Value;

    internal StudioScrollBar ScrollBar => _scrollBar;

    private int Scale(int value) => (int)Math.Round(value * StudioTheme.Dpi(this) / 96f);

    private int HeaderHeight => Scale(Height < Scale(180) ? 28 : 36);

    private int RowHeight => Scale(Height < Scale(180) ? 44 : 48);

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _colors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            if (_selectedIndex == value)
            {
                return;
            }
            _selectedIndex = value;
            EnsureSelectedVisible();
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public PaletteGrid()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        BackColor = StudioTheme.Surface;
        ForeColor = StudioTheme.Text;
        Font = StudioTheme.Mono;
        AccessibleRole = AccessibleRole.List;
        AccessibleName = "Captured color palette";
        AccessibleDescription = "Arrow keys select colors. Enter or double-click copies. Control+C copies the selected color.";
        Controls.Add(_scrollBar);
        _scrollBar.ValueChanged += (_, _) =>
        {
            _hoverIndex = -1;
            _toolTips.SetToolTip(this, null);
            Invalidate();
        };
    }

    public void SetPalette(IReadOnlyList<ColorDetails> colors, IReadOnlyList<ColorFormat> formats)
    {
        _colors = colors;
        _formats = formats.ToArray();
        _selectedIndex = colors.Count > 0 ? 0 : -1;
        _hoverIndex = -1;
        _scrollBar.Value = 0;
        PerformLayout();
        Invalidate();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    internal string TooltipFor(int index) => PaletteExporter.FormatSingleColorSummary(_colors[index], _formats);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        _scrollBar.Bounds = new Rectangle(Math.Max(0, Width - Scale(14)), HeaderHeight, Scale(14), Math.Max(0, Height - HeaderHeight));
        _scrollBar.SetMetrics(_colors.Count * RowHeight, Math.Max(0, Height - HeaderHeight));
        _scrollBar.Visible = _scrollBar.Maximum > 0;
    }

    internal void ScrollBy(int pixels) => _scrollBar.Value += pixels;

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0)
        {
            return;
        }
        int top = _selectedIndex * RowHeight;
        int viewport = Math.Max(1, Height - HeaderHeight);
        if (top < _scrollBar.Value)
        {
            _scrollBar.Value = top;
        }
        else if (top + RowHeight > _scrollBar.Value + viewport)
        {
            _scrollBar.Value = top + RowHeight - viewport;
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        // На плавной мыши Delta бывает меньше одного шага; сохраняем остаток между событиями.
        _wheelDelta += e.Delta;
        int ticks = _wheelDelta / SystemInformation.MouseWheelScrollDelta;
        _wheelDelta %= SystemInformation.MouseWheelScrollDelta;
        int lines = SystemInformation.MouseWheelScrollLines;
        ScrollBy(-ticks * (lines < 0 ? Math.Max(RowHeight, Height - HeaderHeight) : Math.Max(1, lines) * RowHeight));
        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }
        base.OnMouseWheel(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || e.Y < HeaderHeight)
        {
            return;
        }
        Focus();
        int row = (e.Y - HeaderHeight + _scrollBar.Value) / RowHeight;
        if (row < _colors.Count)
        {
            SelectedIndex = row;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int row = e.Y < HeaderHeight ? -1 : (e.Y - HeaderHeight + _scrollBar.Value) / RowHeight;
        if (row >= _colors.Count)
        {
            row = -1;
        }
        if (row == _hoverIndex)
        {
            return;
        }
        _hoverIndex = row;
        _toolTips.SetToolTip(this, row >= 0 ? TooltipFor(row) : null);
    }

    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
        if (SelectedColor is not null)
        {
            ColorActivated?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageDown or Keys.PageUp or Keys.Enter || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_colors.Count > 0)
        {
            int page = Math.Max(1, (Height - HeaderHeight) / RowHeight);
            int index = e.KeyCode switch
            {
                Keys.Up => Math.Max(0, SelectedIndex - 1),
                Keys.Down => Math.Min(_colors.Count - 1, SelectedIndex + 1),
                Keys.Home => 0,
                Keys.End => _colors.Count - 1,
                Keys.PageUp => Math.Max(0, SelectedIndex - page),
                Keys.PageDown => Math.Min(_colors.Count - 1, SelectedIndex + page),
                _ => SelectedIndex
            };
            if (index != SelectedIndex)
            {
                SelectedIndex = index;
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Enter)
            {
                ColorActivated?.Invoke(this, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }
        base.OnKeyDown(e);
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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        int width = Width - (_scrollBar.Visible ? _scrollBar.Width : 0);
        bool showPixels = width >= Scale(480);
        int pixelsWidth = showPixels ? Scale(94) : 0;
        int shareWidth = Scale(132);
        int shareX = Math.Max(Scale(180), width - pixelsWidth - shareWidth);
        int valueX = Scale(52);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(graphics, ColorDetails.GetFormatLabel(_formats[0]), StudioTheme.Small,
            new Rectangle(valueX, 0, Math.Max(0, shareX - valueX), HeaderHeight), StudioTheme.Muted, flags);
        TextRenderer.DrawText(graphics, "SHARE", StudioTheme.Small, new Rectangle(shareX + Scale(8), 0, shareWidth, HeaderHeight), StudioTheme.Muted, flags);
        if (showPixels)
        {
            TextRenderer.DrawText(graphics, "PIXELS", StudioTheme.Small,
                    new Rectangle(width - pixelsWidth, 0, pixelsWidth - Scale(8), HeaderHeight), StudioTheme.Muted, flags | TextFormatFlags.Right);
        }
        using var divider = new Pen(StudioTheme.Border);
        graphics.DrawLine(divider, 0, HeaderHeight - 1, width, HeaderHeight - 1);
        var state = graphics.Save();
        graphics.SetClip(new Rectangle(0, HeaderHeight, Math.Max(0, width), Math.Max(0, Height - HeaderHeight)));
        // Вычисляем диапазон видимых строк вместо перерисовки всей палитры.
        int start = _scrollBar.Value / RowHeight;
        int end = Math.Min(_colors.Count, start + (Height - HeaderHeight) / RowHeight + 2);
        for (int i = start; i < end; i++)
        {
            var color = _colors[i];
            int y = HeaderHeight + i * RowHeight - _scrollBar.Value;
            bool selected = i == SelectedIndex;
            using var background = new SolidBrush(selected ? StudioTheme.Raised : StudioTheme.Surface);
            graphics.FillRectangle(background, 0, y, width, RowHeight);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var swatch = new Rectangle(Scale(12), y + (RowHeight - Scale(28)) / 2, Scale(28), Scale(28));
            using var path = StudioTheme.Round(swatch, Scale(7));
            using var fill = new SolidBrush(color.RawColor);
            using var edge = new Pen(StudioTheme.Accent);
            graphics.FillPath(fill, path);
            graphics.DrawPath(edge, path);
            if (selected)
            {
                using var indicator = new SolidBrush(StudioTheme.Mint);
                graphics.FillRectangle(indicator, 0, y + Scale(10), Scale(3), RowHeight - Scale(20));
            }
            TextRenderer.DrawText(graphics, color.GetValue(_formats[0]), Font,
                new Rectangle(valueX, y, Math.Max(0, shareX - valueX - Scale(6)), RowHeight), StudioTheme.Text, flags);
            TextRenderer.DrawText(graphics, $"{color.Percentage:F1}%", StudioTheme.Small,
                new Rectangle(shareX + Scale(8), y, Scale(52), RowHeight), StudioTheme.Text, flags);
            var track = new Rectangle(shareX + Scale(65), y + RowHeight / 2 - Scale(2), Scale(52), Scale(4));
            using var trackBrush = new SolidBrush(StudioTheme.Border);
            using var shareBrush = new SolidBrush(selected ? StudioTheme.Mint : StudioTheme.Accent);
            graphics.FillRectangle(trackBrush, track);
            graphics.FillRectangle(shareBrush, track.X, track.Y, (int)(track.Width * Math.Clamp(color.Percentage, 0, 100) / 100), track.Height);
            if (showPixels)
            {
                TextRenderer.DrawText(graphics, $"{color.PixelCount:N0}", StudioTheme.Mono,
                            new Rectangle(width - pixelsWidth, y, pixelsWidth - Scale(8), RowHeight), StudioTheme.Muted, flags | TextFormatFlags.Right);
            }
            if (selected && Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(graphics, new Rectangle(valueX - 2, y + 3, Math.Max(1, width - valueX), RowHeight - 6), StudioTheme.Mint, StudioTheme.Raised);
            }
        }
        graphics.Restore(state);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTips.Dispose();
        }
        base.Dispose(disposing);
    }
}
