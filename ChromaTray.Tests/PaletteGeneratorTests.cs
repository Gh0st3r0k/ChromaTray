using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using ChromaTray.Forms;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

[Collection("Desktop UI")]
public class PaletteGeneratorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InitialGenerationAndPixelReplacementRespectExplicitRegeneration(bool seeded)
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            var handler = new RecordingHandler();
            using var http = new HttpClient(handler);
            using var form = new PaletteGeneratorForm(seeded ? Color.Red : null, new ColormindClient(http));
            form.Show();
            var generate = (Button)form.Controls.Find("GeneratePaletteButton", true).Single();
            PumpUntil(() => handler.Inputs.Count == 1 && generate.Enabled);
            using (var first = JsonDocument.Parse(handler.Inputs[0]))
            {
                var input = first.RootElement.GetProperty("input");
                Assert.Equal(seeded ? JsonValueKind.Array : JsonValueKind.String, input[0].ValueKind);
                Assert.All(input.EnumerateArray().Skip(1), value => Assert.Equal("N", value.GetString()));
            }
            if (seeded)
                Assert.Equal("#FF0000", form.Controls.Find("PaletteHex0", true).Single().Text);

            form.ApplyPickedColor(2, Color.Lime);
            Application.DoEvents();
            Assert.Single(handler.Inputs); // Picking a pixel must not issue another request.
            Assert.Equal("#00FF00", form.Controls.Find("PaletteHex2", true).Single().Text);
            generate.PerformClick();
            PumpUntil(() => handler.Inputs.Count == 2 && generate.Enabled);
            Assert.Equal("#00FF00", form.Controls.Find("PaletteHex2", true).Single().Text);
            for (int i = 0; i < 5; i++)
            {
                var value = form.Controls.Find($"PaletteHex{i}", true).Single();
                Assert.True(value.Width >= TextRenderer.MeasureText(value.Text, value.Font).Width);
                var pick = form.Controls.Find($"PickPalettePixel{i}", true).Single();
                Assert.True(pick.Visible && pick.Enabled);
                Assert.True(pick.Parent!.ClientRectangle.Contains(pick.Bounds));
            }
            if (Environment.GetEnvironmentVariable("CHROMATRAY_UI_PREVIEW_DIR") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                using var preview = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(preview, new Rectangle(Point.Empty, form.Size));
                preview.Save(Path.Combine(directory, $"colormind-generator-seeded-{seeded}.png"));
            }
            using var second = JsonDocument.Parse(handler.Inputs[1]);
            Assert.Equal(new[] { 0, 255, 0 }, second.RootElement.GetProperty("input")[2].EnumerateArray().Select(c => c.GetInt32()));
        });
    }

    [Fact]
    public void ClosingCancelsPendingGeneration()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            var handler = new PendingHandler();
            using var http = new HttpClient(handler);
            using var form = new PaletteGeneratorForm(null, new ColormindClient(http));
            form.Show();
            PumpUntil(() => handler.Token.CanBeCanceled);
            form.Close();
            Application.DoEvents();
            Assert.True(handler.Token.IsCancellationRequested);
        });
    }

    [Fact]
    public void DashboardGeneratorSurvivesHidingForPixelCapture()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var http = new HttpClient(new RecordingHandler());
            using var dashboard = new MainDashboardForm(new ChromaTray.Models.AppSettings());
            dashboard.Show();
            dashboard.OpenPaletteGenerator(Color.Red, new ColormindClient(http));
            var generator = Assert.Single(dashboard.OwnedForms.OfType<PaletteGeneratorForm>());
            Application.DoEvents();
            generator.Hide();
            dashboard.Hide();
            Application.DoEvents();
            Assert.False(generator.IsDisposed);
            Assert.Same(dashboard, generator.Owner);
            dashboard.Show();
            generator.Show();
            Assert.True(generator.Visible);
            Assert.False(generator.Modal);
            generator.Close();
        });
    }

    [Fact]
    public void WebsitePreviewSwapsRolesAndRendersIndependently()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var form = new WebsitePreviewForm([Color.Red, Color.Green, Color.Blue, Color.White, Color.Black]);
            form.Show();
            Application.DoEvents();
            var background = (ComboBox)form.Controls.Find("WebsiteRole0", true).Single();
            var text = (ComboBox)form.Controls.Find("WebsiteRole1", true).Single();
            Assert.Equal(4, background.SelectedIndex);
            background.SelectedIndex = 0;
            Assert.Equal(4, text.SelectedIndex);
            Assert.Equal(0, background.SelectedIndex);
            var canvas = form.Controls.Find("WebsiteCanvas", true).Single();
            using var image = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(image, canvas.ClientRectangle);
            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(2, 2).ToArgb());
            if (Environment.GetEnvironmentVariable("CHROMATRAY_UI_PREVIEW_DIR") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                image.Save(Path.Combine(directory, "website-preview-role-swap.png"));
            }
        });
    }

    private static void PumpUntil(Func<bool> ready)
    {
        var timer = Stopwatch.StartNew();
        while (!ready() && timer.Elapsed < TimeSpan.FromSeconds(5))
            Application.DoEvents();
        Assert.True(ready(), "Palette UI did not finish the operation.");
    }

    private sealed class PendingHandler : HttpMessageHandler
    {
        internal CancellationToken Token;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException();
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        internal List<string> Inputs { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Inputs.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":[[1,2,3],[4,5,6],[7,8,9],[10,11,12],[13,14,15]]}", Encoding.UTF8, "application/json")
            };
        }
    }
}
