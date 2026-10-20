using System.Drawing;
using ChromaTray.Models;
using ChromaTray.Services;
using ChromaTray.Tests.Diagnostics;
using Xunit;

namespace ChromaTray.Tests;

public class PaletteExporterTests
{
    private static readonly List<ColorDetails> SamplePalette =
    [
        ColorDetails.FromColor(Color.FromArgb(59, 130, 246), count: 75, percentage: 75.0),
        ColorDetails.FromColor(Color.FromArgb(16, 185, 129), count: 25, percentage: 25.0)
    ];

    [Fact(DisplayName = "[CT-EXP-001] Single color summary includes only active formats in order")]
    public void FormatSingleColorSummary_IncludesOnlyConfiguredFormats()
    {
        var active = new List<ColorFormat> { ColorFormat.Hex, ColorFormat.Cmyk };
        string output = PaletteExporter.FormatSingleColorSummary(SamplePalette[0], active);

        DiagAssert.True(output.Contains("HEX  : #3B82F6") && output.Contains("CMYK : cmyk(76%, 47%, 0%, 4%)"),
            "CT-EXP-001A", "Services/PaletteExporter", "Single color summary active formats", "Active=[Hex, Cmyk]",
            "Contains HEX and CMYK lines", output,
            "FormatSingleColorSummary must iterate over activeFormats and output padded labels.");

        DiagAssert.True(!output.Contains("RGB") && !output.Contains("HSL") && !output.Contains("HSV"),
            "CT-EXP-001B", "Services/PaletteExporter", "Disabled formats exclusion", "Active=[Hex, Cmyk]",
            "No RGB, HSL, or HSV lines", output,
            "Disabled formats must not leak into clipboard summary.");
    }

    [Fact(DisplayName = "[CT-EXP-002] Pipe-separated palette export respects active formats")]
    public void FormatEntirePalette_AllActiveFormats_GeneratesPipeSeparatedLines()
    {
        var active = new List<ColorFormat> { ColorFormat.Hex, ColorFormat.Rgb };
        string output = PaletteExporter.FormatEntirePalette(SamplePalette, active, PaletteExportMode.AllActiveFormats);
        string[] lines = output.Split(Environment.NewLine);

        DiagAssert.Equal(2, lines.Length,
            "CT-EXP-002A", "Services/PaletteExporter", "Line count matches palette count", "2 colors in palette",
            "Each color in palette must produce exactly 1 line in AllActiveFormats mode.");

        DiagAssert.Equal("#3B82F6 | rgb(59, 130, 246) (75.0%)", lines[0],
            "CT-EXP-002B", "Services/PaletteExporter", "Pipe-separated formatting with percentage", "Color 1 (#3B82F6, 75%)",
            "Expected format: '{Format1} | {Format2} ({Percentage:F1}%)'.");
    }

    [Fact(DisplayName = "[CT-EXP-003] PrimaryFormatOnly exports first active standard line-by-line")]
    public void FormatEntirePalette_PrimaryFormatOnly_UsesFirstActiveFormat()
    {
        var active = new List<ColorFormat> { ColorFormat.Rgb, ColorFormat.Hsl };
        string output = PaletteExporter.FormatEntirePalette(SamplePalette, active, PaletteExportMode.PrimaryFormatOnly);
        string[] lines = output.Split(Environment.NewLine);

        DiagAssert.Equal("rgb(59, 130, 246)", lines[0],
            "CT-EXP-003", "Services/PaletteExporter", "PrimaryFormatOnly uses activeFormats[0]", "Active=[Rgb, Hsl]",
            "PrimaryFormatOnly must use activeFormats[0] rather than hardcoding HEX.");
    }

    [Fact(DisplayName = "[CT-EXP-004] CSS Variables export creates valid :root block")]
    public void FormatEntirePalette_CssVariables_GeneratesValidRootBlock()
    {
        var active = new List<ColorFormat> { ColorFormat.Hex };
        string output = PaletteExporter.FormatEntirePalette(SamplePalette, active, PaletteExportMode.CssVariables);

        DiagAssert.True(
            output.StartsWith(":root {") &&
            output.Contains("  --color-1: #3B82F6;") &&
            output.Contains("  --color-2: #10B981;") &&
            output.EndsWith("}"),
            "CT-EXP-004", "Services/PaletteExporter", "CSS custom properties syntax", "2 colors (#3B82F6, #10B981)",
            ":root { --color-1: #3B82F6; --color-2: #10B981; }", output,
            "Verify CSS variable numbering starts at 1 and wraps in :root { ... }.");
    }

    [Fact(DisplayName = "[CT-EXP-005] JSON export includes only enabled color formats and sharePercent")]
    public void FormatEntirePalette_JsonArray_SerializesOnlyActiveFormatKeys()
    {
        var active = new List<ColorFormat> { ColorFormat.Hex, ColorFormat.Hsl };
        string json = PaletteExporter.FormatEntirePalette(SamplePalette, active, PaletteExportMode.JsonArray);

        DiagAssert.True(
            json.Contains("\"hex\": \"#3B82F6\"") &&
            json.Contains("\"hsl\": \"hsl(217deg, 91%, 60%)\"") &&
            json.Contains("\"sharePercent\": 75.00") &&
            !json.Contains("\"rgb\":") &&
            !json.Contains("\"cmyk\":"),
            "CT-EXP-005", "Services/PaletteExporter", "JSON export selective keys", "Active=[Hex, Hsl]",
            "JSON objects with 'hex', 'hsl', 'sharePercent' only", json,
            "Ensure JSON export iterates over activeFormats and formats sharePercent with InvariantCulture.");
    }
}
