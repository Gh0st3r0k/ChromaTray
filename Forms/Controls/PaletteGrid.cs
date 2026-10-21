using System.Drawing.Drawing2D;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms.Controls;

// Virtual rows and a painted scrollbar: no native ListView chrome or white
// non-client scrollbar is left for Windows to paint over the theme.
internal sealed class PaletteGrid : Control
{
    private readonly StudioScrollBar _scroll = new();
    private readonly ToolTip _tips = new();
    private IReadOnlyList<ColorDetails> _colors = [];
    private ColorFormat[] _formats = [ColorFormat.Hex];
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private int _wheelDelta;
    public event EventHandler? SelectedIndexChanged;
    public event EventHandler? ColorActivated;
    public IReadOnlyList<ColorDetails> Colors => _colors;
    public ColorDetails? SelectedColor => _selectedIndex >= 0 ? _colors[_selectedIndex] : null;
    internal int ScrollOffset => _scroll.Value;
    internal StudioScrollBar ScrollBar => _scroll;
    private int S(int value) => (int)Math.Round(value * StudioTheme.Dpi(this) / 96f);
    private int HeaderHeight => S(Height < S(180) ? 28 : 36);
    private int RowHeight => S(Height < S(180) ? 44 : 48);
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _colors.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (_selectedIndex == value) return;
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
        Controls.Add(_scroll);
        _scroll.ValueChanged += (_, _) => { _hoverIndex = -1; _tips.SetToolTip(this, null); Invalidate(); };
    }
    public void SetPalette(IReadOnlyList<ColorDetails> colors, IReadOnlyList<ColorFormat> formats)
    {
        _colors = colors;
        _formats = formats.ToArray();
        _selectedIndex = colors.Count > 0 ? 0 : -1;
        _hoverIndex = -1;
        _scroll.Value = 0;
        PerformLayout();
        Invalidate();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }
    internal string TooltipFor(int index) => PaletteExporter.FormatSingleColorSummary(_colors[index], _formats);
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        _scroll.Bounds = new Rectangle(Math.Max(0, Width - S(14)), HeaderHeight, S(14), Math.Max(0, Height - HeaderHeight));
        _scroll.SetMetrics(_colors.Count * RowHeight, Math.Max(0, Height - HeaderHeight));
        _scroll.Visible = _scroll.Maximum > 0;
    }
    internal void ScrollBy(int pixels) => _scroll.Value += pixels;
    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0) return;
        int top = _selectedIndex * RowHeight;
        int viewport = Math.Max(1, Height - HeaderHeight);
        if (top < _scroll.Value) _scroll.Value = top;
        else if (top + RowHeight > _scroll.Value + viewport) _scroll.Value = top + RowHeight - viewport;
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _wheelDelta += e.Delta;
        int ticks = _wheelDelta / SystemInformation.MouseWheelScrollDelta;
        _wheelDelta %= SystemInformation.MouseWheelScrollDelta;
        int lines = SystemInformation.MouseWheelScrollLines;
        ScrollBy(-ticks * (lines < 0 ? Math.Max(RowHeight, Height - HeaderHeight) : Math.Max(1, lines) * RowHeight));
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        base.OnMouseWheel(e);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || e.Y < HeaderHeight) return;
        Focus();
        int row = (e.Y - HeaderHeight + _scroll.Value) / RowHeight;
        if (row < _colors.Count) SelectedIndex = row;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int row = e.Y < HeaderHeight ? -1 : (e.Y - HeaderHeight + _scroll.Value) / RowHeight;
        if (row >= _colors.Count) row = -1;
        if (row == _hoverIndex) return;
        _hoverIndex = row;
        _tips.SetToolTip(this, row >= 0 ? TooltipFor(row) : null);
    }
    protected override void OnDoubleClick(EventArgs e) { base.OnDoubleClick(e); if (SelectedColor is not null) ColorActivated?.Invoke(this, EventArgs.Empty); }
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageDown or Keys.PageUp or Keys.Enter || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_colors.Count > 0)
        {
            int page = Math.Max(1, (Height - HeaderHeight) / RowHeight);
            int index = e.KeyCode switch
            {
                Keys.Up => Math.Max(0, SelectedIndex - 1), Keys.Down => Math.Min(_colors.Count - 1, SelectedIndex + 1),
                Keys.Home => 0, Keys.End => _colors.Count - 1,
                Keys.PageUp => Math.Max(0, SelectedIndex - page), Keys.PageDown => Math.Min(_colors.Count - 1, SelectedIndex + page),
                _ => SelectedIndex
            };
            if (index != SelectedIndex) { SelectedIndex = index; e.Handled = true; }
            if (e.KeyCode == Keys.Enter) { ColorActivated?.Invoke(this, EventArgs.Empty); e.SuppressKeyPress = true; }
        }
        base.OnKeyDown(e);
    }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        int width = Width - (_scroll.Visible ? _scroll.Width : 0);
        bool showPixels = width >= S(480);
        int pixelsWidth = showPixels ? S(94) : 0;
        int shareWidth = S(132);
        int shareX = Math.Max(S(180), width - pixelsWidth - shareWidth);
        int valueX = S(52);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, ColorDetails.GetFormatLabel(_formats[0]), StudioTheme.Small,
            new Rectangle(valueX, 0, Math.Max(0, shareX - valueX), HeaderHeight), StudioTheme.Muted, flags);
        TextRenderer.DrawText(g, "SHARE", StudioTheme.Small, new Rectangle(shareX + S(8), 0, shareWidth, HeaderHeight), StudioTheme.Muted, flags);
        if (showPixels) TextRenderer.DrawText(g, "PIXELS", StudioTheme.Small,
            new Rectangle(width - pixelsWidth, 0, pixelsWidth - S(8), HeaderHeight), StudioTheme.Muted, flags | TextFormatFlags.Right);
        using var divider = new Pen(StudioTheme.Border);
        g.DrawLine(divider, 0, HeaderHeight - 1, width, HeaderHeight - 1);
        var state = g.Save();
        g.SetClip(new Rectangle(0, HeaderHeight, Math.Max(0, width), Math.Max(0, Height - HeaderHeight)));
        int start = _scroll.Value / RowHeight;
        int end = Math.Min(_colors.Count, start + (Height - HeaderHeight) / RowHeight + 2);
        for (int i = start; i < end; i++)
        {
            var color = _colors[i];
            int y = HeaderHeight + i * RowHeight - _scroll.Value;
            bool selected = i == SelectedIndex;
            using var background = new SolidBrush(selected ? StudioTheme.Raised : StudioTheme.Surface);
            g.FillRectangle(background, 0, y, width, RowHeight);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var swatch = new Rectangle(S(12), y + (RowHeight - S(28)) / 2, S(28), S(28));
            using var path = StudioTheme.Round(swatch, S(7));
            using var fill = new SolidBrush(color.RawColor);
            using var edge = new Pen(StudioTheme.Accent);
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
            if (selected)
            {
                using var indicator = new SolidBrush(StudioTheme.Mint);
                g.FillRectangle(indicator, 0, y + S(10), S(3), RowHeight - S(20));
            }
            TextRenderer.DrawText(g, color.GetValue(_formats[0]), Font,
                new Rectangle(valueX, y, Math.Max(0, shareX - valueX - S(6)), RowHeight), StudioTheme.Text, flags);
            TextRenderer.DrawText(g, $"{color.Percentage:F1}%", StudioTheme.Small,
                new Rectangle(shareX + S(8), y, S(52), RowHeight), StudioTheme.Text, flags);
            var track = new Rectangle(shareX + S(65), y + RowHeight / 2 - S(2), S(52), S(4));
            using var trackBrush = new SolidBrush(StudioTheme.Border);
            using var shareBrush = new SolidBrush(selected ? StudioTheme.Mint : StudioTheme.Accent);
            g.FillRectangle(trackBrush, track);
            g.FillRectangle(shareBrush, track.X, track.Y, (int)(track.Width * Math.Clamp(color.Percentage, 0, 100) / 100), track.Height);
            if (showPixels) TextRenderer.DrawText(g, $"{color.PixelCount:N0}", StudioTheme.Mono,
                new Rectangle(width - pixelsWidth, y, pixelsWidth - S(8), RowHeight), StudioTheme.Muted, flags | TextFormatFlags.Right);
            if (selected && Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(g, new Rectangle(valueX - 2, y + 3, Math.Max(1, width - valueX), RowHeight - 6), StudioTheme.Mint, StudioTheme.Raised);
        }
        g.Restore(state);
    }
    protected override void Dispose(bool disposing) { if (disposing) _tips.Dispose(); base.Dispose(disposing); }
}
