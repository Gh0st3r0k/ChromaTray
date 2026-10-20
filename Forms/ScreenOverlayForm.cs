using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

public class ScreenOverlayForm : Form
{
    private readonly CaptureMode _mode;
    private readonly Bitmap _screenSnapshot;
    private readonly Rectangle _virtualBounds;
    private readonly Rectangle[] _displayBounds;
    private readonly Action<Point> _moveCursor;
    private Point _cursorPos;
    private Point _dragStart;
    private bool _isDragging;

    public Color? PickedPointColor { get; private set; }
    public Bitmap? PickedRegionBitmap { get; private set; }
    internal Point SelectedScreenPoint => new(_cursorPos.X + _virtualBounds.X, _cursorPos.Y + _virtualBounds.Y);

    public ScreenOverlayForm(CaptureMode mode) : this(mode, CaptureDesktop()) { }

    private ScreenOverlayForm(CaptureMode mode, (Bitmap Bitmap, Rectangle Bounds, Rectangle[] Displays) desktop)
        : this(mode, desktop.Bitmap, desktop.Bounds, desktop.Displays) { }

    // The overlay owns the supplied snapshot. Injection allows Windows tests to
    // exercise real key routing and painting without capturing a user's desktop.
    internal ScreenOverlayForm(CaptureMode mode, Bitmap snapshot, Rectangle virtualBounds,
        Rectangle[] displayBounds, Action<Point>? moveCursor = null, Point? initialScreenPosition = null)
    {
        _mode = mode;
        _screenSnapshot = snapshot;
        _virtualBounds = virtualBounds;
        _displayBounds = displayBounds;
        _moveCursor = moveCursor ?? (point => Cursor.Position = point);
        var start = initialScreenPosition ?? Cursor.Position;
        if (!displayBounds.Any(display => display.Contains(start))) start = displayBounds[0].Location;
        _cursorPos = new Point(start.X - virtualBounds.X, start.Y - virtualBounds.Y);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = virtualBounds;
        TopMost = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        ShowInTaskbar = false;
        AccessibleName = mode == CaptureMode.Point ? "Point picker. Arrow keys move one pixel. Enter selects. Escape cancels." : "Region picker. Drag to select. Escape cancels.";
    }

