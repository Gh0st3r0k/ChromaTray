using System.Drawing;
using ChromaTray.Models;

namespace ChromaTray.Services;

public record PaletteAnalysisResult(
    List<ColorDetails> Palette,
    int TotalPixels,
    int UniqueColorsFound,
    long ElapsedMilliseconds);

// Shared by the native bitmap adapter and the platform-independent tests.
internal sealed class PaletteHistogram(bool groupSimilar)
{
    private readonly Dictionary<int, int> _frequencies = new();
    private int _totalPixels;

    public void AddBgraRow(ReadOnlySpan<byte> row)
    {
        if (row.Length % 4 != 0)
            throw new ArgumentException("A BGRA row must contain complete four-byte pixels.", nameof(row));

        for (int i = 0; i < row.Length; i += 4)
        {
            int b = row[i], g = row[i + 1], r = row[i + 2];
            int key = (r << 16) | (g << 8) | b;
            _frequencies[key] = _frequencies.GetValueOrDefault(key) + 1;
            _totalPixels = checked(_totalPixels + 1);
        }
    }

    public PaletteAnalysisResult Complete(int maxResults, long elapsedMilliseconds = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        // Bucket centers are only grouping keys, never output colors. Preserve
        // original frequencies so each group can use its most common real RGB.
        var colors = groupSimilar
            ? _frequencies.GroupBy(pair => GroupKey(pair.Key))
                .Select(group => new KeyValuePair<int, int>(
                    group.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key,
                    group.Sum(pair => pair.Value))).ToList()
            : _frequencies.ToList();
        var palette = colors
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(maxResults)
            .Select(pair => ColorDetails.FromColor(
                Color.FromArgb((pair.Key >> 16) & 255, (pair.Key >> 8) & 255, pair.Key & 255),
                pair.Value, pair.Value * 100.0 / _totalPixels))
            .ToList();
        return new(palette, _totalPixels, colors.Count, elapsedMilliseconds);
    }

    private static int Quantize(int channel) => Math.Min(255, (channel + 4) / 8 * 8);
    private static int GroupKey(int rgb) =>
        (Quantize((rgb >> 16) & 255) << 16) | (Quantize((rgb >> 8) & 255) << 8) | Quantize(rgb & 255);
}
