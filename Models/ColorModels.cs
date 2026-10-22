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

/// <summary>
/// Исходный RGB, готовые строки форматов и статистика одного цвета.
/// Пересчёт форматов не меняет RawColor; CMYK здесь математический, без печатного ICC-профиля.
/// </summary>
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

    // Форматируем с инвариантной культурой: десятичная запятая локали не должна испортить экспорт.
    public static ColorDetails FromColor(Color color, int count = 1, double percentage = 100.0)
    {
        string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        string rgb = $"rgb({color.R}, {color.G}, {color.B})";

        var (hue, saturation, lightness) = RgbToHsl(color.R, color.G, color.B);
        string hsl = FormattableString.Invariant($"hsl({hue:F0}deg, {saturation:F0}%, {lightness:F0}%)");

        var (hsvHue, hsvSaturation, value) = RgbToHsv(color.R, color.G, color.B);
        string hsv = FormattableString.Invariant($"hsv({hsvHue:F0}deg, {hsvSaturation:F0}%, {value:F0}%)");

        var (cyan, magenta, yellow, black) = RgbToCmyk(color.R, color.G, color.B);
        string cmyk = FormattableString.Invariant($"cmyk({cyan:F0}%, {magenta:F0}%, {yellow:F0}%, {black:F0}%)");

        return new ColorDetails(color, hex, rgb, hsl, hsv, cmyk, count, percentage);
    }

    // HSL: светлота — середина между самым ярким и самым тёмным нормированным каналом.
    private static (double H, double S, double L) RgbToHsl(int red, int green, int blue)
    {
        double normalizedRed = red / 255.0;
        double normalizedGreen = green / 255.0;
        double normalizedBlue = blue / 255.0;
        double maximum = Math.Max(normalizedRed, Math.Max(normalizedGreen, normalizedBlue));
        double minimum = Math.Min(normalizedRed, Math.Min(normalizedGreen, normalizedBlue));
        double hue = 0;
        double saturation = 0;
        double lightness = (maximum + minimum) / 2.0;

        if (Math.Abs(maximum - minimum) > 0.0001)
        {
            double range = maximum - minimum;
            saturation = lightness > 0.5 ? range / (2.0 - maximum - minimum) : range / (maximum + minimum);

            hue = CalculateHueTurns(normalizedRed, normalizedGreen, normalizedBlue, maximum, range);
        }
        return (hue * 360.0, saturation * 100.0, lightness * 100.0);
    }

    // HSV: яркость — максимальный канал, насыщенность — доля цветового разброса в яркости.
    private static (double H, double S, double V) RgbToHsv(int red, int green, int blue)
    {
        double normalizedRed = red / 255.0;
        double normalizedGreen = green / 255.0;
        double normalizedBlue = blue / 255.0;
        double maximum = Math.Max(normalizedRed, Math.Max(normalizedGreen, normalizedBlue));
        double minimum = Math.Min(normalizedRed, Math.Min(normalizedGreen, normalizedBlue));
        double range = maximum - minimum;
        double hue = 0;
        double saturation = maximum == 0 ? 0 : range / maximum;
        double value = maximum;

        if (Math.Abs(maximum - minimum) > 0.0001)
        {
            hue = CalculateHueTurns(normalizedRed, normalizedGreen, normalizedBlue, maximum, range);
        }
        return (hue * 360.0, saturation * 100.0, value * 100.0);
    }

    // Для чёрного нужен отдельный случай: общая формула делила бы на ноль.
    private static (double C, double M, double Y, double K) RgbToCmyk(int red, int green, int blue)
    {
        if (red == 0 && green == 0 && blue == 0)
        {
            return (0, 0, 0, 100);
        }
        double normalizedRed = red / 255.0;
        double normalizedGreen = green / 255.0;
        double normalizedBlue = blue / 255.0;
        double black = 1.0 - Math.Max(normalizedRed, Math.Max(normalizedGreen, normalizedBlue));
        double cyan = (1.0 - normalizedRed - black) / (1.0 - black);
        double magenta = (1.0 - normalizedGreen - black) / (1.0 - black);
        double yellow = (1.0 - normalizedBlue - black) / (1.0 - black);
        return (cyan * 100.0, magenta * 100.0, yellow * 100.0, black * 100.0);
    }

    // HSL и HSV используют один цветовой круг: результат здесь — доля оборота [0, 1].
    private static double CalculateHueTurns(double red, double green, double blue, double maximum, double range)
    {
        double hue;
        if (Math.Abs(maximum - red) < 0.0001)
        {
            hue = (green - blue) / range + (green < blue ? 6.0 : 0.0);
        }
        else if (Math.Abs(maximum - green) < 0.0001)
        {
            hue = (blue - red) / range + 2.0;
        }
        else
        {
            hue = (red - green) / range + 4.0;
        }
        return hue / 6.0;
    }
}