    private static (Bitmap, Rectangle, Rectangle[]) CaptureDesktop()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            return (bitmap, bounds, Screen.AllScreens.Select(screen => screen.Bounds).ToArray());
        }
        catch { bitmap.Dispose(); throw; }
    }

    // Arrow keys and Enter are dialog/navigation keys in WinForms. Handle them
    // here, before default focus traversal, rather than relying on KeyDown.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key == Keys.Escape) { CancelCapture(); return true; }
        if (_mode == CaptureMode.Point)
        {
            if (key == Keys.Enter) { ConfirmPoint(); return true; }
            (int dx, int dy) = key switch
            {
                Keys.Left => (-1, 0), Keys.Right => (1, 0), Keys.Up => (0, -1), Keys.Down => (0, 1), _ => (0, 0)
            };
            if (dx != 0 || dy != 0)
            {
                var next = PixelNavigator.Move(SelectedScreenPoint, dx, dy, _displayBounds);
                _cursorPos = new Point(next.X - _virtualBounds.X, next.Y - _virtualBounds.Y);
                _moveCursor(next);
                Invalidate();
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ConfirmPoint()
    {
        PickedPointColor = _screenSnapshot.GetPixel(_cursorPos.X, _cursorPos.Y);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelCapture() { DialogResult = DialogResult.Cancel; Close(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right) { CancelCapture(); return; }
        if (e.Button == MouseButtons.Left)
        {
            UpdatePointer(e.Location);
            if (_mode == CaptureMode.Point) ConfirmPoint();
            else
            {
                _isDragging = true;
                Capture = true;
                _dragStart = _cursorPos;
                Invalidate();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        UpdatePointer(e.Location);
        Invalidate();
        base.OnMouseMove(e);
    }

    private void UpdatePointer(Point point) => _cursorPos = new(
        Math.Clamp(point.X, 0, _screenSnapshot.Width - 1), Math.Clamp(point.Y, 0, _screenSnapshot.Height - 1));

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_mode == CaptureMode.Region && _isDragging && e.Button == MouseButtons.Left)
        {
            UpdatePointer(e.Location);
            _isDragging = false;
            Capture = false;
            var selection = GetNormalizedRect(_dragStart, _cursorPos);
            selection.Intersect(new Rectangle(Point.Empty, _screenSnapshot.Size));
            if (selection.Width > 2 && selection.Height > 2)
            {
                PickedRegionBitmap = _screenSnapshot.Clone(selection, PixelFormat.Format32bppArgb);
                DialogResult = DialogResult.OK;
                Close();
            }
            else Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImageUnscaled(_screenSnapshot, 0, 0);
        using var dim = new SolidBrush(Color.FromArgb(_mode == CaptureMode.Point ? 55 : 110, StudioTheme.Canvas));
        if (_mode == CaptureMode.Region && _isDragging)
        {
            var selected = GetNormalizedRect(_dragStart, _cursorPos);
            using var outside = new Region(ClientRectangle);
            outside.Exclude(selected);
            g.FillRegion(dim, outside);
            using var border = new Pen(StudioTheme.Mint, 2);
            g.DrawRectangle(border, selected);
            string size = $"{selected.Width} × {selected.Height} px";
            var badge = new Rectangle(selected.X, Math.Max(8, selected.Y - 34), 172, 28);
            DrawCard(g, badge, 7);
            TextRenderer.DrawText(g, size, StudioTheme.Mono, badge, StudioTheme.Mint,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        else g.FillRectangle(dim, ClientRectangle);
        if (_mode == CaptureMode.Point)
        {
            DrawCrosshair(g);
            DrawMagnifier(g);
        }
        DrawHint(g);
    }

    private void DrawCrosshair(Graphics g)
    {
        using var shadow = new Pen(Color.FromArgb(180, 0, 0, 0), 3);
        using var light = new Pen(Color.White, 1);
        int x = _cursorPos.X, y = _cursorPos.Y;
        foreach (var pen in new[] { shadow, light })
        {
            g.DrawLine(pen, x - 16, y, x - 5, y);
            g.DrawLine(pen, x + 5, y, x + 16, y);
            g.DrawLine(pen, x, y - 16, x, y - 5);
            g.DrawLine(pen, x, y + 5, x, y + 16);
        }
    }

    private void DrawMagnifier(Graphics g)
    {
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        int width = S(346), height = S(268), cell = S(14), gridSize = cell * 11;
        var monitor = GetSelectionMonitorBounds();
        int x = _cursorPos.X + S(30), y = _cursorPos.Y + S(30);
        if (x + width + S(12) > monitor.Right) x = _cursorPos.X - width - S(30);
        if (y + height + S(12) > monitor.Bottom) y = _cursorPos.Y - height - S(30);
        x = Math.Clamp(x, monitor.Left + S(12), Math.Max(monitor.Left + S(12), monitor.Right - width - S(12)));
        y = Math.Clamp(y, monitor.Top + S(12), Math.Max(monitor.Top + S(12), monitor.Bottom - height - S(12)));
        DrawCard(g, new Rectangle(x, y, width, height), S(14));
        TextRenderer.DrawText(g, "PRECISION PICKER", StudioTheme.Strong, new Rectangle(x + S(16), y + S(12), S(300), S(24)), StudioTheme.Accent);
        int gx = x + S(16), gy = y + S(46);
        // Explicit cells keep the center exactly aligned at all desktop edges.
        for (int row = 0; row < 11; row++)
            for (int col = 0; col < 11; col++)
            {
                int px = _cursorPos.X + col - 5, py = _cursorPos.Y + row - 5;
                var global = new Point(px + _virtualBounds.X, py + _virtualBounds.Y);
                bool visible = px >= 0 && py >= 0 && px < _screenSnapshot.Width && py < _screenSnapshot.Height && _displayBounds.Any(display => display.Contains(global));
                using var brush = new SolidBrush(visible ? _screenSnapshot.GetPixel(px, py) : StudioTheme.Border);
                g.FillRectangle(brush, gx + col * cell, gy + row * cell, cell, cell);
            }
        using var grid = new Pen(Color.FromArgb(30, Color.White));
        for (int i = 0; i <= 11; i++)
        {
            g.DrawLine(grid, gx + i * cell, gy, gx + i * cell, gy + gridSize);
            g.DrawLine(grid, gx, gy + i * cell, gx + gridSize, gy + i * cell);
        }
        var center = new Rectangle(gx + 5 * cell, gy + 5 * cell, cell, cell);
        using var outer = new Pen(Color.White, S(3));
        using var inner = new Pen(StudioTheme.Canvas, 1);
        g.DrawRectangle(outer, center);
        g.DrawRectangle(inner, center);
        Color color = _screenSnapshot.GetPixel(_cursorPos.X, _cursorPos.Y);
        int ix = x + S(188);
        using var swatch = new SolidBrush(color);
        using var swatchPath = StudioTheme.Round(new Rectangle(ix, gy, S(140), S(46)), S(7));
        g.FillPath(swatch, swatchPath);
        TextRenderer.DrawText(g, $"#{color.R:X2}{color.G:X2}{color.B:X2}", StudioTheme.Strong,
            new Rectangle(ix, gy + S(58), S(140), S(26)), StudioTheme.Text);
        TextRenderer.DrawText(g, $"{color.R}, {color.G}, {color.B}", StudioTheme.Mono,
            new Rectangle(ix, gy + S(88), S(140), S(24)), StudioTheme.Muted);
        TextRenderer.DrawText(g, $"X {SelectedScreenPoint.X}\nY {SelectedScreenPoint.Y}", StudioTheme.Small,
            new Rectangle(ix, gy + S(119), S(140), S(40)), StudioTheme.Muted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        using var divider = new Pen(StudioTheme.Border);
        g.DrawLine(divider, x + S(16), y + S(215), x + width - S(16), y + S(215));
        TextRenderer.DrawText(g, "↑ ↓ ← →  1 px", StudioTheme.Small, new Rectangle(x + S(16), y + S(229), S(140), S(24)), StudioTheme.Accent);
        TextRenderer.DrawText(g, "Enter  Pick   ·   Esc  Cancel", StudioTheme.Small,
            new Rectangle(x + S(146), y + S(229), S(184), S(24)), StudioTheme.Text, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
    }

    private void DrawHint(Graphics g)
    {
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        var monitor = GetSelectionMonitorBounds();
        int width = Math.Min(S(720), monitor.Width - S(32));
        var rect = new Rectangle(monitor.Left + (monitor.Width - width) / 2, monitor.Top + S(20), width, S(48));
        DrawCard(g, rect, S(12));
        string title = _mode == CaptureMode.Point ? "PICK POINT" : "SCAN REGION";
        string hint = _mode == CaptureMode.Point ? "Arrow keys  1 px   ·   Enter / Click  Pick   ·   Esc  Cancel" : "Drag to select an area   ·   Esc / Right-click  Cancel";
        TextRenderer.DrawText(g, title, StudioTheme.Strong, new Rectangle(rect.X + S(16), rect.Y, S(120), rect.Height),
            _mode == CaptureMode.Point ? StudioTheme.Accent : StudioTheme.Mint, TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, hint, StudioTheme.Small, new Rectangle(rect.X + S(144), rect.Y, Math.Max(0, rect.Width - S(160)), rect.Height), StudioTheme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private static void DrawCard(Graphics g, Rectangle rect, int radius = 14)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = StudioTheme.Round(rect, radius);
        using var fill = new SolidBrush(StudioTheme.Surface);
        using var border = new Pen(StudioTheme.Border);
        g.FillPath(fill, path);
        g.DrawPath(border, path);
        g.SmoothingMode = previous;
    }

    private Rectangle GetSelectionMonitorBounds()
    {
        var bounds = _displayBounds.FirstOrDefault(display => display.Contains(SelectedScreenPoint));
        if (bounds.IsEmpty) bounds = _displayBounds[0];
        bounds.Offset(-_virtualBounds.X, -_virtualBounds.Y);
        return bounds;
    }

    private static Rectangle GetNormalizedRect(Point start, Point end) =>
        new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y));

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _screenSnapshot.Dispose(); PickedRegionBitmap?.Dispose(); }
        base.Dispose(disposing);
    }
}
