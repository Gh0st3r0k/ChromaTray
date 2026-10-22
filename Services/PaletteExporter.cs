using System.Globalization;
using System.Text;
using System.Text.Json;
using ChromaTray.Models;

namespace ChromaTray.Services;

/// <summary>
/// Controls how an entire palette is converted into clipboard text.
/// </summary>
public enum PaletteExportMode
{
    /// <summary>
    /// Write all enabled representations and the pixel share for each entry.
    /// </summary>
    AllActiveFormats = 0,
    /// <summary>
    /// Write one primary-format value per line.
    /// </summary>
    PrimaryFormatOnly = 1,
    /// <summary>
    /// Write CSS custom properties; HSV and CMYK fall back to HEX.
    /// </summary>
    CssVariables = 2,
    /// <summary>
    /// Write a JSON array with enabled representations and sharePercent.
    /// </summary>
    JsonArray = 3
}

/// <summary>
/// Formats palette text without accessing the clipboard, user interface, or file system.
/// </summary>
public static class PaletteExporter
{
    /// <summary>
    /// Formats one color as a labeled line for each enabled format.
    /// </summary>
    /// <param name="color">The selected color.</param>
    /// <param name="activeFormats">A nonempty ordered list of representations.</param>
    /// <returns>A multiline summary without a trailing newline.</returns>
    public static string FormatSingleColorSummary(ColorDetails color, IReadOnlyList<ColorFormat> activeFormats)
    {
        if (activeFormats == null || activeFormats.Count == 0)
        {
            throw new ArgumentException("At least one active color format is required.", nameof(activeFormats));
        }

        var output = new StringBuilder();
        foreach (var format in activeFormats)
        {
            string label = ColorDetails.GetFormatLabel(format).PadRight(5);
            output.AppendLine($"{label}: {color.GetValue(format)}");
        }
        return output.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats an entire palette using the requested export mode.
    /// </summary>
    /// <param name="palette">The ordered palette. Null or an empty list produces an empty string.</param>
    /// <param name="activeFormats">Enabled formats. At least one is required for a nonempty palette.</param>
    /// <param name="mode">The text, CSS, or JSON output mode.</param>
    /// <returns>Export text without a trailing newline.</returns>
    /// <remarks>Numeric percentages are culture-independent. The first active format is the primary format.</remarks>
    public static string FormatEntirePalette(
        IReadOnlyList<ColorDetails> palette,
        IReadOnlyList<ColorFormat> activeFormats,
        PaletteExportMode mode)
    {
        if (palette == null || palette.Count == 0)
        {
            return string.Empty;
        }

        if (activeFormats == null || activeFormats.Count == 0)
        {
            throw new ArgumentException("At least one active color format is required.", nameof(activeFormats));
        }

        var output = new StringBuilder();
        var primaryFormat = activeFormats[0];

        switch (mode)
        {
            case PaletteExportMode.AllActiveFormats:
                WriteAllFormats(output, palette, activeFormats);
                break;
            case PaletteExportMode.PrimaryFormatOnly:
                WritePrimaryFormat(output, palette, primaryFormat);
                break;
            case PaletteExportMode.CssVariables:
                WriteCssVariables(output, palette, primaryFormat);
                break;
            case PaletteExportMode.JsonArray:
                WriteJsonArray(output, palette, activeFormats);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown export mode.");
        }

        return output.ToString().TrimEnd();
    }

    private static void WriteAllFormats(StringBuilder output, IReadOnlyList<ColorDetails> palette, IReadOnlyList<ColorFormat> activeFormats)
    {
        foreach (var color in palette)
        {
            var parts = activeFormats.Select(color.GetValue);
            output.AppendLine($"{string.Join(" | ", parts)} ({color.Percentage.ToString("F1", CultureInfo.InvariantCulture)}%)");
        }
    }

    private static void WritePrimaryFormat(StringBuilder output, IReadOnlyList<ColorDetails> palette, ColorFormat primaryFormat)
    {
        foreach (var color in palette)
        {
            output.AppendLine(color.GetValue(primaryFormat));
        }
    }

    private static void WriteCssVariables(StringBuilder output, IReadOnlyList<ColorDetails> palette, ColorFormat primaryFormat)
    {
        // CSS has no standard hsv()/cmyk() functions; use HEX when either is the primary format.
        output.AppendLine(":root {");
        for (int i = 0; i < palette.Count; i++)
        {
            output.AppendLine($"  --color-{i + 1}: {palette[i].GetValue(primaryFormat is ColorFormat.Hsv or ColorFormat.Cmyk ? ColorFormat.Hex : primaryFormat)};");
        }
        output.AppendLine("}");
    }

    private static void WriteJsonArray(StringBuilder output, IReadOnlyList<ColorDetails> palette, IReadOnlyList<ColorFormat> activeFormats)
    {
        output.AppendLine("[");
        for (int i = 0; i < palette.Count; i++)
        {
            var color = palette[i];
            var properties = new List<string>();
            foreach (var format in activeFormats)
            {
                string key = format.ToString().ToLowerInvariant();
                // The JSON serializer escapes format strings, including quotes and control characters.
                properties.Add($"\"{key}\": {JsonSerializer.Serialize(color.GetValue(format))}");
            }
            properties.Add($"\"sharePercent\": {color.Percentage.ToString("F2", CultureInfo.InvariantCulture)}");
            string comma = i < palette.Count - 1 ? "," : "";
            output.AppendLine($"  {{ {string.Join(", ", properties)} }}{comma}");
        }
        output.AppendLine("]");
    }
}
