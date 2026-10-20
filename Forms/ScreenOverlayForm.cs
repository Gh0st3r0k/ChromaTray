using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ChromaTray.Models;

namespace ChromaTray.Forms;

public class ScreenOverlayForm : Form
{
    private readonly CaptureMode _mode;
    private readonly Bitmap _screenSnapshot;
    private readonly Rectangle _virtualBounds;

    private Point _cursorPos;
    private Point _dragStart;
    private bool _isDragging = false;

    public Color? PickedPointColor { get; private set; }
    public Bitmap? PickedRegionBitmap { get; private set; }

    public ScreenOverlayForm(CaptureMode mode)
    {
        _mode = mode;
        _virtualBounds = SystemInformation.VirtualScreen;

        _screenSnapshot = new Bitmap(_virtualBounds.Width, _virtualBounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var g = Graphics.FromImage(_screenSnapshot);
            g.CopyFromScreen(_virtualBounds.Location, Point.Empty, _virtualBounds.Size);
        }
        catch { _screenSnapshot.Dispose(); throw; }

        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = _virtualBounds;
        TopMost = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        ShowInTaskbar = false;
        _cursorPos = new Point(Cursor.Position.X - _virtualBounds.X, Cursor.Position.Y - _virtualBounds.Y);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_mode == CaptureMode.Point)
            {
                int x = Math.Clamp(e.X, 0, _screenSnapshot.Width - 1);
                int y = Math.Clamp(e.Y, 0, _screenSnapshot.Height - 1);
                PickedPointColor = _screenSnapshot.GetPixel(x, y);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _isDragging = true;
                Capture = true;
                _dragStart = e.Location;
                _cursorPos = e.Location;
                Invalidate();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        _cursorPos = e.Location;
        Invalidate();
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_mode == CaptureMode.Region && _isDragging && e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Capture = false;
            var sel = GetNormalizedRect(_dragStart, e.Location);

            sel.Intersect(new Rectangle(0, 0, _screenSnapshot.Width, _screenSnapshot.Height));
            if (sel.Width > 2 && sel.Height > 2)
            {
                PickedRegionBitmap = _screenSnapshot.Clone(sel, PixelFormat.Format32bppArgb);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                Invalidate();
            }
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImageUnscaled(_screenSnapshot, 0, 0);

        using var dimBrush = new SolidBrush(Color.FromArgb(115, 0, 0, 0));

        if (_mode == CaptureMode.Point)
        {
            g.FillRectangle(dimBrush, 0, 0, Width, Height);
            DrawMagnifierLoupe(g, _cursorPos);
        }
        else if (_mode == CaptureMode.Region)
        {
            if (_isDragging)
            {
                var sel = GetNormalizedRect(_dragStart, _cursorPos);
                using var region = new Region(new Rectangle(0, 0, Width, Height));
                region.Exclude(sel);
                g.FillRegion(dimBrush, region);

                using var borderPen = new Pen(Color.FromArgb(16, 185, 129), 2f);
                g.DrawRectangle(borderPen, sel);

                string sizeText = $"{sel.Width} × {sel.Height} px";
                using var badgeFont = new Font("Consolas", 10f, FontStyle.Bold);
                var textSize = g.MeasureString(sizeText, badgeFont);
                var badgeRect = new RectangleF(sel.X, Math.Max(4, sel.Y - textSize.Height - 6), textSize.Width + 12, textSize.Height + 4);
                using var badgeBg = new SolidBrush(Color.FromArgb(220, 20, 20, 25));
                g.FillRectangle(badgeBg, badgeRect);
                g.DrawString(sizeText, badgeFont, Brushes.White, badgeRect.X + 6, badgeRect.Y + 2);
            }
            else
            {
                g.FillRectangle(dimBrush, 0, 0, Width, Height);
                DrawTopHintBanner(g, "Click and drag to select a screen rectangle (Press ESC or Right-Click to cancel)");
            }
        }
    }

