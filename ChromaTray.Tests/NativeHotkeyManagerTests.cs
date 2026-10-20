using System.Windows.Forms;
using ChromaTray.Models;
using ChromaTray.Services;
using ChromaTray.Tests.Diagnostics;
using Xunit;

namespace ChromaTray.Tests;

public class NativeHotkeyManagerTests
{
    [Theory(DisplayName = "[CT-WIN32-001] Win32 modifier bitmask matches user32.dll specification")]
    [InlineData(false, false, true, 0x0001u)]  // MOD_ALT
    [InlineData(true, false, false, 0x0002u)]  // MOD_CONTROL
    [InlineData(false, true, false, 0x0004u)]  // MOD_SHIFT
    [InlineData(true, true, false, 0x0006u)]   // MOD_CONTROL | MOD_SHIFT
    [InlineData(true, true, true, 0x0007u)]    // MOD_CONTROL | MOD_SHIFT | MOD_ALT
    public void BuildWin32Modifiers_ComputesExactBitmask(bool ctrl, bool shift, bool alt, uint expectedMask)
    {
        var binding = new HotkeyBinding(ctrl, shift, alt, Keys.X);
        uint actualMask = NativeHotkeyManager.BuildWin32Modifiers(binding);

        DiagAssert.Equal(expectedMask, actualMask,
            "CT-WIN32-001", "Services/NativeHotkeyManager", "Win32 RegisterHotKey fsModifiers bitmask calculation",
            $"Ctrl={ctrl}, Shift={shift}, Alt={alt}",
            "Win32 user32.dll constants must be: MOD_ALT=0x0001, MOD_CONTROL=0x0002, MOD_SHIFT=0x0004.");
    }
}
