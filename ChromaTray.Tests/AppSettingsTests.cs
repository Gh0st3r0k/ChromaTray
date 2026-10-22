using System.Windows.Forms;
using ChromaTray.Models;
using ChromaTray.Tests.Diagnostics;
using Xunit;

namespace ChromaTray.Tests;

public class AppSettingsTests : IDisposable
{
    private readonly string _tempDir;

    public AppSettingsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ChromaTray_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact(DisplayName = "[CT-CFG-001] Settings round-trip JSON persistence preserves hotkeys and formats")]
    public void SaveAndLoad_RoundTrip_PreservesAllCustomConfiguration()
    {
        string filePath = Path.Combine(_tempDir, "settings.json");
        var original = new AppSettings
        {
            PointHotkey = new HotkeyBinding(ctrl: false, shift: true, alt: true, key: Keys.C),
            RegionHotkey = new HotkeyBinding(ctrl: true, shift: false, alt: true, key: Keys.R),
            ActiveFormats = [ColorFormat.Rgb, ColorFormat.Cmyk],
            GroupSimilarColors = true
        };

        original.Save(filePath);
        var loaded = AppSettings.Load(filePath);
        Assert.True(loaded.GroupSimilarColors);

        DiagAssert.Equal("Shift+Alt+C", loaded.PointHotkey.ToString(),
            "CT-CFG-001A", "Models/AppSettings", "Persist PointHotkey to JSON", filePath,
            "Verify HotkeyBinding properties have public getters and setters for System.Text.Json.");

        DiagAssert.Equal("Ctrl+Alt+R", loaded.RegionHotkey.ToString(),
            "CT-CFG-001B", "Models/AppSettings", "Persist RegionHotkey to JSON", filePath,
            "Verify RegionHotkey serialization.");

        DiagAssert.Equal(2, loaded.ActiveFormats.Count,
            "CT-CFG-001C", "Models/AppSettings", "Persist ActiveFormats count", "Saved=[Rgb, Cmyk]",
            "Ensure ActiveFormats list is not duplicated or lost during deserialization.");

        DiagAssert.Equal(ColorFormat.Rgb, loaded.ActiveFormats[0],
            "CT-CFG-001D", "Models/AppSettings", "Preserve ActiveFormats order", "Primary format=Rgb",
            "First element in ActiveFormats determines the primary format and must preserve order.");
    }

    [Fact(DisplayName = "[CT-CFG-002] Loading JSON with empty ActiveFormats auto-recovers to [Hex]")]
    public void Load_EmptyFormatsArrayInJson_EnforcesMinimumOneFormatFallback()
    {
        string filePath = Path.Combine(_tempDir, "empty_formats.json");
        File.WriteAllText(filePath, """{ "ActiveFormats": [] }""");

        var loaded = AppSettings.Load(filePath);

        DiagAssert.Equal(1, loaded.ActiveFormats.Count,
            "CT-CFG-002A", "Models/AppSettings.Load", "Minimum 1 format self-healing on load", "JSON: { \"ActiveFormats\": [] }",
            "If settings.json was manually edited to have 0 formats, Load() must fallback to [ColorFormat.Hex].");

        DiagAssert.Equal(ColorFormat.Hex, loaded.ActiveFormats[0],
            "CT-CFG-002B", "Models/AppSettings.Load", "Fallback format is HEX", "JSON: { \"ActiveFormats\": [] }",
            "Default fallback format must be ColorFormat.Hex.");
    }

    [Fact(DisplayName = "[CT-CFG-003] Corrupted JSON file gracefully falls back to default settings")]
    public void Load_CorruptedJsonFile_DoesNotCrashAndReturnsDefaults()
    {
        string filePath = Path.Combine(_tempDir, "corrupted.json");
        File.WriteAllText(filePath, "{ invalid json syntax /// ");

        var loaded = AppSettings.Load(filePath);

        DiagAssert.Equal("Ctrl+Shift+X", loaded.PointHotkey.ToString(),
            "CT-CFG-003", "Models/AppSettings.Load", "Graceful recovery from corrupted JSON", "File content: '{ invalid json...'",
            "AppSettings.Load must catch JsonException and return a fresh default AppSettings instance.");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
        }
    }
}
