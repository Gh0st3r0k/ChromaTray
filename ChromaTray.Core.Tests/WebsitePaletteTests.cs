using System.Drawing;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Core.Tests;

public class WebsitePaletteTests
{
    [Fact]
    public void RoleSwapPreservesAllFiveSlotsAndDoesNotMutateSource()
    {
        Color[] colors = [Color.Red, Color.Green, Color.Blue, Color.White, Color.Black];
        var palette = new WebsitePalette(colors);
        palette.Assign(0, 0);
        Assert.Equal(Color.Red, palette[0]);
        Assert.Equal(Color.Black, palette[1]);
        Assert.Equal(5, Enumerable.Range(0, 5).Select(palette.AssignedIndex).Distinct().Count());
        colors[0] = Color.Pink;
        Assert.Equal(Color.Red, palette[0]);
    }

    [Fact]
    public void ContrastUsesWcagRelativeLuminance()
    {
        Assert.Equal(21, WebsitePalette.Contrast(Color.Black, Color.White), 6);
        Assert.Equal(1, WebsitePalette.Contrast(Color.Blue, Color.Blue), 6);
        Assert.Equal(WebsitePalette.Contrast(Color.Red, Color.Green), WebsitePalette.Contrast(Color.Green, Color.Red), 6);
    }
}
