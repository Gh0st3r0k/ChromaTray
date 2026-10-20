using System.Drawing;
using System.Windows.Forms;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private HotkeyBinding _tempPointHotkey;
    private HotkeyBinding _tempRegionHotkey;

    private readonly TextBox _txtPointHotkey;
    private readonly TextBox _txtRegionHotkey;
    private readonly Dictionary<ColorFormat, CheckBox> _formatCheckboxes = new();
    private readonly Label _lblValidationNotice;

    public SettingsForm(AppSettings currentSettings)
    {
        _settings = currentSettings;
        _tempPointHotkey = currentSettings.PointHotkey.Clone();
        _tempRegionHotkey = currentSettings.RegionHotkey.Clone();

        Text = "Settings — Hotkeys & Color Formats";
        ClientSize = new Size(500, 460);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.FromArgb(235, 235, 240);
        Font = new Font("Segoe UI", 9.5f);

        // --- Section 1: Global Hotkeys ---
        var lblHotkeysHeader = new Label
        {
            Text = "Global Hotkeys (Click box and press key combination)",
            Font = new Font("Segoe UI Semibold", 10.5f),
            Location = new Point(20, 18),
            AutoSize = true,
            ForeColor = Color.FromArgb(96, 165, 250)
        };

        var lblPoint = new Label
        {
            Text = "Point Picker:",
            Location = new Point(20, 56),
            Width = 130
        };

        _txtPointHotkey = CreateHotkeyCaptureBox(_tempPointHotkey.ToString(), new Point(155, 52));
        _txtPointHotkey.KeyDown += (s, e) => CaptureKeyInput(e, _txtPointHotkey, isPoint: true);

        var lblRegion = new Label
        {
            Text = "Region Extractor:",
            Location = new Point(20, 96),
            Width = 130
        };

        _txtRegionHotkey = CreateHotkeyCaptureBox(_tempRegionHotkey.ToString(), new Point(155, 92));
        _txtRegionHotkey.KeyDown += (s, e) => CaptureKeyInput(e, _txtRegionHotkey, isPoint: false);

        // --- Section 2: Visible Color Formats (Min 1) ---
        var lblFormatsHeader = new Label
        {
            Text = "Visible Color Standards (Select at least 1)",
            Font = new Font("Segoe UI Semibold", 10.5f),
            Location = new Point(20, 145),
            AutoSize = true,
            ForeColor = Color.FromArgb(52, 211, 153)
        };

        int yOffset = 180;
        foreach (ColorFormat fmt in Enum.GetValues<ColorFormat>())
        {
            var chk = new CheckBox
            {
                Text = GetFormatDescription(fmt),
                Checked = _settings.ActiveFormats.Contains(fmt),
                Location = new Point(24, yOffset),
                AutoSize = true,
                ForeColor = Color.FromArgb(225, 225, 235)
            };

            chk.CheckedChanged += (_, _) => OnFormatCheckboxChanged(chk);
            _formatCheckboxes[fmt] = chk;
            Controls.Add(chk);
            yOffset += 34;
        }

        _lblValidationNotice = new Label
        {
            Text = "",
            Location = new Point(20, yOffset + 4),
            Size = new Size(460, 42),
            ForeColor = Color.FromArgb(248, 113, 113),
            Font = new Font("Segoe UI", 9f)
        };

        // --- Footer Buttons ---
        var btnSave = new Button
        {
            Text = "Save Changes",
            Size = new Size(130, 36),
            Location = new Point(225, 406),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (_, _) => SaveAndClose();

        var btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(110, 36),
            Location = new Point(365, 406),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(63, 63, 75),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        CancelButton = btnCancel;

        Controls.AddRange(new Control[]
        {
            lblHotkeysHeader, lblPoint, _txtPointHotkey, lblRegion, _txtRegionHotkey,
            lblFormatsHeader, _lblValidationNotice, btnSave, btnCancel
        });
    }

    private static TextBox CreateHotkeyCaptureBox(string initialText, Point location)
    {
        return new TextBox
        {
            Text = initialText,
            Location = location,
            Width = 260,
            ReadOnly = true,
            BackColor = Color.FromArgb(36, 36, 44),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10.5f, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
            Cursor = Cursors.Hand
        };
    }

    private void CaptureKeyInput(KeyEventArgs e, TextBox targetBox, bool isPoint)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        if (SettingsValidator.IsModifierOnlyKey(e.KeyCode))
            return;

        var binding = new HotkeyBinding(e.Control, e.Shift, e.Alt, e.KeyCode);
        var validation = SettingsValidator.ValidateHotkeyBinding(binding);

        if (!validation.IsValid)
        {
            _lblValidationNotice.Text = validation.Message;
            return;
        }

        _lblValidationNotice.Text = "";
        targetBox.Text = binding.ToString();

        if (isPoint) _tempPointHotkey = binding;
        else _tempRegionHotkey = binding;
    }

    private void OnFormatCheckboxChanged(CheckBox changedBox)
    {
        var activeList = _formatCheckboxes.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToList();
        if (activeList.Count == 0)
        {
            changedBox.Checked = true;
            _lblValidationNotice.Text = "At least 1 color format must remain enabled.";
        }
        else
        {
            _lblValidationNotice.Text = "";
        }
    }

    private void SaveAndClose()
    {
        var activeFormats = _formatCheckboxes.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToList();
        var stateValidation = SettingsValidator.ValidateSettingsState(_tempPointHotkey, _tempRegionHotkey, activeFormats);

        if (!stateValidation.IsValid)
        {
            _lblValidationNotice.Text = stateValidation.Message;
            return;
        }

        // Persist before changing the live settings so a failed write leaves them intact.
        var candidate = new AppSettings
        {
            PointHotkey = _tempPointHotkey,
            RegionHotkey = _tempRegionHotkey,
            ActiveFormats = activeFormats,
            GroupSimilarColors = _settings.GroupSimilarColors
        };
        try { candidate.Save(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _lblValidationNotice.Text = $"Could not save settings: {ex.Message}";
            return;
        }
        _settings.PointHotkey = candidate.PointHotkey;
        _settings.RegionHotkey = candidate.RegionHotkey;
        _settings.ActiveFormats = candidate.ActiveFormats;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string GetFormatDescription(ColorFormat fmt) => fmt switch
    {
        ColorFormat.Hex => "HEX  — Hexadecimal (#3B82F6)",
        ColorFormat.Rgb => "RGB  — Red, Green, Blue (rgb(59, 130, 246))",
        ColorFormat.Hsl => "HSL  — Hue, Saturation, Lightness",
        ColorFormat.Hsv => "HSV  — Hue, Saturation, Value (HSB)",
        ColorFormat.Cmyk => "CMYK — Cyan, Magenta, Yellow, Key",
        _ => fmt.ToString()
    };
}
