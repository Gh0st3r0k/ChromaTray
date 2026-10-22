using System.Windows.Forms;
using ChromaTray.Models;

namespace ChromaTray.Services;

/// <summary>
/// The outcome of a settings check.
/// </summary>
/// <param name="IsValid">Whether the input passed validation.</param>
/// <param name="ErrorCode">A stable diagnostic identifier, or OK on success.</param>
/// <param name="Message">A user-facing explanation, or an empty string on success.</param>
public record ValidationResult(bool IsValid, string ErrorCode, string Message)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A valid result with code OK.</returns>
    public static ValidationResult Success() => new(true, "OK", string.Empty);
    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The explanation displayed to the user.</param>
    /// <returns>An invalid result with the supplied code and message.</returns>
    public static ValidationResult Fail(string code, string message) => new(false, code, message);
}

/// <summary>
/// Validates hotkeys and active color formats without changing or saving settings.
/// </summary>
public static class SettingsValidator
{
    /// <summary>
    /// Checks whether a key is only a modifier rather than a primary shortcut key.
    /// </summary>
    /// <param name="keyCode">A Windows Forms key code.</param>
    /// <returns>True for Ctrl, Shift, Alt, or Windows-key variants.</returns>
    public static bool IsModifierOnlyKey(Keys keyCode) =>
        keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin
            or Keys.LControlKey or Keys.RControlKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LMenu or Keys.RMenu;

    /// <summary>
    /// Checks a shortcut for a valid primary key and required modifiers.
    /// </summary>
    /// <param name="binding">The binding to inspect.</param>
    /// <returns>The first validation failure, or success.</returns>
    /// <remarks>F12 is reserved by Windows. F1–F11 may be used without modifiers; other primary keys require Ctrl, Shift, or Alt.</remarks>
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
        {
            return ValidationResult.Fail("ERR_HOTKEY_RESERVED", "F12 is reserved by Windows for debugging.");
        }
        if (!binding.Ctrl && !binding.Shift && !binding.Alt && !isFunctionKey)
        {
            return ValidationResult.Fail(
                "ERR_HOTKEY_NO_MODIFIER",
                "Please include Ctrl, Shift, or Alt (or use F1–F11).");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Checks both shortcuts and the set of enabled color formats.
    /// </summary>
    /// <param name="pointHotkey">The point-picker shortcut.</param>
    /// <param name="regionHotkey">The region-picker shortcut.</param>
    /// <param name="activeFormats">The selected format collection.</param>
    /// <returns>The first validation failure, or success.</returns>
    /// <remarks>Point and region shortcuts must differ, and at least one known color format must remain enabled.</remarks>
    public static ValidationResult ValidateSettingsState(
        HotkeyBinding pointHotkey,
        HotkeyBinding regionHotkey,
        IReadOnlyCollection<ColorFormat> activeFormats)
    {
        var pointCheck = ValidateHotkeyBinding(pointHotkey);
        if (!pointCheck.IsValid)
        {
            return pointCheck;
        }

        var regionCheck = ValidateHotkeyBinding(regionHotkey);
        if (!regionCheck.IsValid)
        {
            return regionCheck;
        }

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
        {
            return ValidationResult.Fail("ERR_FORMAT_UNKNOWN", "An unknown color format is selected.");
        }

        return ValidationResult.Success();
    }
}
