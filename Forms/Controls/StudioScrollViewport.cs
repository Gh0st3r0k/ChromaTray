namespace ChromaTray.Forms.Controls;

/// <summary>
/// Clips inspector content and moves it using a themed scrollbar.
/// </summary>
internal sealed class StudioScrollViewport : Panel
{
    private readonly Panel _viewport = new()
    {
        BackColor = StudioTheme.Surface
    };
    private readonly StudioScrollBar _scrollBar = new();
    private Control? _content;
    private bool _isUpdatingLayout;
    private int _wheelDelta;

    public Control? Content
    {
        get => _content;
        set
        {
            if (_content is not null)
            {
                _content.SizeChanged -= OnContentSizeChanged;
                _viewport.Controls.Remove(_content);
            }
            _content = value;
            if (value is not null)
            {
                value.Dock = DockStyle.None;
                _viewport.Controls.Add(value);
                value.SizeChanged += OnContentSizeChanged;
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
        Controls.Add(_scrollBar);
        _scrollBar.ValueChanged += (_, _) =>
        {
            if (_content is not null)
            {
                _content.Top = -_scrollBar.Value;
            }
        };
    }

    private void OnContentSizeChanged(object? sender, EventArgs e)
    {
        if (!_isUpdatingLayout)
        {
            PerformLayout();
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_isUpdatingLayout || _content is null)
        {
            return;
        }
        _isUpdatingLayout = true;
        try
        {
            int scrollBarWidth = Math.Max(12, 14 * StudioTheme.Dpi(this) / 96);
            int contentHeight = _content.GetPreferredSize(new Size(Math.Max(1, Width - scrollBarWidth), 0)).Height;
            bool needsScrollBar = contentHeight > Height;
            int contentWidth = Math.Max(1, Width - (needsScrollBar ? scrollBarWidth : 0));
            _viewport.Bounds = new Rectangle(0, 0, contentWidth, Height);
            _scrollBar.Bounds = new Rectangle(contentWidth, 0, scrollBarWidth, Height);
            _scrollBar.Visible = needsScrollBar;
            _scrollBar.SetMetrics(contentHeight, Height);
            _content.Bounds = new Rectangle(0, -_scrollBar.Value, contentWidth, contentHeight);
        }
        finally
        {
            _isUpdatingLayout = false;
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _wheelDelta += e.Delta;
        int ticks = _wheelDelta / SystemInformation.MouseWheelScrollDelta;
        _wheelDelta %= SystemInformation.MouseWheelScrollDelta;
        _scrollBar.Value -= ticks * Math.Max(24, 48 * StudioTheme.Dpi(this) / 96);
        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }
        base.OnMouseWheel(e);
    }
}
