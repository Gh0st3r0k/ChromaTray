using System.Text.Json;
using System.Windows.Forms;
using ChromaTray.Services;

namespace ChromaTray.Models;

public class HotkeyBinding
{
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }
    public bool Alt { get; set; }
    public Keys Key { get; set; }

    public HotkeyBinding() { }

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
        if (Ctrl) parts.Add("Ctrl");
        if (Shift) parts.Add("Shift");
        if (Alt) parts.Add("Alt");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

public class AppSettings
{
    public HotkeyBinding PointHotkey { get; set; } = new(true, true, false, Keys.X);
    public HotkeyBinding RegionHotkey { get; set; } = new(true, true, false, Keys.Z);

    public List<ColorFormat> ActiveFormats { get; set; } =
    [
        ColorFormat.Hex,
        ColorFormat.Rgb,
        ColorFormat.Hsl,
        ColorFormat.Hsv,
        ColorFormat.Cmyk
    ];

    public bool GroupSimilarColors { get; set; } = true;

    private static string GetSettingsFilePath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChromaTray");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static AppSettings Load(string? customFilePath = null)
    {
        try
        {
            string path = customFilePath ?? GetSettingsFilePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    loaded.Normalize();
                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Fall back to default settings if file is corrupted
        }
        return new AppSettings();
    }

    public void Save(string? customFilePath = null)
    {
        Normalize();
        string path = Path.GetFullPath(customFilePath ?? GetSettingsFilePath());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private void Normalize()
    {
        ActiveFormats = (ActiveFormats ?? []).Where(format => Enum.IsDefined(format)).Distinct().ToList();
        if (ActiveFormats.Count == 0) ActiveFormats = [ColorFormat.Hex];

        if (PointHotkey is null || !SettingsValidator.ValidateHotkeyBinding(PointHotkey).IsValid)
            PointHotkey = new(true, true, false, Keys.X);
        if (RegionHotkey is null || !SettingsValidator.ValidateHotkeyBinding(RegionHotkey).IsValid)
            RegionHotkey = new(true, true, false, Keys.Z);
        if (PointHotkey.ToString() == RegionHotkey.ToString())
        {
            PointHotkey = new(true, true, false, Keys.X);
            RegionHotkey = new(true, true, false, Keys.Z);
        }
    }
}
