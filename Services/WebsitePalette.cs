using System.Drawing;

namespace ChromaTray.Services;

/// <summary>A snapshot of five colors and a reversible assignment to website roles.</summary>
internal sealed class WebsitePalette
{
    private readonly Color[] _colors;
    private readonly int[] _assignment = [4, 0, 3, 1, 2];

    internal WebsitePalette(IReadOnlyList<Color> colors)
    {
        if (colors.Count != 5)
            throw new ArgumentException("Five colors are required.", nameof(colors));
        _colors = colors.ToArray();
    }

    internal Color this[int role] => _colors[_assignment[role]];
    internal int AssignedIndex(int role) => _assignment[role];

    internal void Assign(int role, int colorIndex)
    {
        if (role < 0 || role >= 5 || colorIndex < 0 || colorIndex >= 5)
            throw new ArgumentOutOfRangeException();
        int otherRole = Array.IndexOf(_assignment, colorIndex);
        (_assignment[role], _assignment[otherRole]) = (_assignment[otherRole], _assignment[role]);
    }

    internal static double Contrast(Color first, Color second)
    {
        static double Channel(byte value)
        {
            double component = value / 255.0;
            return component <= .04045 ? component / 12.92 : Math.Pow((component + .055) / 1.055, 2.4);
        }
        static double Luminance(Color color) => .2126 * Channel(color.R) + .7152 * Channel(color.G) + .0722 * Channel(color.B);
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
}
