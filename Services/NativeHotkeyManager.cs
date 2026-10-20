using System.Runtime.InteropServices;
using ChromaTray.Models;

namespace ChromaTray.Services;

public static class NativeHotkeyManager
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int HOTKEY_ID_POINT = 9001;
    public const int HOTKEY_ID_REGION = 9002;
    public const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    public static uint BuildWin32Modifiers(HotkeyBinding binding)
    {
        uint modifiers = 0;
        if (binding.Ctrl) modifiers |= MOD_CONTROL;
        if (binding.Shift) modifiers |= MOD_SHIFT;
        if (binding.Alt) modifiers |= MOD_ALT;
        return modifiers;
    }

    public static bool Register(IntPtr handle, int id, HotkeyBinding binding)
    {
        UnregisterHotKey(handle, id);
        uint modifiers = BuildWin32Modifiers(binding);
        return RegisterHotKey(handle, id, modifiers | 0x4000 /* MOD_NOREPEAT */, (uint)binding.Key);
    }

    public static void UnregisterAll(IntPtr handle)
    {
        UnregisterHotKey(handle, HOTKEY_ID_POINT);
        UnregisterHotKey(handle, HOTKEY_ID_REGION);
    }
}
