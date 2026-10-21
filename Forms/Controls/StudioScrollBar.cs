using System.Drawing.Drawing2D;

namespace ChromaTray.Forms.Controls;

internal sealed class StudioScrollBar : Control
{
    private int _value;
    private int _content;
    private int _viewport;
    private bool _hover;
    private int _dragY;
    private int _dragValue;
    public event EventHandler? ValueChanged;
    public int Maximum => Math.Max(0, _content - _viewport);
    public int Value
    {
        get => _value;
        set
        {
            int next = Math.Clamp(value, 0, Maximum);
            if (next == _value) return;
            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public StudioScrollBar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = false;
        BackColor = StudioTheme.Surface;
        AccessibleRole = AccessibleRole.ScrollBar;
        AccessibleName = "Scroll colors";
    }
    public void SetMetrics(int content, int viewport)
    {
        _content = Math.Max(0, content);
        _viewport = Math.Max(0, viewport);
        Value = _value;
        Invalidate();
    }
    internal Rectangle ThumbBounds
    {
        get
        {
            int inset = Math.Max(2, StudioTheme.Dpi(this) / 32);
            int track = Math.Max(1, Height - inset * 2);
            int length = _content <= 0 ? track : Math.Clamp((int)((long)track * _viewport / _content), Math.Min(track, 28 * StudioTheme.Dpi(this) / 96), track);
            int top = inset + (Maximum == 0 ? 0 : (int)((long)(track - length) * Value / Maximum));
            int width = Math.Max(4, (_hover || Capture ? 8 : 6) * StudioTheme.Dpi(this) / 96);
            return new((Width - width) / 2, top, width, length);
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Maximum <= 0) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = StudioTheme.Round(ThumbBounds, Math.Max(2, Width / 3));
        using var brush = new SolidBrush(_hover || Capture ? StudioTheme.Mint : StudioTheme.Accent);
        e.Graphics.FillPath(brush, path);
    }
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        if (ThumbBounds.Contains(e.Location)) { _dragY = e.Y; _dragValue = Value; Capture = true; }
        else Value += e.Y < ThumbBounds.Top ? -_viewport : _viewport;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!Capture) return;
        int travel = Math.Max(1, Height - 2 * Math.Max(2, StudioTheme.Dpi(this) / 32) - ThumbBounds.Height);
        Value = _dragValue + (int)((long)(e.Y - _dragY) * Maximum / travel);
    }
    protected override void OnMouseUp(MouseEventArgs e) { Capture = false; Invalidate(); base.OnMouseUp(e); }
}

internal sealed class StudioScrollViewport : Panel
{
    private readonly Panel _viewport = new() { BackColor = StudioTheme.Surface };
    private readonly StudioScrollBar _scroll = new();
    private Control? _content;
    private bool _layingOut;
    private int _wheelDelta;
    public Control? Content
    {
        get => _content;
        set
        {
            if (_content is not null) { _content.SizeChanged -= ContentResized; _viewport.Controls.Remove(_content); }
            _content = value;
            if (value is not null)
            {
                value.Dock = DockStyle.None;
                _viewport.Controls.Add(value);
                value.SizeChanged += ContentResized;
            }
            PerformLayout();
        }
    }
    public StudioScrollViewport()
    {
        DoubleBuffered = true;
        BackColor = StudioTheme.Surface;
        Margin = Padding.Empty;
        Controls.Add(_viewport);
        Controls.Add(_scroll);
        _scroll.ValueChanged += (_, _) => { if (_content is not null) _content.Top = -_scroll.Value; };
    }
    private void ContentResized(object? sender, EventArgs e) { if (!_layingOut) PerformLayout(); }
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_layingOut || _content is null) return;
        _layingOut = true;
        try
        {
            int barWidth = Math.Max(12, 14 * StudioTheme.Dpi(this) / 96);
            int preferred = _content.GetPreferredSize(new Size(Math.Max(1, Width - barWidth), 0)).Height;
            bool needed = preferred > Height;
            int contentWidth = Math.Max(1, Width - (needed ? barWidth : 0));
            _viewport.Bounds = new Rectangle(0, 0, contentWidth, Height);
            _scroll.Bounds = new Rectangle(contentWidth, 0, barWidth, Height);
            _scroll.Visible = needed;
            _scroll.SetMetrics(preferred, Height);
            _content.Bounds = new Rectangle(0, -_scroll.Value, contentWidth, preferred);
        }
        finally { _layingOut = false; }
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _wheelDelta += e.Delta;
        int ticks = _wheelDelta / SystemInformation.MouseWheelScrollDelta;
        _wheelDelta %= SystemInformation.MouseWheelScrollDelta;
        _scroll.Value -= ticks * Math.Max(24, 48 * StudioTheme.Dpi(this) / 96);
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        base.OnMouseWheel(e);
    }
}
