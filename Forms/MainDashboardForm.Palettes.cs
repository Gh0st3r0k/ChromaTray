using ChromaTray.Services;

namespace ChromaTray.Forms;

public partial class MainDashboardForm
{
    private PaletteGeneratorForm? _paletteGenerator;

    internal void OpenPaletteGenerator(Color? seed = null, ColormindClient? client = null)
    {
        if (_paletteGenerator is { IsDisposed: false })
        {
            _paletteGenerator.Show();
            _paletteGenerator.Activate();
            return;
        }
        if (_isCapturing || _isSettingsDialogOpen || _isExiting)
        {
            return;
        }
        // A modeless owned window survives Hide() during desktop capture.
        // Hiding a ShowDialog window ends its modal loop and used to dispose
        // the generator before the asynchronous picker could open.
        var generator = new PaletteGeneratorForm(seed, client);
        _paletteGenerator = generator;
        generator.FormClosed += (_, _) => _paletteGenerator = null;
        generator.Show(this);
    }

    private void OpenWebsitePreview()
    {
        var colors = _currentPalette.Take(5).Select(color => color.RawColor).ToArray();
        new WebsitePreviewForm(colors).Show(this);
    }
}
