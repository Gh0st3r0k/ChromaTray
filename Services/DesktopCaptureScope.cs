using System.Runtime.InteropServices;

namespace ChromaTray.Services;

// DWM is the Windows desktop compositor. A delay alone cannot guarantee
// that a fading window or closing menu is absent from the snapshot.
/// <summary>
/// Hides the dashboard without an animation while preparing a capture.
/// Disposal restores window flags but does not show the window.
/// </summary>
internal sealed class DesktopCaptureScope : IDisposable
{
    private const uint ExcludeFromCapture = 0x11;
    private const int TransitionsForceDisabled = 3;
    private const int HideWindow = 0;
    private const int DesktopSettleDelayMilliseconds = 350;
    private readonly IntPtr _window;
    private readonly uint _previousAffinity;
    private readonly bool _affinityChanged;
    private readonly int _previousTransitions;
    private readonly bool _transitionsChanged;
    private bool _disposed;

    public DesktopCaptureScope(Form owner)
    {
        _window = owner.Handle;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) && GetWindowDisplayAffinity(_window, out _previousAffinity))
        {
            _affinityChanged = SetWindowDisplayAffinity(_window, ExcludeFromCapture);
        }
        if (DwmGetWindowAttribute(_window, TransitionsForceDisabled, out _previousTransitions, sizeof(int)) == 0)
        {
            int disabled = 1;
            _transitionsChanged = DwmSetWindowAttribute(_window, TransitionsForceDisabled, ref disabled, sizeof(int)) == 0;
        }
        owner.Hide();
        ShowWindow(_window, HideWindow);
    }

    public async Task WaitForDesktopAsync()
    {
        // Let the message loop process button release and menu dismissal,
        // then wait for the compositor to present the next frame.
        await Task.Yield();
        _ = DwmFlush();
        await Task.Delay(DesktopSettleDelayMilliseconds);
        _ = DwmFlush();
        if (IsWindowVisible(_window))
        {
            throw new InvalidOperationException("The dashboard is still visible. Capture was cancelled to avoid sampling the application.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (!IsWindow(_window))
        {
            return;
        }
        if (_affinityChanged)
        {
            _ = SetWindowDisplayAffinity(_window, _previousAffinity);
        }
        if (_transitionsChanged)
        {
            int value = _previousTransitions;
            _ = DwmSetWindowAttribute(_window, TransitionsForceDisabled, ref value, sizeof(int));
        }
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowDisplayAffinity(IntPtr window, out uint affinity);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr window);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
