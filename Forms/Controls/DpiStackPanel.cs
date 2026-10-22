namespace ChromaTray.Forms.Controls;

// Исходные размеры не изменяются при масштабировании. Это предотвращает
// повторное увеличение размеров контролов, созданных после смены DPI.
/// <summary>
/// Вертикальный макет, который хранит исходные размеры и заново применяет масштаб окна.
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
        if (!_isUpdatingLayout)
        {
            _isUpdatingLayout = true;
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
                _isUpdatingLayout = false;
            }
        }
        base.OnLayout(levent);
    }
}
