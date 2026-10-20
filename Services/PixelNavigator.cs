using System.Drawing;

namespace ChromaTray.Services;

internal static class PixelNavigator
{
    // Coordinates are physical screen pixels, including negative monitor origins.
    // A desktop can contain gaps; never select a pixel in an invisible gap.
    public static Point Move(Point current, int dx, int dy, IReadOnlyList<Rectangle> displays)
    {
        if (Math.Abs(dx) + Math.Abs(dy) != 1)
            throw new ArgumentException("Move exactly one pixel along one axis.");
        var next = new Point(current.X + dx, current.Y + dy);
        return displays.Any(display => display.Contains(next)) ? next : current;
    }
}
