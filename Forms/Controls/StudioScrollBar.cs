using System.Drawing.Drawing2D;

namespace ChromaTray.Forms.Controls;

/// <summary>
/// A themed scrollbar whose value is the content offset in physical pixels.
/// </summary>
internal sealed class StudioScrollBar : Control
{
    private int _value;
    private int _contentHeight;
    private int _viewportHeight;
    private bool _isHovered;
    private int _dragStartY;
    private int _dragStartValue;

    public event EventHandler? ValueChanged;

    public int Maximum => Math.Max(0, _contentHeight - _viewportHeight);

    public int Value
    {
        get => _value;
        set
        {
            int next = Math.Clamp(value, 0, Maximum);
            if (next == _value)
            {
                return;
            }
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

    public void SetMetrics(int contentHeight, int viewportHeight)
    {
        _contentHeight = Math.Max(0, contentHeight);
        _viewportHeight = Math.Max(0, viewportHeight);
        Value = _value;
        Invalidate();
    }

    internal Rectangle ThumbBounds
    {
        get
        {
            int inset = Math.Max(2, StudioTheme.Dpi(this) / 32);
            int trackHeight = Math.Max(1, Height - inset * 2);
            int minimumThumbHeight = Math.Min(trackHeight, 28 * StudioTheme.Dpi(this) / 96);
            int thumbHeight = _contentHeight <= 0
                ? trackHeight
                : Math.Clamp((int)((long)trackHeight * _viewportHeight / _contentHeight), minimumThumbHeight, trackHeight);
            int thumbTop = inset + (Maximum == 0 ? 0 : (int)((long)(trackHeight - thumbHeight) * Value / Maximum));
            int thumbWidth = Math.Max(4, (_isHovered || Capture ? 8 : 6) * StudioTheme.Dpi(this) / 96);
            return new((Width - thumbWidth) / 2, thumbTop, thumbWidth, thumbHeight);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Maximum <= 0)
        {
            return;
        }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = StudioTheme.Round(ThumbBounds, Math.Max(2, Width / 3));
        using var brush = new SolidBrush(_isHovered || Capture ? StudioTheme.Mint : StudioTheme.Accent);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }
        if (ThumbBounds.Contains(e.Location))
        {
            _dragStartY = e.Y;
            _dragStartValue = Value;
            Capture = true;
        }
        else
        {
            Value += e.Y < ThumbBounds.Top ? -_viewportHeight : _viewportHeight;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!Capture)
        {
            return;
        }
        int travel = Math.Max(1, Height - 2 * Math.Max(2, StudioTheme.Dpi(this) / 32) - ThumbBounds.Height);
        Value = _dragStartValue + (int)((long)(e.Y - _dragStartY) * Maximum / travel);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        Capture = false;
        Invalidate();
        base.OnMouseUp(e);
    }
}
