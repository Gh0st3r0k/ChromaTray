using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChromaTray.Forms;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

[Collection("Desktop UI")]
public class AppearanceRegressionTests
{
    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(192)]
    public void DpiChanges_DoNotOverlapHeaderOrClipCopyButtons(int dpi)
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            ApplyDpiChange(dashboard, dpi);
            dashboard.PerformLayout();
            var header = Find(dashboard, "MainHeader");
            var actions = Find(dashboard, "CaptureActions");
            var settings = Find(dashboard, "SettingsButton");
            Assert.True(header.Bottom <= actions.Top, "The header overlaps the capture actions.");
            AssertContainedByAncestors(settings, dashboard);
            Assert.True(settings.Height >= 40 * dpi / 96, "Settings button is shorter than its drawing metrics.");
            SwitchWorkspace(dashboard, "InspectorViewButton");
            AssertContainedByAncestors(Find(dashboard, "CopyFormatsButton"), dashboard);
            foreach (var format in Enum.GetValues<ColorFormat>())
            {
                var copy = Find(dashboard, $"Copy{format}");
                Assert.True(copy.Width >= 70 * dpi / 96, $"{format} copy button is too narrow.");
                Assert.True(copy.Height >= 32 * dpi / 96, $"{format} copy button is too short.");
                Assert.True(copy.Parent!.ClientRectangle.Contains(copy.Bounds), $"{format} copy button is clipped by its row.");
            }
            Assert.True(Find(dashboard, "FormatsViewport").Height >= 48 * dpi / 96, "The inspector cannot display one complete format row.");
            SavePreview(dashboard, $"inspector-dpi-{dpi}.png");
            SwitchWorkspace(dashboard, "PaletteViewButton");
            AssertContainedByAncestors(Find(dashboard, "ExportPaletteButton"), dashboard);
            var palette = Find(dashboard, "PaletteTable");
            Assert.True(palette.Height >= 72 * dpi / 96, $"The palette cannot display one complete row at DPI {dpi} (height {palette.Height}).");
            SavePreview(dashboard, $"dashboard-dpi-{dpi}.png");
        });
    }

    [Fact]
    public void PaletteScrolling_SelectionAndThumbWorkWithFiveHundredColors()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            var colors = Enumerable.Range(0, 500).Select(i => ColorDetails.FromColor(Color.FromArgb(i % 256, i / 2 % 256, i / 3 % 256), 1, .2)).ToList();
            dashboard.UpdatePaletteList(colors, "500 colors");
            var grid = Assert.IsType<PaletteGrid>(Find(dashboard, "PaletteTable"));
            Assert.True(grid.ScrollBar.Visible);
            grid.Focus();
            var point = grid.PointToScreen(new Point(40, 80));
            SendMessage(grid.Handle, 0x020A /* WM_MOUSEWHEEL */, new IntPtr(unchecked(-120 << 16)), new IntPtr((point.Y << 16) | (point.X & 0xffff)));
            Assert.True(grid.ScrollOffset > 0, "The mouse wheel did not scroll the palette.");
            grid.SelectedIndex = 499;
            Assert.True(grid.ScrollOffset > 0);
            Assert.Equal(colors[499], grid.SelectedColor);
            Assert.True(grid.ScrollBar.ThumbBounds.Bottom <= grid.ScrollBar.Height);
            grid.SelectedIndex = 0;
            Assert.Equal(0, grid.ScrollOffset);
            grid.ScrollBy(grid.ScrollBar.Maximum);
            Assert.Equal(grid.ScrollBar.Maximum, grid.ScrollOffset);
            Assert.Equal(StudioTheme.Surface, grid.ScrollBar.BackColor);
            SavePreview(dashboard, "palette-scroll.png");
        });
    }

    [Fact]
    public void ExportPopup_IsThemedAndSelectingAnItemChangesTheExportMode()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            var select = Assert.IsType<StudioSelect>(Find(dashboard, "ExportMode"));
            select.OpenDropDown();
            Assert.True(select.DropDown.Visible);
            Assert.IsType<StudioMenuRenderer>(select.DropDown.Renderer);
            Assert.Equal(StudioTheme.Surface, select.DropDown.BackColor);
            Assert.Equal(4, select.DropDown.Items.Count);
            SavePreview(select.DropDown, "export-popup.png");
            select.DropDown.Items[2].PerformClick();
            Assert.Equal(2, select.SelectedIndex);
            Assert.False(select.DropDown.Visible);
            SavePreview(dashboard, "export-selector.png");
        });
    }

    [Fact]
    public void CapturePreparation_HidesTheNativeWindowWithoutReducingOpacity()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            Assert.True(DesktopCaptureScope.IsWindowVisible(dashboard.Handle));
            double opacity = dashboard.Opacity;
            using (new DesktopCaptureScope(dashboard))
            {
                Assert.False(dashboard.Visible);
                Assert.False(DesktopCaptureScope.IsWindowVisible(dashboard.Handle));
                Assert.Equal(opacity, dashboard.Opacity);
            }
            dashboard.Show();
            Assert.True(DesktopCaptureScope.IsWindowVisible(dashboard.Handle));
        });
    }

    [Fact]
    public void PointOverlay_PreservesOriginalScreenColorsAndUsesOneCustomCursor()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            var source = Color.FromArgb(199, 255, 216);
            var bitmap = new Bitmap(960, 640);
            using (var g = Graphics.FromImage(bitmap)) g.Clear(source);
            using var overlay = new ScreenOverlayForm(CaptureMode.Point, bitmap, new Rectangle(0, 0, 960, 640),
                [new(0, 0, 960, 640)], _ => { }, new(300, 280));
            _ = overlay.Handle;
            Assert.NotEqual(Cursors.Cross.Handle, overlay.Cursor.Handle);
            Assert.Equal(new Point(16, 16), overlay.Cursor.HotSpot);
            using var rendered = new Bitmap(960, 640);
            overlay.DrawToBitmap(rendered, new Rectangle(0, 0, 960, 640));
            Assert.Equal(source.ToArgb(), rendered.GetPixel(20, 500).ToArgb());
            Assert.Equal(source.ToArgb(), rendered.GetPixel(310, 280).ToArgb());
            SavePreview(overlay, "point-picker-untinted.png");
        });
    }

    private static Control Find(Control root, string name) => Assert.Single(root.Controls.Find(name, true));
    private static void SwitchWorkspace(Form form, string buttonName)
    {
        var button = Assert.IsAssignableFrom<Button>(Find(form, buttonName));
        if (button.Visible) { button.PerformClick(); form.PerformLayout(); }
    }
    private static void AssertContainedByAncestors(Control control, Control root)
    {
        var bounds = control.RectangleToScreen(control.ClientRectangle);
        for (Control? parent = control.Parent; parent is not null; parent = parent.Parent)
        {
            var parentBounds = parent.RectangleToScreen(parent.ClientRectangle);
            Assert.True(parentBounds.Contains(bounds), $"{control.Name} {bounds} is clipped by {parent.Name} {parentBounds} at DPI {root.DeviceDpi}.");
            if (parent == root) break;
        }
    }
    private static void ApplyDpiChange(Form form, int dpi)
    {
        if (dpi == form.DeviceDpi) return;
        // Exercise the same WM_DPICHANGED path used when a user moves between
        // differently scaled monitors. This does not emulate a physical display.
        var bounds = new NativeRect { Left = form.Left, Top = form.Top, Right = form.Left + 1280, Bottom = form.Top + 900 };
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
        try
        {
            Marshal.StructureToPtr(bounds, pointer, false);
            SendMessage(form.Handle, 0x02E0, (IntPtr)((dpi << 16) | dpi), pointer);
            Assert.Equal(dpi, form.DeviceDpi);
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }
    private static void SavePreview(Control form, string filename)
    {
        string? directory = Environment.GetEnvironmentVariable("CHROMATRAY_UI_PREVIEW_DIR");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
        image.Save(Path.Combine(directory, filename), ImageFormat.Png);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
