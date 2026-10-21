using System.Runtime.InteropServices;

namespace ChromaTray.Services;

// Exclude the window before hiding it and disable its DWM transition. A fixed
// delay alone can capture a fading dashboard (or a closing tray/dropdown menu).
internal sealed class DesktopCaptureScope : IDisposable
{
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
            _affinityChanged = SetWindowDisplayAffinity(_window, 0x11 /* WDA_EXCLUDEFROMCAPTURE */);
        if (DwmGetWindowAttribute(_window, 3 /* DWMWA_TRANSITIONS_FORCEDISABLED */, out _previousTransitions, sizeof(int)) == 0)
        {
            int disabled = 1;
            _transitionsChanged = DwmSetWindowAttribute(_window, 3, ref disabled, sizeof(int)) == 0;
        }
        owner.Hide();
        ShowWindow(_window, 0 /* SW_HIDE */);
    }

    public async Task WaitForDesktopAsync()
    {
        // Return to the message loop so click-up/close notifications and native
        // menu teardown complete before waiting for the compositor's frame.
        await Task.Yield();
        _ = DwmFlush();
        await Task.Delay(350);
        _ = DwmFlush();
        if (IsWindowVisible(_window))
            throw new InvalidOperationException("The dashboard is still visible. Capture was cancelled to avoid sampling the application.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!IsWindow(_window)) return;
        if (_affinityChanged) _ = SetWindowDisplayAffinity(_window, _previousAffinity);
        if (_transitionsChanged)
        {
            int value = _previousTransitions;
            _ = DwmSetWindowAttribute(_window, 3, ref value, sizeof(int));
        }
    }

    [DllImport("user32.dll")] private static extern bool GetWindowDisplayAffinity(IntPtr window, out uint affinity);
    [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
