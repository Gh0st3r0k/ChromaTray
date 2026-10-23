namespace ChromaTray.Forms.Controls;

// Keep the original dimensions unchanged during scaling. This prevents
// controls created after a DPI change from being scaled twice.
/// <summary>
/// A vertical layout that retains design dimensions and reapplies the current window DPI.
/// </summary>
internal sealed class DpiStackPanel(int[] rowHeights) : TableLayoutPanel
{
    private readonly Dictionary<Control, Padding> _margins = new();
    private Padding _logicalPadding;
    private bool _isUpdatingLayout;

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
            PerformLayout();
        }
    }

    public void SetRowHeight(int row, int logicalHeight)
    {
        if (rowHeights[row] == logicalHeight)
        {
            return;
        }
        rowHeights[row] = logicalHeight;
        PerformLayout();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        if (e.Control is not null)
        {
            _margins[e.Control] = e.Control.Margin;
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
        SuspendLayout();
        try
        {
            int Scale(int value) => (int)Math.Round(value * StudioTheme.Dpi(this) / 96f);
            Padding ScalePadding(Padding padding) => new(Scale(padding.Left), Scale(padding.Top), Scale(padding.Right), Scale(padding.Bottom));
            Padding = ScalePadding(_logicalPadding);
            for (int row = 0; row < Math.Min(rowHeights.Length, RowStyles.Count); row++)
            {
                if (rowHeights[row] >= 0)
                {
                    RowStyles[row].Height = Scale(rowHeights[row]);
                }
            }
            foreach (var (control, margin) in _margins)
                control.Margin = ScalePadding(margin);
        }
        finally
        {
            // Updating metrics queues layout requests. Consume them in the single
            // base pass below, keeping the guard active while children are laid out.
            ResumeLayout(false);
        }
        try
        {
            base.OnLayout(levent);
        }
        finally
        {
            _isUpdatingLayout = false;
        }
    }
}
