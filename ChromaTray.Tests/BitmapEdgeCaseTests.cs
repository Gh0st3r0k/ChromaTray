using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ChromaTray.Models;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

public class BitmapEdgeCaseTests
{
    [Fact]
    public void PngPalette_DefaultAnalysisPreservesEveryRgbWithoutExtraColors()
    {
        string[] hex = ["#161D6F", "#0B2F9F", "#98DED9", "#C7FFD8"];
        using var source = new Bitmap(8, 4, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++)
                source.SetPixel(x, y, ColorTranslator.FromHtml(hex[x / 2]));
        using var png = new MemoryStream();
        source.Save(png, ImageFormat.Png);
        png.Position = 0;
        using var decoded = new Bitmap(png);
        var result = PaletteAnalyzer.AnalyzeBitmap(decoded, new AppSettings().GroupSimilarColors);

        Assert.Equal(32, result.TotalPixels);
        Assert.Equal(4, result.UniqueColorsFound);
        Assert.Equal(hex.OrderBy(value => value), result.Palette.Select(color => color.Hex).OrderBy(value => value));
        Assert.All(result.Palette, color => Assert.Equal(25, color.Percentage));
    }

    [Fact]
    public void TwentyFourBitBitmap_ConvertsWithoutChangingRgb()
    {
        using var bitmap = new Bitmap(3, 2, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.FromArgb(12, 34, 56));
        var result = PaletteAnalyzer.AnalyzeBitmap(bitmap, false);
        Assert.Single(result.Palette);
        Assert.Equal("#0C2238", result.Palette[0].Hex);
        Assert.Equal(6, result.TotalPixels);
        Assert.Equal(PixelFormat.Format24bppRgb, bitmap.PixelFormat);
    }

    [Fact]
    public void NegativeStrideBitmap_ReadsAllLogicalRows()
    {
        IntPtr buffer = Marshal.AllocHGlobal(16);
        try
        {
            Marshal.Copy(new byte[] { 0, 0, 255, 255, 0, 0, 255, 255, 255, 0, 0, 255, 255, 0, 0, 255 }, 0, buffer, 16);
            using var bitmap = new Bitmap(2, 2, -8, PixelFormat.Format32bppArgb, IntPtr.Add(buffer, 8));
            var result = PaletteAnalyzer.AnalyzeBitmap(bitmap, false);
            Assert.Equal(4, result.TotalPixels);
            Assert.Equal(2, result.UniqueColorsFound);
            Assert.All(result.Palette, color => Assert.Equal(50, color.Percentage));
            Assert.Equal(new[] { "#0000FF", "#FF0000" }, result.Palette.Select(color => color.Hex));
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    [Fact]
    public void CancelledAnalysis_DoesNotLeaveBitmapLocked()
    {
        using var bitmap = new Bitmap(10, 10);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => PaletteAnalyzer.AnalyzeBitmap(bitmap, false, cancellationToken: cancellation.Token));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Red);
        Assert.Equal("#FF0000", PaletteAnalyzer.AnalyzeBitmap(bitmap, false).Palette[0].Hex);
    }
}
