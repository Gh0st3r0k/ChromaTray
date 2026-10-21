using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChromaTray.Forms;
using ChromaTray.Models;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

[Collection("Desktop UI")]
public class OverlayKeyboardTests
{
    [Fact]
    public void BothCaptureModes_PreserveExistingTextEdgePixelsExactly()
    {
        OnSta(() =>
        {
            Color[] edge = [Color.FromArgb(7, 7, 7), Color.FromArgb(0, 145, 183),
                Color.FromArgb(153, 206, 236), Color.FromArgb(238, 235, 220), Color.FromArgb(132, 0, 0)];
            Bitmap Fixture()
            {
                var bitmap = new Bitmap(10, 10);
                for (int y = 0; y < 10; y++)
                    for (int x = 0; x < 10; x++) bitmap.SetPixel(x, y, edge[x % edge.Length]);
                return bitmap;
            }
            using var point = new OverlayHarness(CaptureMode.Point, Fixture(), _ => { });
            point.SendKey(Keys.Right);
            point.SendKey(Keys.Enter);
            Assert.Equal(edge[1].ToArgb(), point.PickedPointColor!.Value.ToArgb());

            using var region = new OverlayHarness(CaptureMode.Region, Fixture(), _ => { });
            region.Shown += (_, _) => region.SelectRegion(new(0, 0), new(9, 9));
            Assert.Equal(DialogResult.OK, region.ShowDialog());
            var cropped = Assert.IsType<Bitmap>(region.PickedRegionBitmap);
            Assert.Equal(new Size(9, 9), cropped.Size);
            for (int y = 0; y < cropped.Height; y++)
                for (int x = 0; x < cropped.Width; x++) Assert.Equal(edge[x % edge.Length].ToArgb(), cropped.GetPixel(x, y).ToArgb());
            var palette = PaletteAnalyzer.AnalyzeBitmap(cropped, false);
            Assert.Equal(5, palette.UniqueColorsFound);
            Assert.Equal(edge.Select(color => color.ToArgb()).OrderBy(value => value),
                palette.Palette.Select(color => color.RawColor.ToArgb()).OrderBy(value => value));
        });
    }

    [Fact]
    public void ArrowThenEnter_SelectsAdjustedPixelAndSynchronizesCursor()
    {
        OnSta(() =>
        {
            var bitmap = new Bitmap(10, 10);
            bitmap.SetPixel(6, 5, Color.Red);
            var moved = new List<Point>();
            using var overlay = new OverlayHarness(CaptureMode.Point, bitmap, moved.Add);
            Assert.True(overlay.SendKey(Keys.Right));
            Assert.Equal(new Point(-94, -45), overlay.SelectedScreenPoint);
            Assert.Equal(new Point(-94, -45), Assert.Single(moved));
            Assert.True(overlay.SendKey(Keys.Enter));
            Assert.Equal(DialogResult.OK, overlay.DialogResult);
            Assert.Equal(Color.Red.ToArgb(), overlay.PickedPointColor!.Value.ToArgb());
        });
    }

    [Fact]
    public void MouseThenKeyboard_ContinueFromTheSameSelection()
    {
        OnSta(() =>
        {
            var bitmap = new Bitmap(10, 10);
            bitmap.SetPixel(4, 7, Color.Lime);
            using var overlay = new OverlayHarness(CaptureMode.Point, bitmap, _ => { });
            overlay.MoveMouse(new(3, 6));
            overlay.SendKey(Keys.Down);
            overlay.SendKey(Keys.Right);
            overlay.SendKey(Keys.Enter);
            Assert.Equal(Color.Lime.ToArgb(), overlay.PickedPointColor!.Value.ToArgb());
        });
    }

    [Fact]
    public void EscapeAfterMoving_DoesNotPickAColor()
    {
        OnSta(() =>
        {
            using var overlay = new OverlayHarness(CaptureMode.Point, new Bitmap(10, 10), _ => { });
            overlay.SendKey(Keys.Left);
            overlay.SendKey(Keys.Escape);
            Assert.Equal(DialogResult.Cancel, overlay.DialogResult);
            Assert.Null(overlay.PickedPointColor);
        });
    }

    [Fact]
    public void EnterInRegionMode_DoesNotConfirmAPoint()
    {
        OnSta(() =>
        {
            using var overlay = new OverlayHarness(CaptureMode.Region, new Bitmap(10, 10), _ => { });
            overlay.SendKey(Keys.Enter);
            Assert.Null(overlay.PickedPointColor);
            Assert.NotEqual(DialogResult.OK, overlay.DialogResult);
        });
    }

    internal static void OnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            IntPtr previousDpi = IntPtr.Zero;
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: true);
                previousDpi = SetThreadDpiAwarenessContext(new IntPtr(-4));
                action();
            }
            catch (Exception ex) { failure = ex; }
            finally { if (previousDpi != IntPtr.Zero) SetThreadDpiAwarenessContext(previousDpi); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "Windows UI test timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

    private sealed class OverlayHarness(CaptureMode mode, Bitmap bitmap, Action<Point> moveCursor)
        : ScreenOverlayForm(mode, bitmap, new Rectangle(-100, -50, 10, 10), [new(-100, -50, 10, 10)], moveCursor, new(-95, -45))
    {
        public bool SendKey(Keys key) { var message = new Message(); return ProcessCmdKey(ref message, key); }
        public void MoveMouse(Point point) => OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
        public void SelectRegion(Point start, Point end)
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, start.X, start.Y, 0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, end.X, end.Y, 0));
        }
    }
}
