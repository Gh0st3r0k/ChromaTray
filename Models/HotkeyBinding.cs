using System.Windows.Forms;

namespace ChromaTray.Models;

/// <summary>
/// A primary key and its modifiers. Cloning supports editing a shortcut without changing the active binding.
/// </summary>
public class HotkeyBinding
{
    /// <summary>
    /// Whether Ctrl is required.
    /// </summary>
    public bool Ctrl
    {
        get; set;
    }

    /// <summary>
    /// Whether Shift is required.
    /// </summary>
    public bool Shift
    {
        get; set;
    }

    /// <summary>
    /// Whether Alt is required.
    /// </summary>
    public bool Alt
    {
        get; set;
    }

    /// <summary>
    /// The primary non-modifier key.
    /// </summary>
    public Keys Key
    {
        get; set;
    }

    /// <summary>
    /// Creates an empty binding for deserialization or later editing.
    /// </summary>
    public HotkeyBinding()
    {
    }

    /// <summary>
    /// Creates a shortcut from modifier flags and a primary key.
    /// </summary>
    /// <param name="ctrl">Whether Ctrl is required.</param>
    /// <param name="shift">Whether Shift is required.</param>
    /// <param name="alt">Whether Alt is required.</param>
    /// <param name="key">The primary key.</param>
    public HotkeyBinding(bool ctrl, bool shift, bool alt, Keys key)
    {
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
        Key = key;
    }

    /// <summary>
    /// Creates an independent editable copy.
    /// </summary>
    /// <returns>A new binding with the same keys and modifiers.</returns>
    public HotkeyBinding Clone() => new(Ctrl, Shift, Alt, Key);

    /// <summary>
    /// Formats a shortcut for display.
    /// </summary>
    /// <returns>Modifiers in Ctrl, Shift, Alt order followed by the primary key.</returns>
    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl)
        {
            parts.Add("Ctrl");
        }
        if (Shift)
        {
            parts.Add("Shift");
        }
        if (Alt)
        {
            parts.Add("Alt");
        }
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}
