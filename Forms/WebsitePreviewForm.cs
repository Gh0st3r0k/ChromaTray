using System.Runtime.InteropServices;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

/// <summary>A local sample website with swappable palette-to-role assignments.</summary>
internal sealed class WebsitePreviewForm : Form
{
    private static readonly string[] Roles = ["Background", "Text", "Cards / surfaces", "Primary button", "Accent"];
    private static readonly string[] Tokens = ["background", "text", "surface", "primary", "accent"];
    private readonly WebsitePalette _palette;
    private readonly ComboBox[] _selectors = new ComboBox[5];
    private readonly Label _status = StudioTheme.Label("", StudioTheme.Small, StudioTheme.Muted);
    private readonly WebsiteCanvas _canvas;
    private bool _updating;

    internal WebsitePreviewForm(IReadOnlyList<Color> colors)
    {
        Color[] fallback = [StudioTheme.Accent, StudioTheme.Mint, StudioTheme.Raised, StudioTheme.Surface, StudioTheme.Canvas];
        var snapshot = Enumerable.Range(0, 5).Select(i => i < colors.Count ? colors[i] : fallback[i]).ToArray();
        _palette = new WebsitePalette(snapshot);
        _canvas = new WebsiteCanvas(_palette) { Dock = DockStyle.Fill, Name = "WebsiteCanvas" };
        Text = "ChromaTray · Website preview";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(950, 680);
        BackColor = StudioTheme.Canvas;
        ForeColor = StudioTheme.Text;
        Font = StudioTheme.Body;
        DoubleBuffered = true;
        StartPosition = FormStartPosition.CenterParent;
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 14, Padding = new Padding(12), BackColor = StudioTheme.Surface };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        sidebar.Controls.Add(StudioTheme.Label("WEBSITE COLORS", StudioTheme.Strong), 0, 0);
        for (int role = 0; role < 5; role++)
        {
            int currentRole = role;
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            sidebar.Controls.Add(StudioTheme.Label(Roles[role]), 0, role * 2 + 1);
            var select = new ComboBox
            {
                Name = $"WebsiteRole{role}",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = StudioTheme.Raised,
                ForeColor = StudioTheme.Text,
                AccessibleName = $"Color for {Roles[role]}"
            };
            select.Items.AddRange(snapshot.Select((color, i) => (object)$"{i + 1} · {ColorDetails.FromColor(color).Hex}").ToArray());
            select.SelectedIndexChanged += (_, _) =>
            {
                if (!_updating && select.SelectedIndex >= 0)
                {
                    _palette.Assign(currentRole, select.SelectedIndex);
                    RefreshAssignments();
                }
            };
            _selectors[role] = select;
            sidebar.Controls.Add(select, 0, role * 2 + 2);
        }
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        var copy = new StudioButton { Text = "Copy role CSS", Dock = DockStyle.Fill };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(":root {\n" + string.Join("\n", Enumerable.Range(0, 5).Select(role => $"  --{Tokens[role]}: {ColorDetails.FromColor(_palette[role]).Hex};")) + "\n}");
                _status.Text = "Role CSS copied.";
            }
            catch (ExternalException) { _status.Text = "Clipboard is busy. Try again."; }
        };
        sidebar.Controls.Add(copy, 0, 11);
        sidebar.Controls.Add(_status, 0, 12);
        sidebar.Controls.Add(StudioTheme.Label("Changing a role swaps two colors. This is a sample, not a live website.", StudioTheme.Small, StudioTheme.Muted), 0, 13);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(sidebar, 0, 0);
        layout.Controls.Add(_canvas, 1, 0);
        Controls.Add(layout);
        RefreshAssignments();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StudioTheme.ApplyWindowChrome(this);
    }

    private void RefreshAssignments()
    {
        _updating = true;
        try
        {
            for (int role = 0; role < 5; role++)
                _selectors[role].SelectedIndex = _palette.AssignedIndex(role);
        }
        finally { _updating = false; }
        double contrast = WebsitePalette.Contrast(_palette[0], _palette[1]);
        _status.Text = $"Text / background: {contrast:F2}:1\nWCAG AA normal text: {(contrast >= 4.5 ? "Pass" : "Fail")}";
        _canvas.Invalidate();
    }

    private sealed class WebsiteCanvas(WebsitePalette palette) : Control
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            base.OnHandleCreated(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(palette[0]);
            float scale = DeviceDpi / 96f;
            int S(int value) => (int)Math.Round(value * scale);
            int padding = S(28), width = ClientSize.Width - padding * 2;
            void Text(string text, Font font, Color color, int y, int height) => TextRenderer.DrawText(g, text, font,
                new Rectangle(padding, S(y), width, S(height)), color, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            using var surface = new SolidBrush(palette[2]);
            using var primary = new SolidBrush(palette[3]);
            using var accent = new SolidBrush(palette[4]);
            Text("STUDIO / HOME     WORK     ABOUT", StudioTheme.Strong, palette[1], 26, 40);
            g.FillRectangle(accent, padding, S(86), S(70), S(5));
            Text("Make room for\nyour next big idea.", StudioTheme.Title, palette[1], 112, 120);
            Text("A sample landing page for exploring your palette.\nAssign each color to a role and compare the results.", StudioTheme.Body, palette[1], 244, 66);
            var button = new Rectangle(padding, S(328), Math.Min(width, S(210)), S(48));
            g.FillRectangle(primary, button);
            var buttonInk = WebsitePalette.Contrast(palette[3], Color.White) >= WebsitePalette.Contrast(palette[3], Color.Black) ? Color.White : Color.Black;
            TextRenderer.DrawText(g, "Explore the collection", StudioTheme.Strong, button, buttonInk, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            int gap = S(16), cardWidth = Math.Max(1, (width - gap) / 2);
            for (int i = 0; i < 2; i++)
            {
                var card = new Rectangle(padding + i * (cardWidth + gap), S(418), cardWidth, S(160));
                g.FillRectangle(surface, card);
                g.FillRectangle(accent, card.X + S(16), card.Y + S(18), S(32), S(6));
                var label = new Rectangle(card.X + S(16), card.Y + S(42), Math.Max(1, card.Width - S(32)), S(110));
                TextRenderer.DrawText(g, i == 0 ? "Thoughtful design\n\nYour content belongs here." : "Built to inspire\n\nSee how the colors work together.", StudioTheme.Body, label, palette[1], TextFormatFlags.WordBreak);
            }
        }
    }
}
