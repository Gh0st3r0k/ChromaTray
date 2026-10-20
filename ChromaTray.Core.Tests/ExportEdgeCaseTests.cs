using System.Drawing;
using System.Globalization;
using System.Text.Json;
using ChromaTray.Models;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

public class ExportEdgeCaseTests
{
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("sk-SK")]
    [InlineData("ar-SA")]
    public void Exports_AreInvariantAndParseAsJson(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var color = ColorDetails.FromColor(Color.FromArgb(59, 130, 246), 3, 37.5);
            var json = PaletteExporter.FormatEntirePalette([color], [ColorFormat.Hex, ColorFormat.Hsl], PaletteExportMode.JsonArray);
            using var parsed = JsonDocument.Parse(json);
            var first = parsed.RootElement[0];
            Assert.Equal("#3B82F6", first.GetProperty("hex").GetString());
            Assert.Equal("hsl(217deg, 91%, 60%)", first.GetProperty("hsl").GetString());
            Assert.Equal(37.5, first.GetProperty("sharePercent").GetDouble());
            Assert.False(first.TryGetProperty("rgb", out _));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void JsonExport_EscapesStringValues()
    {
        var color = ColorDetails.FromColor(Color.Red) with { Hex = "\"quoted\"\nvalue" };
        using var parsed = JsonDocument.Parse(PaletteExporter.FormatEntirePalette([color], [ColorFormat.Hex], PaletteExportMode.JsonArray));
        Assert.Equal(color.Hex, parsed.RootElement[0].GetProperty("hex").GetString());
    }

    [Theory]
    [InlineData(ColorFormat.Hsv)]
    [InlineData(ColorFormat.Cmyk)]
    public void CssExport_FallsBackToHexForFormatsWithoutCssColorSyntax(ColorFormat format)
    {
        string css = PaletteExporter.FormatEntirePalette([ColorDetails.FromColor(Color.Red)], [format], PaletteExportMode.CssVariables);
        Assert.Contains("--color-1: #FF0000;", css);
    }

    [Fact]
    public void Export_RejectsUnknownModeAndMissingFormats()
    {
        var color = ColorDetails.FromColor(Color.Red);
        Assert.Throws<ArgumentOutOfRangeException>(() => PaletteExporter.FormatEntirePalette([color], [ColorFormat.Hex], (PaletteExportMode)100));
        Assert.Throws<ArgumentException>(() => PaletteExporter.FormatSingleColorSummary(color, []));
    }
}
