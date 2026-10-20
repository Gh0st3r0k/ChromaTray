using System.Drawing;

namespace ChromaTray.Models;

public enum CaptureMode
{
    Point,
    Region
}

public enum ColorFormat
{
    Hex,
    Rgb,
    Hsl,
    Hsv,
    Cmyk
}

public record ColorDetails(
    Color RawColor,
    string Hex,
    string Rgb,
    string Hsl,
    string Hsv,
    string Cmyk,
    int PixelCount = 1,
    double Percentage = 100.0)
{
    public string GetValue(ColorFormat format) => format switch
    {
        ColorFormat.Hex => Hex,
        ColorFormat.Rgb => Rgb,
        ColorFormat.Hsl => Hsl,
        ColorFormat.Hsv => Hsv,
        ColorFormat.Cmyk => Cmyk,
        _ => Hex
    };

    public static string GetFormatLabel(ColorFormat format) => format switch
    {
        ColorFormat.Hex => "HEX",
        ColorFormat.Rgb => "RGB",
        ColorFormat.Hsl => "HSL",
        ColorFormat.Hsv => "HSV",
        ColorFormat.Cmyk => "CMYK",
        _ => format.ToString().ToUpperInvariant()
    };

    public static ColorDetails FromColor(Color c, int count = 1, double percentage = 100.0)
    {
        string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        string rgb = $"rgb({c.R}, {c.G}, {c.B})";

        var (h, s, l) = RgbToHsl(c.R, c.G, c.B);
        string hsl = FormattableString.Invariant($"hsl({h:F0}deg, {s:F0}%, {l:F0}%)");

        var (hv, sv, v) = RgbToHsv(c.R, c.G, c.B);
        string hsv = FormattableString.Invariant($"hsv({hv:F0}deg, {sv:F0}%, {v:F0}%)");

        var (cy, mg, yl, k) = RgbToCmyk(c.R, c.G, c.B);
        string cmyk = FormattableString.Invariant($"cmyk({cy:F0}%, {mg:F0}%, {yl:F0}%, {k:F0}%)");

        return new ColorDetails(c, hex, rgb, hsl, hsv, cmyk, count, percentage);
    }

    private static (double H, double S, double L) RgbToHsl(int r, int g, int b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double h = 0, s = 0, l = (max + min) / 2.0;

        if (Math.Abs(max - min) > 0.0001)
        {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (Math.Abs(max - rd) < 0.0001)
                h = (gd - bd) / d + (gd < bd ? 6.0 : 0.0);
            else if (Math.Abs(max - gd) < 0.0001)
                h = (bd - rd) / d + 2.0;
            else
                h = (rd - gd) / d + 4.0;

            h /= 6.0;
        }
        return (h * 360.0, s * 100.0, l * 100.0);
    }

    private static (double H, double S, double V) RgbToHsv(int r, int g, int b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double d = max - min;
        double h = 0;
        double s = max == 0 ? 0 : d / max;
        double v = max;

        if (Math.Abs(max - min) > 0.0001)
        {
            if (Math.Abs(max - rd) < 0.0001)
                h = (gd - bd) / d + (gd < bd ? 6.0 : 0.0);
            else if (Math.Abs(max - gd) < 0.0001)
                h = (bd - rd) / d + 2.0;
            else
                h = (rd - gd) / d + 4.0;

            h /= 6.0;
        }
        return (h * 360.0, s * 100.0, v * 100.0);
    }

    private static (double C, double M, double Y, double K) RgbToCmyk(int r, int g, int b)
    {
        if (r == 0 && g == 0 && b == 0) return (0, 0, 0, 100);
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double k = 1.0 - Math.Max(rd, Math.Max(gd, bd));
        double c = (1.0 - rd - k) / (1.0 - k);
        double m = (1.0 - gd - k) / (1.0 - k);
        double y = (1.0 - bd - k) / (1.0 - k);
        return (c * 100.0, m * 100.0, y * 100.0, k * 100.0);
    }
}
