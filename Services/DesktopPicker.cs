using ChromaTray.Forms;
using ChromaTray.Models;

namespace ChromaTray.Services;

/// <summary>Prepares a frozen desktop using the same capture path for every picker.</summary>
internal static class DesktopPicker
{
    internal static async Task<ScreenOverlayForm> CreateOverlayAsync(Form window, CaptureMode mode,
        CancellationToken cancellationToken = default)
    {
        using var hidden = new DesktopCaptureScope(window);
        await hidden.WaitForDesktopAsync();
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(window.IsDisposed, window);
        return new ScreenOverlayForm(mode);
    }
}
