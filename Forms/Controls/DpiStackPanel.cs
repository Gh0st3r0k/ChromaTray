namespace ChromaTray.Forms.Controls;

/// <summary>
/// A vertical layout that retains design dimensions and reapplies the current window DPI.
/// </summary>
internal sealed class DpiStackPanel : TableLayoutPanel
{
    private readonly int[] _rowHeights;
    private readonly Dictionary<Control, Padding> _margins = new();
    private Padding _logicalPadding;
    private bool _isUpdatingLayout;
    private bool _metricsDirty = true;
    private int _metricsDpi;

    public DpiStackPanel(int[] rowHeights)
    {
        _rowHeights = rowHeights;
        // Buffer each layout surface independently; do not compose the whole
        // native window tree, which can leave a black frame during restore.
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    public Padding LogicalPadding
    {
        get => _logicalPadding;
        set
        {
            if (_logicalPadding == value)
            {
                return;
            }
            _logicalPadding = value;
            _metricsDirty = true;
            PerformLayout();
        }
    }

    public void SetRowHeight(int row, int logicalHeight)
    {
        if (_rowHeights[row] == logicalHeight)
        {
            return;
        }
        _rowHeights[row] = logicalHeight;
        _metricsDirty = true;
        PerformLayout();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        if (e.Control is not null)
        {
            _margins[e.Control] = e.Control.Margin;
            _metricsDirty = true;
        }
        base.OnControlAdded(e);
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is not null)
        {
            _margins.Remove(e.Control);
        }
        base.OnControlRemoved(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        if (_isUpdatingLayout)
        {
            return;
        }

        _isUpdatingLayout = true;
        try
        {
            int dpi = StudioTheme.Dpi(this);
            // Visibility, painting and child layout do not change design metrics.
            // Reapplying them invalidates TableLayoutPanel's preferred-size cache.
            if (_metricsDirty || _metricsDpi != dpi)
            {
                SuspendLayout();
                try
                {
                    int Scale(int value) => (int)Math.Round(value * dpi / 96f);
                    Padding ScalePadding(Padding padding) => new(Scale(padding.Left), Scale(padding.Top), Scale(padding.Right), Scale(padding.Bottom));
                    Padding = ScalePadding(_logicalPadding);
                    for (int row = 0; row < Math.Min(_rowHeights.Length, RowStyles.Count); row++)
                    {
                        if (_rowHeights[row] >= 0)
                        {
                            RowStyles[row].Height = Scale(_rowHeights[row]);
                        }
                    }
                    foreach (var (control, margin) in _margins)
                    {
                        control.Margin = ScalePadding(margin);
                    }
                    _metricsDpi = dpi;
                    _metricsDirty = false;
                }
                finally
                {
                    ResumeLayout(false);
                }
            }
            base.OnLayout(levent);
        }
        finally
        {
            _isUpdatingLayout = false;
        }
    }
}
