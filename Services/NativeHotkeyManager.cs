using System.Runtime.InteropServices;
using ChromaTray.Models;

namespace ChromaTray.Services;

/// <summary>
/// Registers Windows global hotkeys and translates shortcut modifiers into Win32 flags.
/// </summary>
public static class NativeHotkeyManager
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

    /// <summary>
    /// Win32 identifier assigned to the point-picker shortcut.
    /// </summary>
    public const int PointHotkeyId = 9001;
    /// <summary>
    /// Win32 identifier assigned to the region-picker shortcut.
    /// </summary>
    public const int RegionHotkeyId = 9002;
    /// <summary>
    /// The WM_HOTKEY message delivered to the registered window.
    /// </summary>
    public const int HotkeyMessage = 0x0312;

    private const uint AltModifier = 0x0001;
    private const uint ControlModifier = 0x0002;
    private const uint ShiftModifier = 0x0004;
    private const uint NoRepeatModifier = 0x4000;

    /// <summary>
    /// Converts shortcut modifiers into the Win32 modifier bitmask.
    /// </summary>
    /// <param name="binding">The shortcut binding.</param>
    /// <returns>Ctrl, Shift, and Alt flags. Registration adds the no-repeat flag separately.</returns>
    public static uint BuildWin32Modifiers(HotkeyBinding binding)
    {
        uint modifiers = 0;
        if (binding.Ctrl)
        {
            modifiers |= ControlModifier;
        }
        if (binding.Shift)
        {
            modifiers |= ShiftModifier;
        }
        if (binding.Alt)
        {
            modifiers |= AltModifier;
        }
        return modifiers;
    }

    /// <summary>
    /// Replaces the registration for one application shortcut.
    /// </summary>
    /// <param name="handle">The window receiving hotkey messages.</param>
    /// <param name="id">The application-specific hotkey identifier.</param>
    /// <param name="binding">The shortcut to register.</param>
    /// <returns>True when Windows accepted the shortcut.</returns>
    /// <remarks>Requires Windows. A false result commonly means another application already owns the combination.</remarks>
    public static bool Register(IntPtr handle, int id, HotkeyBinding binding)
    {
        UnregisterHotKey(handle, id);
        uint modifiers = BuildWin32Modifiers(binding);
        // Holding a key must trigger capture once, rather than on every auto-repeat event.
        return RegisterHotKey(handle, id, modifiers | NoRepeatModifier, (uint)binding.Key);
    }

    /// <summary>
    /// Releases both application hotkeys for a window.
    /// </summary>
    /// <param name="handle">The window whose registrations should be removed.</param>
    public static void UnregisterAll(IntPtr handle)
    {
        UnregisterHotKey(handle, PointHotkeyId);
        UnregisterHotKey(handle, RegionHotkeyId);
    }
}
