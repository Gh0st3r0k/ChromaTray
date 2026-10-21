using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

public class PaletteHistogramTests
{
    [Fact]
    public void ExactMode_CombinesRowsAndRanksByActualPixelShare()
    {
        var histogram = new PaletteHistogram(false);
        histogram.AddBgraRow([0, 0, 255, 255, 0, 0, 255, 255, 0, 255, 0, 255]);
        histogram.AddBgraRow([255, 0, 0, 255, 0, 0, 255, 255]);
        var result = histogram.Complete(500);

        Assert.Equal(5, result.TotalPixels);
        Assert.Equal(3, result.UniqueColorsFound);
        Assert.Equal("#FF0000", result.Palette[0].Hex);
        Assert.Equal(3, result.Palette[0].PixelCount);
        Assert.Equal(60, result.Palette[0].Percentage);
        Assert.Equal(100, result.Palette.Sum(color => color.Percentage));
    }

    [Fact]
    public void Grouping_MergesStepEightShadesAndClampsWhite()
    {
        var histogram = new PaletteHistogram(true);
        histogram.AddBgraRow([16, 15, 14, 255, 16, 16, 16, 255, 15, 17, 18, 255, 255, 255, 255, 255]);
        var result = histogram.Complete(500);
        Assert.Equal(2, result.UniqueColorsFound);
        Assert.Equal("#0E0F10", result.Palette[0].Hex);
        Assert.Equal(75, result.Palette[0].Percentage);
        Assert.Equal("#FFFFFF", result.Palette[1].Hex);
    }

    [Fact]
    public void Grouping_UsesMostFrequentActualRgbEvenWhenTheBucketCenterIsAbsent()
    {
        var histogram = new PaletteHistogram(true);
        histogram.AddBgraRow([16, 15, 14, 255]);
        Assert.Equal("#0E0F10", Assert.Single(histogram.Complete(500).Palette).Hex);
        histogram.AddBgraRow([15, 17, 18, 255, 15, 17, 18, 255]);
        var group = Assert.Single(histogram.Complete(500).Palette);
        Assert.Equal("#12110F", group.Hex);
        Assert.Equal(3, group.PixelCount);
        Assert.Equal(100, group.Percentage);
    }

    [Fact]
    public void Grouping_TiesAreIndependentOfPixelScanOrder()
    {
        var forward = new PaletteHistogram(true);
        var reverse = new PaletteHistogram(true);
        forward.AddBgraRow([16, 15, 14, 255, 15, 17, 18, 255]);
        reverse.AddBgraRow([15, 17, 18, 255, 16, 15, 14, 255]);
        Assert.Equal("#0E0F10", Assert.Single(forward.Complete(500).Palette).Hex);
        Assert.Equal(forward.Complete(500).Palette, reverse.Complete(500).Palette);
    }

    [Fact]
    public void ResultCap_DoesNotRenormalizeOrLoseUniqueCount()
    {
        var histogram = new PaletteHistogram(false);
        histogram.AddBgraRow([0, 0, 255, 255, 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255]);
        var result = histogram.Complete(1);
        Assert.Single(result.Palette);
        Assert.Equal(3, result.UniqueColorsFound);
        Assert.Equal(50, result.Palette[0].Percentage);
    }

    [Fact]
    public void EqualFrequency_UsesStableRgbOrdering()
    {
        var histogram = new PaletteHistogram(false);
        histogram.AddBgraRow([0, 0, 255, 255, 255, 0, 0, 255]);
        Assert.Equal(new[] { "#0000FF", "#FF0000" }, histogram.Complete(500).Palette.Select(c => c.Hex));
    }

    [Fact]
    public void EmptyImage_ReturnsNoColors()
    {
        var result = new PaletteHistogram(false).Complete(500);
        Assert.Empty(result.Palette);
        Assert.Equal(0, result.TotalPixels);
    }

    [Fact]
    public void InvalidRowAndResultLimit_AreRejected()
    {
        var histogram = new PaletteHistogram(false);
        Assert.Throws<ArgumentException>(() => histogram.AddBgraRow([1, 2, 3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => histogram.Complete(0));
    }
}
