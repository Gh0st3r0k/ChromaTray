using System.Drawing;
using ChromaTray.Models;
using ChromaTray.Tests.Diagnostics;
using Xunit;

namespace ChromaTray.Tests;

public class ColorModelsTests
{
    [Theory(DisplayName = "[CT-COLOR-001] sRGB to HEX/RGB/HSL/HSV/CMYK conversion accuracy")]
    [InlineData(0, 0, 0, "#000000", "rgb(0, 0, 0)", "hsl(0deg, 0%, 0%)", "hsv(0deg, 0%, 0%)", "cmyk(0%, 0%, 0%, 100%)")]
    [InlineData(255, 255, 255, "#FFFFFF", "rgb(255, 255, 255)", "hsl(0deg, 0%, 100%)", "hsv(0deg, 0%, 100%)", "cmyk(0%, 0%, 0%, 0%)")]
    [InlineData(255, 0, 0, "#FF0000", "rgb(255, 0, 0)", "hsl(0deg, 100%, 50%)", "hsv(0deg, 100%, 100%)", "cmyk(0%, 100%, 100%, 0%)")]
    [InlineData(0, 255, 0, "#00FF00", "rgb(0, 255, 0)", "hsl(120deg, 100%, 50%)", "hsv(120deg, 100%, 100%)", "cmyk(100%, 0%, 100%, 0%)")]
    [InlineData(0, 0, 255, "#0000FF", "rgb(0, 0, 255)", "hsl(240deg, 100%, 50%)", "hsv(240deg, 100%, 100%)", "cmyk(100%, 100%, 0%, 0%)")]
    [InlineData(128, 128, 128, "#808080", "rgb(128, 128, 128)", "hsl(0deg, 0%, 50%)", "hsv(0deg, 0%, 50%)", "cmyk(0%, 0%, 0%, 50%)")]
    [InlineData(59, 130, 246, "#3B82F6", "rgb(59, 130, 246)", "hsl(217deg, 91%, 60%)", "hsv(217deg, 76%, 96%)", "cmyk(76%, 47%, 0%, 4%)")]
    public void FromColor_ConvertsAllStandardsAccurately(
        int r, int g, int b,
        string expectedHex,
        string expectedRgb,
        string expectedHsl,
        string expectedHsv,
        string expectedCmyk)
    {
        var input = Color.FromArgb(r, g, b);
        string ctx = $"RGB({r}, {g}, {b})";

        var details = ColorDetails.FromColor(input, count: 15, percentage: 37.5);

        DiagAssert.Equal(expectedHex, details.Hex,
            "CT-COLOR-001A", "Models/ColorDetails.FromColor", "HEX string formatting", ctx,
            "Verify uppercase 2-digit hexadecimal formatting #{R:X2}{G:X2}{B:X2}.");

        DiagAssert.Equal(expectedRgb, details.Rgb,
            "CT-COLOR-001B", "Models/ColorDetails.FromColor", "RGB string formatting", ctx,
            "Verify standard CSS rgb(r, g, b) formatting.");

        DiagAssert.Equal(expectedHsl, details.Hsl,
            "CT-COLOR-001C", "Models/ColorDetails.RgbToHsl", "HSL mathematical conversion", ctx,
            "Check hue sector calculation and saturation formula for L > 0.5 vs L <= 0.5.");

        DiagAssert.Equal(expectedHsv, details.Hsv,
            "CT-COLOR-001D", "Models/ColorDetails.RgbToHsv", "HSV mathematical conversion", ctx,
            "Check HSV value = max(R,G,B) and saturation = delta / max.");

        DiagAssert.Equal(expectedCmyk, details.Cmyk,
            "CT-COLOR-001E", "Models/ColorDetails.RgbToCmyk", "CMYK mathematical conversion", ctx,
            "Check division-by-zero guard for pure black RGB(0,0,0) -> K=100% and (1 - channel - K) / (1 - K).");
    }

    [Fact(DisplayName = "[CT-COLOR-002] GetValue(ColorFormat) returns matching property")]
    public void GetValue_ReturnsCorrectFormatStringForEachEnum()
    {
        var details = ColorDetails.FromColor(Color.FromArgb(16, 185, 129));

        foreach (ColorFormat fmt in Enum.GetValues<ColorFormat>())
        {
            string val = details.GetValue(fmt);
            DiagAssert.True(!string.IsNullOrWhiteSpace(val),
                "CT-COLOR-002", "Models/ColorDetails.GetValue", $"Fetch formatted string for {fmt}",
                $"Color={details.Hex}, Format={fmt}",
                "Non-empty formatted color string", $"'{val}'",
                "Ensure switch expression in ColorDetails.GetValue covers all ColorFormat enum members.");
        }
    }

    [Theory(DisplayName = "[CT-COLOR-003] GetFormatLabel(ColorFormat) returns uppercase UI tag")]
    [InlineData(ColorFormat.Hex, "HEX")]
    [InlineData(ColorFormat.Rgb, "RGB")]
    [InlineData(ColorFormat.Hsl, "HSL")]
    [InlineData(ColorFormat.Hsv, "HSV")]
    [InlineData(ColorFormat.Cmyk, "CMYK")]
    public void GetFormatLabel_ReturnsExpectedUppercaseTag(ColorFormat format, string expectedLabel)
    {
        string actual = ColorDetails.GetFormatLabel(format);
        DiagAssert.Equal(expectedLabel, actual,
            "CT-COLOR-003", "Models/ColorDetails.GetFormatLabel", "UI column & label header resolution",
            $"Format={format}",
            "Check switch mapping in ColorDetails.GetFormatLabel.");
    }
}
