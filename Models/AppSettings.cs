using System.Text.Json;
using System.Windows.Forms;
using ChromaTray.Services;

namespace ChromaTray.Models;

/// <summary>
/// User preferences for hotkeys, color-format order, and shade grouping.
/// Settings are stored under LocalApplicationData/ChromaTray. Invalid JSON falls back to defaults in memory.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Global shortcut used to open the point picker.
    /// </summary>
    public HotkeyBinding PointHotkey { get; set; } = new(true, true, false, Keys.X);

    /// <summary>
    /// Global shortcut used to start region selection.
    /// </summary>
    public HotkeyBinding RegionHotkey { get; set; } = new(true, true, false, Keys.Z);

    /// <summary>
    /// Enabled formats in display order. The first format is used for quick copy.
    /// </summary>
    public List<ColorFormat> ActiveFormats
    {
        get; set;
    } =
    [
        ColorFormat.Hex,
        ColorFormat.Rgb,
        ColorFormat.Hsl,
        ColorFormat.Hsv,
        ColorFormat.Cmyk
    ];

    /// <summary>
    /// Whether region analysis combines nearby shades. Point picking always uses the exact pixel.
    /// </summary>
    public bool GroupSimilarColors
    {
        get; set;
    }

    private static string GetSettingsFilePath()
    {
        string settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChromaTray");
        Directory.CreateDirectory(settingsDirectory);
        return Path.Combine(settingsDirectory, "settings.json");
    }

    /// <summary>
    /// Loads and normalizes preferences, falling back to defaults when the file is missing, unreadable, or invalid JSON.
    /// </summary>
    /// <param name="customFilePath">Optional settings file for an isolated run or test. Null uses the current user profile.</param>
    /// <returns>Validated settings or a new default configuration.</returns>
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
            // Нечитаемый JSON не должен мешать запуску: используем настройки по умолчанию.
        }
        return new AppSettings();
    }

    /// <summary>
    /// Normalizes preferences and replaces the destination file after a complete temporary-file write.
    /// </summary>
    /// <param name="customFilePath">Optional destination. Null uses LocalApplicationData/ChromaTray/settings.json.</param>
    /// <remarks>I/O and permission errors are reported to the caller. A failed temporary write does not truncate the previous settings file.</remarks>
    public void Save(string? customFilePath = null)
    {
        Normalize();
        string path = Path.GetFullPath(customFilePath ?? GetSettingsFilePath());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        // Записываем рядом с основным файлом, затем заменяем его целиком.
        // Ошибка записи не должна обрезать уже сохранённые настройки.
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void Normalize()
    {
        ActiveFormats = (ActiveFormats ?? []).Where(format => Enum.IsDefined(format)).Distinct().ToList();
        if (ActiveFormats.Count == 0)
        {
            ActiveFormats = [ColorFormat.Hex];
        }

        if (PointHotkey is null || !SettingsValidator.ValidateHotkeyBinding(PointHotkey).IsValid)
        {
            PointHotkey = new(true, true, false, Keys.X);
        }
        if (RegionHotkey is null || !SettingsValidator.ValidateHotkeyBinding(RegionHotkey).IsValid)
        {
            RegionHotkey = new(true, true, false, Keys.Z);
        }
        if (PointHotkey.ToString() == RegionHotkey.ToString())
        {
            PointHotkey = new(true, true, false, Keys.X);
            RegionHotkey = new(true, true, false, Keys.Z);
        }
    }
}
