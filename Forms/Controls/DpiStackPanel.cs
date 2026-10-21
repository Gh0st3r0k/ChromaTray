namespace ChromaTray.Forms.Controls;

// Reapply design metrics after WinForms has scaled the children. Controls added
// before/after handle creation must use the same DPI baseline.
internal sealed class DpiStackPanel(int[] rowHeights) : TableLayoutPanel
{
    private readonly Dictionary<Control, Padding> _margins = new();
    private Padding _logicalPadding;
    private bool _updating;

    public Padding LogicalPadding
    {
        get => _logicalPadding;
        set { _logicalPadding = value; PerformLayout(); }
    }

    public void SetRowHeight(int row, int logicalHeight)
    {
        if (rowHeights[row] == logicalHeight) return;
        rowHeights[row] = logicalHeight;
        PerformLayout();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        if (e.Control is not null) _margins[e.Control] = e.Control.Margin;
        base.OnControlAdded(e);
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is not null) _margins.Remove(e.Control);
        base.OnControlRemoved(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        if (!_updating)
        {
            _updating = true;
            try
            {
                int S(int value) => (int)Math.Round(value * StudioTheme.Dpi(this) / 96f);
                Padding Scale(Padding p) => new(S(p.Left), S(p.Top), S(p.Right), S(p.Bottom));
                Padding = Scale(_logicalPadding);
                for (int row = 0; row < Math.Min(rowHeights.Length, RowStyles.Count); row++)
                    if (rowHeights[row] >= 0) RowStyles[row].Height = S(rowHeights[row]);
                foreach (var (control, margin) in _margins) control.Margin = Scale(margin);
            }
            finally { _updating = false; }
        }
        base.OnLayout(levent);
    }
}
