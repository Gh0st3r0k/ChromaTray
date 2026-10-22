using System.Drawing;

namespace ChromaTray.Services;

/// <summary>
/// Moves the selection by one physical pixel while preventing movement into gaps between monitors.
/// </summary>
internal static class PixelNavigator
{
    // Координаты мониторов могут быть отрицательными. Если следующий пиксель
    // не относится ни к одному дисплею, оставляем выбор на месте.
    public static Point Move(Point current, int dx, int dy, IReadOnlyList<Rectangle> displays)
    {
        if (Math.Abs(dx) + Math.Abs(dy) != 1)
        {
            throw new ArgumentException("Move exactly one pixel along one axis.");
        }
        var next = new Point(current.X + dx, current.Y + dy);
        return displays.Any(display => display.Contains(next)) ? next : current;
    }
}
