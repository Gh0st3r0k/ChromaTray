using System.Windows.Forms;
using ChromaTray.Models;

namespace ChromaTray.Services;

public record ValidationResult(bool IsValid, string ErrorCode, string Message)
{
    public static ValidationResult Success() => new(true, "OK", string.Empty);
    public static ValidationResult Fail(string code, string message) => new(false, code, message);
}

public static class SettingsValidator
{
    public static bool IsModifierOnlyKey(Keys keyCode) =>
        keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin
            or Keys.LControlKey or Keys.RControlKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LMenu or Keys.RMenu;

    public static ValidationResult ValidateHotkeyBinding(HotkeyBinding binding)
    {
        if (IsModifierOnlyKey(binding.Key) || binding.Key == Keys.None ||
            !Enum.IsDefined(binding.Key) || (binding.Key & Keys.Modifiers) != 0)
        {
            return ValidationResult.Fail(
                "ERR_HOTKEY_MISSING_MAIN_KEY",
                "Hotkey must include a primary non-modifier key.");
        }

        bool isFunctionKey = binding.Key >= Keys.F1 && binding.Key <= Keys.F12;
        if (binding.Key == Keys.F12)
            return ValidationResult.Fail("ERR_HOTKEY_RESERVED", "F12 is reserved by Windows for debugging.");
        if (!binding.Ctrl && !binding.Shift && !binding.Alt && !isFunctionKey)
        {
            return ValidationResult.Fail(
                "ERR_HOTKEY_NO_MODIFIER",
                "Please include Ctrl, Shift, or Alt (or use F1–F11).");
        }

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateSettingsState(
        HotkeyBinding pointHotkey,
        HotkeyBinding regionHotkey,
        IReadOnlyCollection<ColorFormat> activeFormats)
    {
        var pointCheck = ValidateHotkeyBinding(pointHotkey);
        if (!pointCheck.IsValid) return pointCheck;

        var regionCheck = ValidateHotkeyBinding(regionHotkey);
        if (!regionCheck.IsValid) return regionCheck;

        if (pointHotkey.ToString().Equals(regionHotkey.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Fail(
                "ERR_HOTKEY_DUPLICATE",
                "Point and Region hotkeys cannot be identical.");
        }

        if (activeFormats == null || activeFormats.Count == 0)
        {
            return ValidationResult.Fail(
                "ERR_FORMATS_EMPTY",
                "At least 1 color format must remain enabled.");
        }

        if (activeFormats.Any(format => !Enum.IsDefined(format)))
            return ValidationResult.Fail("ERR_FORMAT_UNKNOWN", "An unknown color format is selected.");

        return ValidationResult.Success();
    }
}
