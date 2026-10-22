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
    [InlineData(144)]
    [InlineData(192)]
    public void RoundedControls_KeepThePaintedParentVisibleOutsideTheirCurves(int dpi)
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            // The custom fill differs from both Canvas and Surface. This also
            // detects a fix that merely paints a hard-coded corner color.
            var background = Color.FromArgb(47, 69, 93);
            using var host = new Form { ClientSize = new Size(600, 360), BackColor = StudioTheme.Canvas,
                AutoScaleDimensions = new SizeF(96, 96), AutoScaleMode = AutoScaleMode.Dpi };
            var card = new SurfacePanel { Dock = DockStyle.Fill, FillColor = background };
            var container = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var button = new StudioButton { Text = "Rounded button", Bounds = new Rectangle(40, 40, 240, 56) };
            var select = new StudioSelect { Bounds = new Rectangle(40, 120, 240, 48) };
            select.SetItems("All active formats", "JSON palette array");
            container.Controls.Add(button);
            container.Controls.Add(select);
            card.Controls.Add(container);
            host.Controls.Add(card);
            host.Show();
            ApplyDpiChange(host, dpi);

            void AssertCorners(Control control, string state)
            {
                using var rendered = new Bitmap(control.Width, control.Height);
                control.DrawToBitmap(rendered, control.ClientRectangle);
                foreach (int x in new[] { 0, 1, control.Width - 2, control.Width - 1 })
                    foreach (int y in new[] { 0, 1, control.Height - 2, control.Height - 1 })
                        Assert.True(background.ToArgb() == rendered.GetPixel(x, y).ToArgb(),
                            $"{control.GetType().Name} has an incorrect corner at {x},{y}, {state}, DPI {dpi}: {rendered.GetPixel(x, y)}.");
            }
            void MouseEvent(string method, EventArgs args) => typeof(StudioButton)
                .GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(button, [args]);

            foreach (bool primary in new[] { false, true })
            {
                button.Primary = primary;
                MouseEvent("OnMouseLeave", EventArgs.Empty);
                AssertCorners(button, $"normal, primary={primary}");
                MouseEvent("OnMouseEnter", EventArgs.Empty);
                AssertCorners(button, $"hover, primary={primary}");
                MouseEvent("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0));
                AssertCorners(button, $"pressed, primary={primary}");
                MouseEvent("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0));
                button.Focus();
                AssertCorners(button, $"focused, primary={primary}");
                button.Enabled = false;
                AssertCorners(button, $"disabled, primary={primary}");
                button.Enabled = true;
            }
            AssertCorners(select, "closed");
            select.Focus();
            AssertCorners(select, "focused");
            select.OpenDropDown();
            AssertCorners(select, "open");
            select.CloseDropDown();
            SavePreview(host, $"rounded-controls-dpi-{dpi}.png");
        });
    }

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
            var grouping = Assert.IsType<CheckBox>(Find(dashboard, "GroupSimilarToggle"));
            Assert.True(grouping.Visible, "The grouping option is hidden.");
            AssertContainedByAncestors(grouping, dashboard);
            var required = TextRenderer.MeasureText(grouping.Text, grouping.Font);
            Assert.True(grouping.Width >= required.Width + 24 * dpi / 96, "The grouping option's text is clipped.");
            Assert.True(grouping.Height >= required.Height, "The grouping option is too short.");
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
    public void Settings_ExposesGroupingAndCancelLeavesTheLiveSettingsAlone()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            var live = new AppSettings { GroupSimilarColors = false };
            using var settings = new SettingsForm(live);
            settings.Show();
            var grouping = Assert.IsType<CheckBox>(Find(settings, "SettingsGroupSimilarToggle"));
            Assert.True(grouping.Visible);
            AssertContainedByAncestors(grouping, settings);
            Assert.False(grouping.Checked);
            grouping.Checked = true;
            Assert.False(live.GroupSimilarColors);
            Assert.IsAssignableFrom<Button>(Find(settings, "CancelSettingsButton")).PerformClick();
            Assert.False(live.GroupSimilarColors);
        });
    }

    [Fact]
    public void Magnifier_EnlargesExistingColorFringesWithoutBlendingTheirRgb()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            // A ClearType-like edge, including tinted pixels that must stay
            // distinct. Removing their tint would change the actual screenshot.
            Color[] pixels = [Color.FromArgb(7, 7, 7), Color.FromArgb(0, 145, 183),
                Color.FromArgb(153, 206, 236), Color.FromArgb(238, 235, 220),
                Color.FromArgb(132, 0, 0), Color.White];
            var source = new Bitmap(960, 640);
            using (var graphics = Graphics.FromImage(source)) graphics.Clear(pixels[0]);
            for (int row = 0; row < 11; row++)
                for (int col = 0; col < 11; col++) source.SetPixel(295 + col, 275 + row, pixels[col % pixels.Length]);
            using var overlay = new ScreenOverlayForm(CaptureMode.Point, source, new Rectangle(0, 0, 960, 640),
                [new(0, 0, 960, 640)], _ => { }, new(300, 280));
            _ = overlay.Handle;
            using var rendered = new Bitmap(960, 640);
            overlay.DrawToBitmap(rendered, new Rectangle(0, 0, 960, 640));
            int S(int value) => (int)Math.Round(value * overlay.DeviceDpi / 96f);
            int x = 300 + S(30), y = 280 + S(30);
            if (x + S(346) + S(12) > 960) x = 300 - S(346) - S(30);
            if (y + S(268) + S(12) > 640) y = 280 - S(268) - S(30);
            x = Math.Clamp(x, S(12), Math.Max(S(12), 960 - S(346) - S(12)));
            y = Math.Clamp(y, S(12), Math.Max(S(12), 640 - S(268) - S(12)));
            for (int col = 0; col < 11; col++)
            {
                // Sample away from the grid lines and the selected-cell outline.
                var actual = rendered.GetPixel(x + S(16) + col * S(14) + S(14) / 2, y + S(46) + 2 * S(14) + S(14) / 2);
                Assert.Equal(pixels[col % pixels.Length].ToArgb(), actual.ToArgb());
            }
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
