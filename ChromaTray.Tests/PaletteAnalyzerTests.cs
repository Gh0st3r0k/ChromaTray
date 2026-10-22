using System.Drawing;
using System.Drawing.Imaging;
using ChromaTray.Services;
using ChromaTray.Tests.Diagnostics;
using Xunit;

namespace ChromaTray.Tests;

public class PaletteAnalyzerTests
{
    [Fact(DisplayName = "[CT-SCAN-001] Solid color region yields 1 color with 100% share")]
    public void AnalyzeBitmap_SolidColor_ReturnsSingleColorWith100PercentShare()
    {
        using var bmp = new Bitmap(25, 20, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(255, 0, 0));
        }

        var result = PaletteAnalyzer.AnalyzeBitmap(bmp, groupSimilar: false);

        DiagAssert.Equal(500, result.TotalPixels,
            "CT-SCAN-001A", "Services/PaletteAnalyzer", "Total pixel calculation", "Bitmap 25x20",
            "TotalPixels must equal bmp.Width * bmp.Height.");

        DiagAssert.Equal(1, result.UniqueColorsFound,
            "CT-SCAN-001B", "Services/PaletteAnalyzer", "Solid bitmap unique color count", "Bitmap 25x20 filled #FF0000",
            "Check LockBits stride iteration and BGRA byte offsets (idx+0=B, idx+1=G, idx+2=R).");

        DiagAssert.Equal("#FF0000", result.Palette[0].Hex,
            "CT-SCAN-001C", "Services/PaletteAnalyzer", "Channel order in 32bppArgb buffer", "Solid Red (255,0,0)",
            "In Format32bppArgb little-endian memory, byte[0]=Blue, byte[1]=Green, byte[2]=Red. Ensure R and B are not swapped.");

        DiagAssert.DoubleEqual(100.0, result.Palette[0].Percentage, 0.01,
            "CT-SCAN-001D", "Services/PaletteAnalyzer", "Dominance percentage calculation", "500 of 500 pixels",
            "Percentage formula should be (pixelCount * 100.0) / totalPixels.");
    }

    [Fact(DisplayName = "[CT-SCAN-002] Multi-color region ranks colors by dominance descending")]
    public void AnalyzeBitmap_MultiColorRegion_RanksByDominanceAndComputesExactShares()
    {
        using var bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                // 70% Red, 20% Green, 10% Blue
                Color c = y < 7 ? Color.FromArgb(255, 0, 0)
                        : y < 9 ? Color.FromArgb(0, 255, 0)
                                : Color.FromArgb(0, 0, 255);
                bmp.SetPixel(x, y, c);
            }
        }

        var result = PaletteAnalyzer.AnalyzeBitmap(bmp, groupSimilar: false);

        DiagAssert.Equal(3, result.Palette.Count,
            "CT-SCAN-002A", "Services/PaletteAnalyzer", "Multi-color extraction count", "10x10 Bitmap (70 R, 20 G, 10 B)",
            "All 3 distinct colors should be detected when groupSimilar is false.");

        DiagAssert.Equal("#FF0000", result.Palette[0].Hex,
            "CT-SCAN-002B", "Services/PaletteAnalyzer", "Dominant color sorting (1st place)", "70% Red vs 20% Green vs 10% Blue",
            "Palette must be ordered by frequency descending (.OrderByDescending(kv => kv.Value)).");

        DiagAssert.DoubleEqual(70.0, result.Palette[0].Percentage, 0.01,
            "CT-SCAN-002C", "Services/PaletteAnalyzer", "1st color share percentage", "70 Red pixels out of 100",
            "Verify pixel counter increments per matching RGB key.");

        DiagAssert.Equal("#00FF00", result.Palette[1].Hex,
            "CT-SCAN-002D", "Services/PaletteAnalyzer", "Secondary color sorting (2nd place)", "20% Green",
            "Second most frequent color should be #00FF00.");

        DiagAssert.Equal("#0000FF", result.Palette[2].Hex,
            "CT-SCAN-002E", "Services/PaletteAnalyzer", "Tertiary color sorting (3rd place)", "10% Blue",
            "Least frequent color should be last.");
    }

    [Fact(DisplayName = "[CT-SCAN-003] Shade quantization merges anti-aliased noise into 1 color")]
    public void AnalyzeBitmap_GroupSimilarEnabled_MergesNearIdenticalShades()
    {
        using var bmp = new Bitmap(3, 1, PixelFormat.Format32bppArgb);
        bmp.SetPixel(0, 0, Color.FromArgb(14, 15, 16));
        bmp.SetPixel(1, 0, Color.FromArgb(16, 16, 16));
        bmp.SetPixel(2, 0, Color.FromArgb(18, 17, 15));

        var ungrouped = PaletteAnalyzer.AnalyzeBitmap(bmp, groupSimilar: false);
        var grouped = PaletteAnalyzer.AnalyzeBitmap(bmp, groupSimilar: true);

        DiagAssert.Equal(3, ungrouped.UniqueColorsFound,
            "CT-SCAN-003A", "Services/PaletteAnalyzer", "Exact mode preserves subtle variations", "Pixels: (14,15,16), (16,16,16), (18,17,15)",
            "When groupSimilar=false, RGB channels must not be quantized.");

        DiagAssert.Equal(1, grouped.UniqueColorsFound,
            "CT-SCAN-003B", "Services/PaletteAnalyzer", "Quantized mode merges step-8 neighbors", "Pixels: (14,15,16), (16,16,16), (18,17,15)",
            "Check quantization rounding formula: Math.Min(255, (channel + 4) / 8 * 8).");

        DiagAssert.Equal("#0E0F10", grouped.Palette[0].Hex,
            "CT-SCAN-003C", "Services/PaletteAnalyzer", "Representative is an actual source RGB", "Three equally frequent shades in one bucket",
            "Break frequency ties by RGB. Do not output the synthetic bucket center #101010.");
    }

    [Fact(DisplayName = "[CT-SCAN-004] MaxResults cap limits UI list size while preserving UniqueColorsFound")]
    public void AnalyzeBitmap_RespectsMaxResultsCap()
    {
        using var bmp = new Bitmap(30, 1, PixelFormat.Format32bppArgb);
        for (int x = 0; x < 30; x++)
        {
            bmp.SetPixel(x, 0, Color.FromArgb(x * 8, 0, 0));
        }

        var result = PaletteAnalyzer.AnalyzeBitmap(bmp, groupSimilar: false, maxResults: 10);

        DiagAssert.Equal(30, result.UniqueColorsFound,
            "CT-SCAN-004A", "Services/PaletteAnalyzer", "Total unique colors counter before Take()", "30 distinct pixels, maxResults=10",
            "UniqueColorsFound should report freq.Count before .Take(maxResults) truncates the UI list.");

        DiagAssert.Equal(10, result.Palette.Count,
            "CT-SCAN-004B", "Services/PaletteAnalyzer", "Palette list truncation to maxResults", "maxResults=10",
            "Ensure .Take(maxResults) is applied to the returned Palette list.");
    }
}
