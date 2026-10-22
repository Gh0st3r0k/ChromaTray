using System.Drawing;
using System.Windows.Forms;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

/// <summary>
/// Редактирует копии настроек. Живые настройки меняются только после успешного сохранения.
/// </summary>
public partial class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private HotkeyBinding _editedPointHotkey;
    private HotkeyBinding _editedRegionHotkey;

    private readonly Dictionary<ColorFormat, CheckBox> _formatCheckboxes = new();

    public SettingsForm(AppSettings currentSettings)
    {
        _settings = currentSettings;
        // Cancel должен отбросить изменения: сочетания редактируются независимо от оригинала.
        _editedPointHotkey = currentSettings.PointHotkey.Clone();
        _editedRegionHotkey = currentSettings.RegionHotkey.Clone();
        ConfigureWindow();
        InitializeLayout();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StudioTheme.ApplyWindowChrome(this);
    }

    private static TextBox CreateHotkeyCaptureBox(string text, string name) => new()
    {
        Name = name,
        Text = text,
        ReadOnly = true,
        BackColor = StudioTheme.Raised,
        ForeColor = StudioTheme.Accent,
        BorderStyle = BorderStyle.FixedSingle,
        Font = StudioTheme.Mono,
        TextAlign = HorizontalAlignment.Center,
        Cursor = Cursors.Hand,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 8, 0, 8),
        AccessibleName = name
    };

    private void CaptureKeyInput(KeyEventArgs e, TextBox targetBox, bool isPoint)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        if (SettingsValidator.IsModifierOnlyKey(e.KeyCode))
        {
            return;
        }

        var binding = new HotkeyBinding(e.Control, e.Shift, e.Alt, e.KeyCode);
        var validation = SettingsValidator.ValidateHotkeyBinding(binding);

        if (!validation.IsValid)
        {
            _validationLabel.Text = validation.Message;
            return;
        }

        _validationLabel.Text = "";
        targetBox.Text = binding.ToString();

        if (isPoint)
        {
            _editedPointHotkey = binding;
        }
        else
        {
            _editedRegionHotkey = binding;
        }
    }

    private void OnFormatCheckboxChanged(CheckBox changedBox)
    {
        var activeFormats = _formatCheckboxes.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToList();
        if (activeFormats.Count == 0)
        {
            changedBox.Checked = true;
            _validationLabel.Text = "At least 1 color format must remain enabled.";
        }
        else
        {
            _validationLabel.Text = "";
        }
    }

    private void SaveAndClose()
    {
        var activeFormats = _formatCheckboxes.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToList();
        var stateValidation = SettingsValidator.ValidateSettingsState(_editedPointHotkey, _editedRegionHotkey, activeFormats);

        if (!stateValidation.IsValid)
        {
            _validationLabel.Text = stateValidation.Message;
            return;
        }

        // Сначала сохраняем кандидата. При ошибке записи действующие настройки остаются прежними.
        var candidate = new AppSettings
        {
            PointHotkey = _editedPointHotkey,
            RegionHotkey = _editedRegionHotkey,
            ActiveFormats = activeFormats,
            GroupSimilarColors = _groupSimilarCheckBox.Checked
        };
        try
        {
            candidate.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _validationLabel.Text = $"Could not save settings: {ex.Message}";
            return;
        }
        _settings.PointHotkey = candidate.PointHotkey;
        _settings.RegionHotkey = candidate.RegionHotkey;
        _settings.ActiveFormats = candidate.ActiveFormats;
        _settings.GroupSimilarColors = candidate.GroupSimilarColors;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string GetFormatDescription(ColorFormat format) => format switch
    {
        ColorFormat.Hex => "HEX  — Hexadecimal (#98DED9)",
        ColorFormat.Rgb => "RGB  — Red, Green, Blue (rgb(152, 222, 217))",
        ColorFormat.Hsl => "HSL  — Hue, Saturation, Lightness",
        ColorFormat.Hsv => "HSV  — Hue, Saturation, Value (HSB)",
        ColorFormat.Cmyk => "CMYK — Cyan, Magenta, Yellow, Key",
        _ => format.ToString()
    };
}
