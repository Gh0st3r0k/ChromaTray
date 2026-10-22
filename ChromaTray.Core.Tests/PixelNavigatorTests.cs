using System.Drawing;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

public class PixelNavigatorTests
{
    [Theory]
    [InlineData(-1, 0, 49, 50)]
    [InlineData(1, 0, 51, 50)]
    [InlineData(0, -1, 50, 49)]
    [InlineData(0, 1, 50, 51)]
    public void Arrows_MoveExactlyOnePixel(int dx, int dy, int x, int y)
    {
        Assert.Equal(new Point(x, y), PixelNavigator.Move(new(50, 50), dx, dy, [new(0, 0, 100, 100)]));
    }

    [Theory]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 0, -1)]
    [InlineData(99, 99, 1, 0)]
    [InlineData(99, 99, 0, 1)]
    public void ScreenEdges_KeepSelectionOnTheLastVisiblePixel(int x, int y, int dx, int dy)
    {
        var current = new Point(x, y);
        Assert.Equal(current, PixelNavigator.Move(current, dx, dy, [new(0, 0, 100, 100)]));
    }

    [Fact]
    public void AdjacentDisplays_CanCrossIntoNegativeScreenCoordinates()
    {
        Rectangle[] screens = [new(-100, -50, 100, 100), new(0, 0, 100, 100)];
        Assert.Equal(new Point(-1, 25), PixelNavigator.Move(new(0, 25), -1, 0, screens));
        Assert.Equal(new Point(0, 25), PixelNavigator.Move(new(-1, 25), 1, 0, screens));
        Assert.Equal(new Point(-50, -1), PixelNavigator.Move(new(-50, 0), 0, -1, screens));
    }

    [Fact]
    public void GapsBetweenDisplays_CannotBeSelected()
    {
        Rectangle[] screens = [new(-100, -50, 100, 100), new(0, 0, 100, 100)];
        Assert.Equal(new Point(-1, -10), PixelNavigator.Move(new(-1, -10), 1, 0, screens));
        Assert.Equal(new Point(0, 75), PixelNavigator.Move(new(0, 75), -1, 0, screens));
    }

    [Fact]
    public void RepeatedArrows_DoNotAccumulateBeyondTheScreenEdge()
    {
        var position = new Point(5, 5);
        Rectangle[] screens = [new(0, 0, 10, 10)];
        for (int i = 0; i < 100; i++)
        {
            position = PixelNavigator.Move(position, 1, 0, screens);
        }
        Assert.Equal(new Point(9, 5), position);
        Assert.Equal(new Point(8, 5), PixelNavigator.Move(position, -1, 0, screens));
    }
}
