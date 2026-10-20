using System.Windows.Forms;
using ChromaTray.Models;
using ChromaTray.Services;
using ChromaTray.Tests.Diagnostics;
using Xunit;

namespace ChromaTray.Tests;

public class SettingsValidatorTests
{
    [Fact(DisplayName = "[CT-VAL-001] Standalone letter key without Ctrl/Shift/Alt is rejected")]
    public void ValidateHotkeyBinding_SingleLetterWithoutModifiers_FailsWithNoModifierCode()
    {
        var binding = new HotkeyBinding(ctrl: false, shift: false, alt: false, key: Keys.X);
        var res = SettingsValidator.ValidateHotkeyBinding(binding);

        DiagAssert.Equal(false, res.IsValid,
            "CT-VAL-001A", "Services/SettingsValidator", "Reject unmodified alphanumeric key", "Key='X' (no modifiers)",
            "Regular keys without Ctrl/Shift/Alt hijack normal typing and must be rejected.");

        DiagAssert.Equal("ERR_HOTKEY_NO_MODIFIER", res.ErrorCode,
            "CT-VAL-001B", "Services/SettingsValidator", "Error code for missing modifier", "Key='X'",
            "Expected error code ERR_HOTKEY_NO_MODIFIER.");
    }

    [Fact(DisplayName = "[CT-VAL-002] Function keys (F1-F11) are allowed without modifiers")]
    public void ValidateHotkeyBinding_FunctionKeyWithoutModifiers_Succeeds()
    {
        var binding = new HotkeyBinding(ctrl: false, shift: false, alt: false, key: Keys.F8);
        var res = SettingsValidator.ValidateHotkeyBinding(binding);

        DiagAssert.Equal(true, res.IsValid,
            "CT-VAL-002", "Services/SettingsValidator", "Allow standalone F1-F11 keys", "Key='F8'",
            "Check condition: key >= Keys.F1 && key <= Keys.F12.");
    }

    [Fact(DisplayName = "[CT-VAL-003] Modifier-only key press is rejected")]
    public void ValidateHotkeyBinding_ModifierOnlyKey_Fails()
    {
        var binding = new HotkeyBinding(ctrl: true, shift: false, alt: false, key: Keys.ControlKey);
        var res = SettingsValidator.ValidateHotkeyBinding(binding);

        DiagAssert.Equal("ERR_HOTKEY_MISSING_MAIN_KEY", res.ErrorCode,
            "CT-VAL-003", "Services/SettingsValidator", "Reject standalone modifier key", "Ctrl + ControlKey",
            "IsModifierOnlyKey must filter out ControlKey, ShiftKey, Menu (Alt), and LWin/RWin.");
    }

    [Fact(DisplayName = "[CT-VAL-004] Identical Point and Region hotkeys are rejected")]
    public void ValidateSettingsState_IdenticalHotkeys_FailsWithDuplicateCode()
    {
        var hk1 = new HotkeyBinding(true, true, false, Keys.X);
        var hk2 = new HotkeyBinding(true, true, false, Keys.X);
        var formats = new List<ColorFormat> { ColorFormat.Hex };

        var res = SettingsValidator.ValidateSettingsState(hk1, hk2, formats);

        DiagAssert.Equal("ERR_HOTKEY_DUPLICATE", res.ErrorCode,
            "CT-VAL-004", "Services/SettingsValidator", "Prevent hotkey collision between Point and Region",
            "Point=Ctrl+Shift+X, Region=Ctrl+Shift+X",
            "Win32 cannot bind two different actions to the same hotkey combination.");
    }

    [Fact(DisplayName = "[CT-VAL-005] Empty color formats list is rejected (Minimum 1 format rule)")]
    public void ValidateSettingsState_ZeroFormatsSelected_FailsWithFormatsEmptyCode()
    {
        var hk1 = new HotkeyBinding(true, true, false, Keys.X);
        var hk2 = new HotkeyBinding(true, true, false, Keys.Z);
        var emptyFormats = new List<ColorFormat>();

        var res = SettingsValidator.ValidateSettingsState(hk1, hk2, emptyFormats);

        DiagAssert.Equal("ERR_FORMATS_EMPTY", res.ErrorCode,
            "CT-VAL-005", "Services/SettingsValidator", "Enforce minimum 1 color standard",
            "ActiveFormats=[] (0 items)",
            "User must have at least 1 visible color format enabled at all times.");
    }
}
