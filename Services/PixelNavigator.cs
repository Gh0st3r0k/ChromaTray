using System.Drawing;

namespace ChromaTray.Services;

/// <summary>
/// Двигает выбор на один физический пиксель, не позволяя попасть в промежуток между мониторами.
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
