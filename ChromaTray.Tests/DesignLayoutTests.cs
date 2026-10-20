using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ChromaTray.Forms;
using ChromaTray.Models;
using Xunit;

namespace ChromaTray.Tests;

[CollectionDefinition("Desktop UI", DisableParallelization = true)]
public sealed class DesktopUiCollection { }

[Collection("Desktop UI")]
public class DesignLayoutTests
{
    [Theory]
    [InlineData(1280, 840)]
    [InlineData(1060, 700)]
    public void MainActions_RemainVisibleAtSupportedWindowSizes(int width, int height)
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.ClientSize = new Size(width, height);
            dashboard.Show();
            dashboard.PerformLayout();
            foreach (var name in new[] { "PickPointButton", "ScanRegionButton", "SettingsButton", "ExportPaletteButton", "PaletteTable", "CopyFormatsButton" })
            {
                var control = Assert.Single(dashboard.Controls.Find(name, true));
                var onForm = dashboard.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
                Assert.True(onForm.Width > 30 && onForm.Height > 20, $"{name} collapsed at {width}×{height}.");
                Assert.True(dashboard.ClientRectangle.Contains(onForm), $"{name} is clipped at {width}×{height}.");
            }
        });
    }

    [Fact]
    public void PaletteSelection_UpdatesInspectorAndRendersPreviews()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            dashboard.UpdatePaletteList([
                ColorDetails.FromColor(Color.FromArgb(181, 166, 255), 480, 48),
                ColorDetails.FromColor(Color.FromArgb(116, 225, 193), 240, 24),
                ColorDetails.FromColor(Color.FromArgb(255, 153, 166), 160, 16),
                ColorDetails.FromColor(Color.FromArgb(245, 191, 113), 80, 8),
                ColorDetails.FromColor(Color.FromArgb(117, 169, 255), 40, 4)
            ], "Region 40×25 px · 1,000 pixels · 5 colors");
            SavePreview(dashboard, "dashboard.png");
            var table = Assert.IsAssignableFrom<ListView>(Assert.Single(dashboard.Controls.Find("PaletteTable", true)));
            table.Items[0].Selected = false;
            table.Items[1].Selected = true;
            var hex = Assert.IsType<TextBox>(Assert.Single(dashboard.Controls.Find("ValueHex", true)));
            Assert.Equal("#74E1C1", hex.Text);
            Assert.Equal(5, table.Items.Count);
            Assert.Contains("#74E1C1", table.Items[1].ToolTipText);
            using var settings = new SettingsForm(new AppSettings());
            settings.Show();
            SavePreview(settings, "settings.png");
            var screen = new Bitmap(960, 640);
            using (var g = Graphics.FromImage(screen))
            {
                g.Clear(Color.FromArgb(80, 70, 120));
                using var brush = new SolidBrush(Color.FromArgb(181, 166, 255));
                g.FillRectangle(brush, 220, 180, 220, 220);
            }
            using var overlay = new ScreenOverlayForm(CaptureMode.Point, screen, new Rectangle(0, 0, 960, 640),
                [new(0, 0, 960, 640)], _ => { }, new(300, 280));
            _ = overlay.Handle;
            SavePreview(overlay, "point-picker.png");
        });
    }

    private static void SavePreview(Form form, string filename)
    {
        var directory = Environment.GetEnvironmentVariable("CHROMATRAY_UI_PREVIEW_DIR");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        form.PerformLayout();
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
        image.Save(Path.Combine(directory, filename), ImageFormat.Png);
    }
}
