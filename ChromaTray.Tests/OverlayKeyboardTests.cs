using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChromaTray.Forms;
using ChromaTray.Models;
using Xunit;

namespace ChromaTray.Tests;

[Collection("Desktop UI")]
public class OverlayKeyboardTests
{
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
    }
}
