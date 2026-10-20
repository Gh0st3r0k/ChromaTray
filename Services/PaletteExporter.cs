using System.Globalization;
using System.Text;
using System.Text.Json;
using ChromaTray.Models;

namespace ChromaTray.Services;

public enum PaletteExportMode
{
    AllActiveFormats = 0,
    PrimaryFormatOnly = 1,
    CssVariables = 2,
    JsonArray = 3
}

public static class PaletteExporter
{
    public static string FormatSingleColorSummary(ColorDetails color, IReadOnlyList<ColorFormat> activeFormats)
    {
        if (activeFormats == null || activeFormats.Count == 0)
            throw new ArgumentException("At least one active color format is required.", nameof(activeFormats));

        var sb = new StringBuilder();
        foreach (var fmt in activeFormats)
        {
            string label = ColorDetails.GetFormatLabel(fmt).PadRight(5);
            sb.AppendLine($"{label}: {color.GetValue(fmt)}");
        }
        return sb.ToString().TrimEnd();
    }

    public static string FormatEntirePalette(
        IReadOnlyList<ColorDetails> palette,
        IReadOnlyList<ColorFormat> activeFormats,
        PaletteExportMode mode)
    {
        if (palette == null || palette.Count == 0)
            return string.Empty;

        if (activeFormats == null || activeFormats.Count == 0)
            throw new ArgumentException("At least one active color format is required.", nameof(activeFormats));

        var sb = new StringBuilder();
        var primaryFmt = activeFormats[0];

        switch (mode)
        {
            case PaletteExportMode.AllActiveFormats:
                foreach (var c in palette)
                {
                    var parts = activeFormats.Select(c.GetValue);
                    sb.AppendLine($"{string.Join(" | ", parts)} ({c.Percentage.ToString("F1", CultureInfo.InvariantCulture)}%)");
                }
                break;

            case PaletteExportMode.PrimaryFormatOnly:
                foreach (var c in palette)
                    sb.AppendLine(c.GetValue(primaryFmt));
                break;

            case PaletteExportMode.CssVariables:
                sb.AppendLine(":root {");
                for (int i = 0; i < palette.Count; i++)
                    sb.AppendLine($"  --color-{i + 1}: {palette[i].GetValue(primaryFmt is ColorFormat.Hsv or ColorFormat.Cmyk ? ColorFormat.Hex : primaryFmt)};");
                sb.AppendLine("}");
                break;

            case PaletteExportMode.JsonArray:
                sb.AppendLine("[");
                for (int i = 0; i < palette.Count; i++)
                {
                    var c = palette[i];
                    var props = new List<string>();
                    foreach (var fmt in activeFormats)
                    {
                        string key = fmt.ToString().ToLowerInvariant();
                        props.Add($"\"{key}\": {JsonSerializer.Serialize(c.GetValue(fmt))}");
                    }
                    props.Add($"\"sharePercent\": {c.Percentage.ToString("F2", CultureInfo.InvariantCulture)}");
                    string comma = i < palette.Count - 1 ? "," : "";
                    sb.AppendLine($"  {{ {string.Join(", ", props)} }}{comma}");
                }
                sb.AppendLine("]");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown export mode.");
        }

        return sb.ToString().TrimEnd();
    }
}