    private void DrawMagnifierLoupe(Graphics g, Point cursor)
    {
        DrawTopHintBanner(g, "Click any pixel to inspect its color (Press ESC or Right-Click to cancel)");

        int cx = Math.Clamp(cursor.X, 0, _screenSnapshot.Width - 1);
        int cy = Math.Clamp(cursor.Y, 0, _screenSnapshot.Height - 1);
        Color c = _screenSnapshot.GetPixel(cx, cy);

        int loupeSize = 132;
        int zoomRadius = 5;
        int lx = cursor.X + 24;
        int ly = cursor.Y + 24;

        var monitor = GetCursorMonitorBounds();
        if (lx + loupeSize + 155 > monitor.Right) lx = cursor.X - loupeSize - 160;
        if (ly + loupeSize + 8 > monitor.Bottom) ly = cursor.Y - loupeSize - 12;
        lx = Math.Clamp(lx, monitor.Left + 4, Math.Max(monitor.Left + 4, monitor.Right - loupeSize - 155));
        ly = Math.Clamp(ly, monitor.Top + 4, Math.Max(monitor.Top + 4, monitor.Bottom - loupeSize - 8));

        var srcRect = new Rectangle(cx - zoomRadius, cy - zoomRadius, zoomRadius * 2 + 1, zoomRadius * 2 + 1);
        var destRect = new Rectangle(lx, ly, loupeSize, loupeSize);

        var oldMode = g.InterpolationMode;
        var oldOffset = g.PixelOffsetMode;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        using var bgBrush = new SolidBrush(Color.FromArgb(24, 24, 28));
        g.FillRectangle(bgBrush, lx - 4, ly - 4, loupeSize + 155, loupeSize + 8);
        g.DrawImage(_screenSnapshot, destRect, srcRect, GraphicsUnit.Pixel);

        g.InterpolationMode = oldMode;
        g.PixelOffsetMode = oldOffset;

        int cell = loupeSize / (zoomRadius * 2 + 1);
        var centerCell = new Rectangle(lx + zoomRadius * cell, ly + zoomRadius * cell, cell, cell);
        using var crosshairPen = new Pen(Color.Red, 2f);
        g.DrawRectangle(crosshairPen, centerCell);
        g.DrawRectangle(Pens.White, destRect);

        int infoX = lx + loupeSize + 10;
        using var swatchBrush = new SolidBrush(c);
        g.FillRectangle(swatchBrush, infoX, ly, 130, 32);
        g.DrawRectangle(Pens.White, infoX, ly, 130, 32);

        using var fontBold = new Font("Consolas", 11f, FontStyle.Bold);
        using var fontReg = new Font("Consolas", 9.5f);
        g.DrawString($"#{c.R:X2}{c.G:X2}{c.B:X2}", fontBold, Brushes.White, infoX, ly + 40);
        g.DrawString($"R:{c.R} G:{c.G} B:{c.B}", fontReg, Brushes.LightGray, infoX, ly + 64);
        g.DrawString($"X:{cx + _virtualBounds.X} Y:{cy + _virtualBounds.Y}", fontReg, Brushes.Gray, infoX, ly + 86);
    }

    private void DrawTopHintBanner(Graphics g, string text)
    {
        using var font = new Font("Segoe UI Semibold", 11f);
        var size = g.MeasureString(text, font);
        var monitor = GetCursorMonitorBounds();
        float bx = monitor.Left + (monitor.Width - size.Width - 32) / 2f;
        float by = monitor.Top + 24f;
        using var bg = new SolidBrush(Color.FromArgb(210, 20, 20, 26));
        g.FillRectangle(bg, bx, by, size.Width + 32, size.Height + 16);
        g.DrawString(text, font, Brushes.White, bx + 16, by + 8);
    }

    private static Rectangle GetNormalizedRect(Point p1, Point p2) =>
        new(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));

    private Rectangle GetCursorMonitorBounds()
    {
        var bounds = Screen.FromPoint(Cursor.Position).Bounds;
        bounds.Offset(-_virtualBounds.X, -_virtualBounds.Y);
        return bounds;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screenSnapshot.Dispose();
            PickedRegionBitmap?.Dispose();
        }
        base.Dispose(disposing);
    }
}
