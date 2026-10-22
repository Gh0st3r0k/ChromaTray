using System.Text.Json;
using System.Windows.Forms;
using ChromaTray.Services;

namespace ChromaTray.Models;

/// <summary>
/// Настройки пользователя: хоткеи, порядок цветовых форматов и группировка.
/// JSON хранится в LocalApplicationData/ChromaTray; повреждённый файл заменяется значениями по умолчанию в памяти.
/// </summary>
public class AppSettings
{
    public HotkeyBinding PointHotkey { get; set; } = new(true, true, false, Keys.X);

    public HotkeyBinding RegionHotkey { get; set; } = new(true, true, false, Keys.Z);

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
