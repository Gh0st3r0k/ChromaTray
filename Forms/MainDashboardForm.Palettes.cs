using ChromaTray.Services;

namespace ChromaTray.Forms;

public partial class MainDashboardForm
{
    private void OpenPaletteGenerator(Color? seed = null)
    {
        if (_isCapturing || _isSettingsDialogOpen || _isExiting)
        {
            return;
        }
        _isSettingsDialogOpen = true;
        NativeHotkeyManager.UnregisterAll(Handle);
        try
        {
            using var generator = new PaletteGeneratorForm(seed);
            generator.ShowDialog(this);
        }
        finally
        {
            _isSettingsDialogOpen = false;
            if (!IsDisposed && !_isExiting)
            {
                RegisterConfiguredHotkeys();
            }
        }
    }
}
