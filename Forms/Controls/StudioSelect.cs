using System.Drawing.Drawing2D;

namespace ChromaTray.Forms.Controls;

internal sealed class StudioSelect : Control
{
    private readonly ToolStripDropDownMenu _menu;
    private string[] _items = [];
    private int _selectedIndex = -1;
    public event EventHandler? SelectedIndexChanged;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _items.Length) throw new ArgumentOutOfRangeException(nameof(value));
            if (_selectedIndex == value) return;
            _selectedIndex = value;
            AccessibleName = value >= 0 ? $"Export format: {_items[value]}" : "Export format";
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    internal ToolStripDropDownMenu DropDown => _menu;
    public StudioSelect()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Font = StudioTheme.Body;
        BackColor = StudioTheme.Surface;
        AccessibleRole = AccessibleRole.ComboBox;
        _menu = new ToolStripDropDownMenu
        {
            AutoSize = true, ShowImageMargin = false, ShowCheckMargin = false, Padding = new Padding(6),
            BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text, Font = StudioTheme.Body,
            Renderer = new StudioMenuRenderer()
        };
        _menu.Closed += (_, _) => { Invalidate(); if (!IsDisposed && Visible) Focus(); };
    }
    public void SetItems(params string[] items)
    {
        _menu.Items.Clear();
        _items = items.ToArray();
        for (int i = 0; i < items.Length; i++)
        {
            int index = i;
            var item = new ToolStripMenuItem(items[i])
            {
                Padding = new Padding(14, 10, 24, 10), BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text
            };
            item.Click += (_, _) => { SelectedIndex = index; _menu.Close(); };
            _menu.Items.Add(item);
        }
        _selectedIndex = -1;
        if (items.Length > 0) SelectedIndex = 0;
    }
    internal void OpenDropDown()
    {
        if (_menu.Visible) { _menu.Close(); return; }
        if (_items.Length == 0) return;
        _menu.MinimumSize = new Size(Width, 0);
        _menu.Show(this, new Point(0, Height + 4));
        if (SelectedIndex >= 0) _menu.Items[SelectedIndex].Select();
        Invalidate();
    }
    public void CloseDropDown() => _menu.Close();
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Focus(); OpenDropDown(); } }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key is Keys.Enter or Keys.Space or Keys.F4 || (key == Keys.Down && keyData.HasFlag(Keys.Alt)))
        { OpenDropDown(); return true; }
        if (key == Keys.Escape) { CloseDropDown(); return true; }
        if (key is Keys.Down or Keys.Up && _items.Length > 0)
        { SelectedIndex = Math.Clamp(SelectedIndex + (key == Keys.Down ? 1 : -1), 0, _items.Length - 1); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = StudioTheme.Round(new RectangleF(1, 1, Width - 2, Height - 2), S(9));
        using var fill = new SolidBrush(StudioTheme.Raised);
        using var edge = new Pen(Focused || _menu.Visible ? StudioTheme.Accent : StudioTheme.Border, Focused ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(edge, path);
        TextRenderer.DrawText(e.Graphics, SelectedIndex >= 0 ? _items[SelectedIndex] : "Choose format", Font,
            new Rectangle(S(12), 0, Math.Max(0, Width - S(48)), Height), StudioTheme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        using var pen = new Pen(StudioTheme.Accent, Math.Max(1.5f, DeviceDpi / 64f));
        int x = Width - S(23), y = Height / 2;
        e.Graphics.DrawLines(pen, new Point[] { new(x - S(4), y - S(2)), new(x, y + S(2)), new(x + S(4), y - S(2)) });
    }
    protected override void Dispose(bool disposing) { if (disposing) _menu.Dispose(); base.Dispose(disposing); }
}

internal sealed class StudioMenuRenderer : ToolStripProfessionalRenderer
{
    public StudioMenuRenderer() : base(new StudioMenuColors()) { RoundedEdges = false; }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rect = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
        using var fill = new SolidBrush(e.Item.Selected ? StudioTheme.Raised : StudioTheme.Surface);
        e.Graphics.FillRectangle(fill, rect);
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? StudioTheme.Mint : StudioTheme.Text;
        base.OnRenderItemText(e);
    }
    private sealed class StudioMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => StudioTheme.Surface;
        public override Color MenuBorder => StudioTheme.Border;
        public override Color MenuItemBorder => StudioTheme.Border;
        public override Color MenuItemSelected => StudioTheme.Raised;
        public override Color MenuItemSelectedGradientBegin => StudioTheme.Raised;
        public override Color MenuItemSelectedGradientEnd => StudioTheme.Raised;
        public override Color MenuItemPressedGradientBegin => StudioTheme.Raised;
        public override Color MenuItemPressedGradientMiddle => StudioTheme.Raised;
        public override Color MenuItemPressedGradientEnd => StudioTheme.Raised;
        public override Color ImageMarginGradientBegin => StudioTheme.Surface;
        public override Color ImageMarginGradientMiddle => StudioTheme.Surface;
        public override Color ImageMarginGradientEnd => StudioTheme.Surface;
        public override Color SeparatorDark => StudioTheme.Border;
        public override Color SeparatorLight => StudioTheme.Surface;
    }
}
