using System.Windows.Forms;

namespace ChromaTray.Models;

/// <summary>
/// Сочетание основной клавиши и модификаторов; Clone позволяет редактировать его без изменения оригинала.
/// </summary>
public class HotkeyBinding
{
    public bool Ctrl
    {
        get; set;
    }

    public bool Shift
    {
        get; set;
    }

    public bool Alt
    {
        get; set;
    }

    public Keys Key
    {
        get; set;
    }

    public HotkeyBinding()
    {
    }

    public HotkeyBinding(bool ctrl, bool shift, bool alt, Keys key)
    {
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
        Key = key;
    }

    public HotkeyBinding Clone() => new(Ctrl, Shift, Alt, Key);

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
