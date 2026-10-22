using System.Drawing;
using ChromaTray.Models;

namespace ChromaTray.Services;

/// <summary>
/// A completed palette scan, including counts before the result limit.
/// </summary>
/// <param name="Palette">Most frequent entries, sorted by frequency and then RGB.</param>
/// <param name="TotalPixels">Total scanned pixels.</param>
/// <param name="UniqueColorsFound">Number of exact colors or shade groups before limiting the result list.</param>
/// <param name="ElapsedMilliseconds">Analysis duration in milliseconds.</param>
public record PaletteAnalysisResult(
    List<ColorDetails> Palette,
    int TotalPixels,
    int UniqueColorsFound,
    long ElapsedMilliseconds);

// This logic does not depend on Bitmap or WinForms and is also tested on Linux.
/// <summary>
/// Counts RGB frequencies and represents each optional shade group with an actual color from the image.
/// </summary>
internal sealed class PaletteHistogram(bool groupSimilar)
{
    private const int BytesPerPixel = 4;
    private const int GroupChannelStep = 8;
    private readonly Dictionary<int, int> _frequencies = new();
    private int _totalPixels;

    public void AddBgraRow(ReadOnlySpan<byte> row)
    {
        if (row.Length % BytesPerPixel != 0)
        {
            throw new ArgumentException("A BGRA row must contain complete four-byte pixels.", nameof(row));
        }

        for (int i = 0; i < row.Length; i += BytesPerPixel)
        {
            int blue = row[i];
            int green = row[i + 1];
            int red = row[i + 2];
            int key = (red << 16) | (green << 8) | blue;
            _frequencies[key] = _frequencies.GetValueOrDefault(key) + 1;
            _totalPixels = checked(_totalPixels + 1);
        }
    }

    public PaletteAnalysisResult Complete(int maxResults, long elapsedMilliseconds = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        // The bucket center is only a grouping key, never the displayed color.
        // Store the group's most frequent original RGB separately from its total frequency.
        var colors = GetDisplayFrequencies();
        // Calculate percentages over the full region before the top-N limit; listed shares may sum to less than 100%.
        var palette = colors
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(maxResults)
            .Select(pair => ColorDetails.FromColor(
                UnpackRgb(pair.Key),
                pair.Value, pair.Value * 100.0 / _totalPixels))
            .ToList();
        return new(palette, _totalPixels, colors.Count, elapsedMilliseconds);
    }

    private List<KeyValuePair<int, int>> GetDisplayFrequencies()
    {
        if (!groupSimilar)
        {
            return _frequencies.ToList();
        }

        return _frequencies
            .GroupBy(pair => GroupKey(pair.Key))
            .Select(SummarizeGroup)
            .ToList();
    }

    private static KeyValuePair<int, int> SummarizeGroup(IEnumerable<KeyValuePair<int, int>> group)
    {
        // Break frequency ties using the lower RGB value so scan order cannot change the result.
        int representativeRgb = group.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key;
        int pixelCount = group.Sum(pair => pair.Value);
        return new KeyValuePair<int, int>(representativeRgb, pixelCount);
    }

    private static Color UnpackRgb(int rgb) => Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

    private static int Quantize(int channel) => Math.Min(255, (channel + GroupChannelStep / 2) / GroupChannelStep * GroupChannelStep);

    private static int GroupKey(int rgb) =>
        (Quantize((rgb >> 16) & 255) << 16) | (Quantize((rgb >> 8) & 255) << 8) | Quantize(rgb & 255);
}
