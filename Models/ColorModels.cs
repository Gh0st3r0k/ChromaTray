using System.Drawing;

namespace ChromaTray.Models;

/// <summary>
/// The portion of the frozen desktop selected by a capture operation.
/// </summary>
public enum CaptureMode
{
    /// <summary>
    /// Select one original screen pixel.
    /// </summary>
    Point,
    /// <summary>
    /// Select a rectangular region for palette analysis.
    /// </summary>
    Region
}

/// <summary>
/// Supported text representations of an RGB color.
/// </summary>
public enum ColorFormat
{
    /// <summary>
    /// Hexadecimal RGB, for example #98DED9.
    /// </summary>
    Hex,
    /// <summary>
    /// RGB channels in the range 0–255.
    /// </summary>
    Rgb,
    /// <summary>
    /// Hue, saturation, and lightness.
    /// </summary>
    Hsl,
    /// <summary>
    /// Hue, saturation, and value.
    /// </summary>
    Hsv,
    /// <summary>
    /// Cyan, magenta, yellow, and black percentages without print-profile correction.
    /// </summary>
    Cmyk
}

/// <summary>
/// An original RGB value, its formatted representations, and palette statistics.
/// Formatting preserves RawColor. CMYK is a mathematical conversion without a print ICC profile.
/// </summary>
/// <param name="RawColor">The original pixel color, or an original representative color when grouping shades.</param>
/// <param name="Hex">The hexadecimal RGB representation.</param>
/// <param name="Rgb">The RGB function representation.</param>
/// <param name="Hsl">The HSL function representation.</param>
/// <param name="Hsv">The HSV function representation.</param>
/// <param name="Cmyk">The mathematical CMYK representation.</param>
/// <param name="PixelCount">Number of pixels represented by this entry, including the full bucket when grouped.</param>
/// <param name="Percentage">Share of all captured pixels, before any result-list limit.</param>
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
    /// <summary>
    /// Returns the stored representation for the requested color format.
    /// </summary>
    /// <param name="format">The requested representation.</param>
    /// <returns>The formatted value; HEX is used for an unknown enum value.</returns>
    public string GetValue(ColorFormat format) => format switch
    {
        ColorFormat.Hex => Hex,
        ColorFormat.Rgb => Rgb,
        ColorFormat.Hsl => Hsl,
        ColorFormat.Hsv => Hsv,
        ColorFormat.Cmyk => Cmyk,
        _ => Hex
    };

    /// <summary>
    /// Returns the short label displayed for a color format.
    /// </summary>
    /// <param name="format">The color format.</param>
    /// <returns>A label such as HEX, RGB, or HSL.</returns>
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
    /// <summary>
    /// Builds all supported representations without changing the original color.
    /// </summary>
    /// <param name="color">The source RGB color.</param>
    /// <param name="count">Pixel count associated with the entry.</param>
    /// <param name="percentage">Share of the captured region, in percent.</param>
    /// <returns>The original color, formatted values, and supplied statistics.</returns>
    /// <remarks>HSL/HSV hue uses degrees; saturation, lightness, value, and CMYK use percentages. Formatting is culture-independent.</remarks>
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
