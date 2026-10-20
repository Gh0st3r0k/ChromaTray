using System.Windows.Forms;
using ChromaTray.Models;
using ChromaTray.Services;
using Xunit;

namespace ChromaTray.Tests;

public sealed class SettingsRecoveryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ChromaTray_Recovery_" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("{\"PointHotkey\":null,\"RegionHotkey\":null,\"ActiveFormats\":[999,0,0]}")]
    [InlineData("{\"PointHotkey\":{\"Key\":16},\"ActiveFormats\":null}")]
    [InlineData("{\"PointHotkey\":{\"Key\":88,\"Ctrl\":true},\"RegionHotkey\":{\"Key\":88,\"Ctrl\":true}}")]
    public void DamagedSettings_RecoverToValidBindingsAndFormats(string json)
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, json);
        var loaded = AppSettings.Load(path);
        Assert.True(SettingsValidator.ValidateSettingsState(loaded.PointHotkey, loaded.RegionHotkey, loaded.ActiveFormats).IsValid);
        Assert.All(loaded.ActiveFormats, format => Assert.True(Enum.IsDefined(format)));
        Assert.Equal(loaded.ActiveFormats.Distinct().Count(), loaded.ActiveFormats.Count);
    }

    [Fact]
    public void RepeatedSave_ReplacesTheFileAndLeavesNoTemporaryFiles()
    {
        string path = Path.Combine(_directory, "settings.json");
        var settings = new AppSettings();
        settings.Save(path);
        settings.GroupSimilarColors = false;
        settings.Save(path);
        Assert.False(AppSettings.Load(path).GroupSimilarColors);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void SaveFailure_IsReportedToCaller()
    {
        Directory.CreateDirectory(_directory);
        string blocker = Path.Combine(_directory, "file-not-directory");
        File.WriteAllText(blocker, "test");
        Assert.Throws<IOException>(() => new AppSettings().Save(Path.Combine(blocker, "settings.json")));
    }

    [Theory]
    [InlineData(Keys.LControlKey)]
    [InlineData(Keys.RMenu)]
    [InlineData(Keys.LShiftKey)]
    [InlineData(Keys.F12)]
    [InlineData((Keys)999)]
    [InlineData(Keys.Control | Keys.X)]
    public void InvalidAndReservedKeys_AreRejected(Keys key)
    {
        Assert.False(SettingsValidator.ValidateHotkeyBinding(new(true, false, false, key)).IsValid);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
