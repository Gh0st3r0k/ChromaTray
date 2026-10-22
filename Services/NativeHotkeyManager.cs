using System.Runtime.InteropServices;
using ChromaTray.Models;

namespace ChromaTray.Services;

/// <summary>
/// Регистрирует глобальные хоткеи Windows и переводит модификаторы в битовую маску Win32.
/// </summary>
public static class NativeHotkeyManager
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

    public const int PointHotkeyId = 9001;
    public const int RegionHotkeyId = 9002;
    public const int HotkeyMessage = 0x0312;

    private const uint AltModifier = 0x0001;
    private const uint ControlModifier = 0x0002;
    private const uint ShiftModifier = 0x0004;
    private const uint NoRepeatModifier = 0x4000;

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

    public static bool Register(IntPtr handle, int id, HotkeyBinding binding)
    {
        UnregisterHotKey(handle, id);
        uint modifiers = BuildWin32Modifiers(binding);
        // Удерживание клавиши должно запускать захват один раз, а не на каждое автоповторение.
        return RegisterHotKey(handle, id, modifiers | NoRepeatModifier, (uint)binding.Key);
    }

    public static void UnregisterAll(IntPtr handle)
    {
        UnregisterHotKey(handle, PointHotkeyId);
        UnregisterHotKey(handle, RegionHotkeyId);
    }
}
