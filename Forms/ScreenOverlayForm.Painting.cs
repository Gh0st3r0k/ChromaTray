using System.Drawing.Drawing2D;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;

namespace ChromaTray.Forms;

// Только отрисовка: сетка лупы, информация о пикселе и рамка региона.
// Этот код не изменяет снимок, из которого берутся результаты выбора.
public partial class ScreenOverlayForm
{
    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.DrawImageUnscaled(_screenSnapshot, 0, 0);
        using var dim = new SolidBrush(Color.FromArgb(85, Color.Black));
        if (_mode == CaptureMode.Region && _isDragging)
        {
            var selected = GetNormalizedRect(_dragStart, _cursorPosition);
            using var outside = new Region(ClientRectangle);
            outside.Exclude(selected);
            graphics.FillRegion(dim, outside);
            using var border = new Pen(StudioTheme.Mint, 2);
            graphics.DrawRectangle(border, selected);
            string size = $"{selected.Width} × {selected.Height} px";
            var badge = new Rectangle(selected.X, Math.Max(8, selected.Y - 34), 172, 28);
            DrawCard(graphics, badge, 7);
            TextRenderer.DrawText(graphics, size, StudioTheme.Mono, badge, StudioTheme.Mint,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        // Точка показывает неизменённый снимок; регион затемняет только пространство вне выделения.
        if (_mode == CaptureMode.Point)
        {
            DrawMagnifier(graphics);
        }
        DrawHint(graphics);
    }

    private void DrawMagnifier(Graphics graphics)
    {
        int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        int width = Scale(346), height = Scale(268), cell = Scale(14), gridSize = cell * MagnifierCellsPerSide;
        var monitor = GetSelectionMonitorBounds();
        int x = _cursorPosition.X + Scale(30), y = _cursorPosition.Y + Scale(30);
        if (x + width + Scale(12) > monitor.Right)
        {
            x = _cursorPosition.X - width - Scale(30);
        }
        if (y + height + Scale(12) > monitor.Bottom)
        {
            y = _cursorPosition.Y - height - Scale(30);
        }
        x = Math.Clamp(x, monitor.Left + Scale(12), Math.Max(monitor.Left + Scale(12), monitor.Right - width - Scale(12)));
        y = Math.Clamp(y, monitor.Top + Scale(12), Math.Max(monitor.Top + Scale(12), monitor.Bottom - height - Scale(12)));
        DrawCard(graphics, new Rectangle(x, y, width, height), Scale(14));
        TextRenderer.DrawText(graphics, "PRECISION PICKER", StudioTheme.Strong, new Rectangle(x + Scale(16), y + Scale(12), Scale(300), Scale(24)), StudioTheme.Accent);
        int gridX = x + Scale(16), gridY = y + Scale(46);
        // Каждый исходный пиксель рисуется отдельной ячейкой без интерполяции.
        for (int row = 0; row < MagnifierCellsPerSide; row++)
        {
            for (int col = 0; col < MagnifierCellsPerSide; col++)
            {
                int pixelX = _cursorPosition.X + col - MagnifierCenterIndex, pixelY = _cursorPosition.Y + row - MagnifierCenterIndex;
                var global = new Point(pixelX + _virtualBounds.X, pixelY + _virtualBounds.Y);
                bool visible = pixelX >= 0 && pixelY >= 0 && pixelX < _screenSnapshot.Width && pixelY < _screenSnapshot.Height && _displayBounds.Any(display => display.Contains(global));
                using var brush = new SolidBrush(visible ? _screenSnapshot.GetPixel(pixelX, pixelY) : StudioTheme.Border);
                graphics.FillRectangle(brush, gridX + col * cell, gridY + row * cell, cell, cell);
            }
        }
        using var grid = new Pen(Color.FromArgb(30, Color.White));
        for (int i = 0; i <= MagnifierCellsPerSide; i++)
        {
            graphics.DrawLine(grid, gridX + i * cell, gridY, gridX + i * cell, gridY + gridSize);
            graphics.DrawLine(grid, gridX, gridY + i * cell, gridX + gridSize, gridY + i * cell);
        }
        var center = new Rectangle(gridX + MagnifierCenterIndex * cell, gridY + MagnifierCenterIndex * cell, cell, cell);
        using var outer = new Pen(Color.White, Scale(3));
        using var inner = new Pen(StudioTheme.Canvas, 1);
        graphics.DrawRectangle(outer, center);
        graphics.DrawRectangle(inner, center);
        Color color = _screenSnapshot.GetPixel(_cursorPosition.X, _cursorPosition.Y);
        int detailsX = x + Scale(188);
        using var swatch = new SolidBrush(color);
        using var swatchPath = StudioTheme.Round(new Rectangle(detailsX, gridY, Scale(140), Scale(46)), Scale(7));
        graphics.FillPath(swatch, swatchPath);
        TextRenderer.DrawText(graphics, $"#{color.R:X2}{color.G:X2}{color.B:X2}", StudioTheme.Strong,
            new Rectangle(detailsX, gridY + Scale(58), Scale(140), Scale(26)), StudioTheme.Text);
        TextRenderer.DrawText(graphics, $"{color.R}, {color.G}, {color.B}", StudioTheme.Mono,
            new Rectangle(detailsX, gridY + Scale(88), Scale(140), Scale(24)), StudioTheme.Muted);
        TextRenderer.DrawText(graphics, $"X {SelectedScreenPoint.X}\nY {SelectedScreenPoint.Y}", StudioTheme.Small,
            new Rectangle(detailsX, gridY + Scale(119), Scale(140), Scale(40)), StudioTheme.Muted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        using var divider = new Pen(StudioTheme.Border);
        graphics.DrawLine(divider, x + Scale(16), y + Scale(215), x + width - Scale(16), y + Scale(215));
        TextRenderer.DrawText(graphics, "↑ ↓ ← →  1 px", StudioTheme.Small, new Rectangle(x + Scale(16), y + Scale(229), Scale(140), Scale(24)), StudioTheme.Accent);
        TextRenderer.DrawText(graphics, "Enter  Pick   ·   Esc  Cancel", StudioTheme.Small,
            new Rectangle(x + Scale(146), y + Scale(229), Scale(184), Scale(24)), StudioTheme.Text, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
    }

    private void DrawHint(Graphics graphics)
    {
        int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        var monitor = GetSelectionMonitorBounds();
        int width = Math.Min(Scale(720), monitor.Width - Scale(32));
        var rect = new Rectangle(monitor.Left + (monitor.Width - width) / 2, monitor.Top + Scale(20), width, Scale(48));
        DrawCard(graphics, rect, Scale(12));
        string title = _mode == CaptureMode.Point ? "PICK POINT" : "SCAN REGION";
        string hint = _mode == CaptureMode.Point ? "Arrow keys  1 px   ·   Enter / Click  Pick   ·   Esc  Cancel" : "Drag to select an area   ·   Esc / Right-click  Cancel";
        TextRenderer.DrawText(graphics, title, StudioTheme.Strong, new Rectangle(rect.X + Scale(16), rect.Y, Scale(120), rect.Height),
            _mode == CaptureMode.Point ? StudioTheme.Accent : StudioTheme.Mint, TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(graphics, hint, StudioTheme.Small, new Rectangle(rect.X + Scale(144), rect.Y, Math.Max(0, rect.Width - Scale(160)), rect.Height), StudioTheme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private static void DrawCard(Graphics graphics, Rectangle rect, int radius = 14)
    {
        var previous = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = StudioTheme.Round(rect, radius);
        using var fill = new SolidBrush(StudioTheme.Surface);
        using var border = new Pen(StudioTheme.Border);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
        graphics.SmoothingMode = previous;
    }

    private Rectangle GetSelectionMonitorBounds()
    {
        var bounds = _displayBounds.FirstOrDefault(display => display.Contains(SelectedScreenPoint));
        if (bounds.IsEmpty)
        {
            bounds = _displayBounds[0];
        }
        bounds.Offset(-_virtualBounds.X, -_virtualBounds.Y);
        return bounds;
    }
}
