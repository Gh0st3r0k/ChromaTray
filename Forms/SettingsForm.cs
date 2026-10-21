using System.Drawing;
using System.Windows.Forms;
using ChromaTray.Forms.Controls;
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
        Text = "ChromaTray · Preferences";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 740);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = StudioTheme.Canvas;
        ForeColor = StudioTheme.Text;
        Font = StudioTheme.Body;
        var heading = StudioTheme.Stack(
            (StudioTheme.Label("Make it yours.", StudioTheme.Title), 44),
            (StudioTheme.Label("Your shortcuts. Your color language.", StudioTheme.Body, StudioTheme.Muted), 28));

        _txtPointHotkey = CreateHotkeyCaptureBox(_tempPointHotkey.ToString(), "PointHotkey");
        _txtRegionHotkey = CreateHotkeyCaptureBox(_tempRegionHotkey.ToString(), "RegionHotkey");
        _txtPointHotkey.KeyDown += (_, e) => CaptureKeyInput(e, _txtPointHotkey, true);
        _txtRegionHotkey.KeyDown += (_, e) => CaptureKeyInput(e, _txtRegionHotkey, false);
        var hotkeys = new SurfacePanel { Name = "HotkeyCard", Margin = Padding.Empty };
        var hotkeyRows = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill, Margin = Padding.Empty };
        hotkeyRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        hotkeyRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        for (int row = 0; row < 2; row++) hotkeyRows.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        hotkeyRows.Controls.Add(StudioTheme.Label("Pick Point", StudioTheme.Strong), 0, 0);
        hotkeyRows.Controls.Add(_txtPointHotkey, 1, 0);
        hotkeyRows.Controls.Add(StudioTheme.Label("Scan Region", StudioTheme.Strong), 0, 1);
        hotkeyRows.Controls.Add(_txtRegionHotkey, 1, 1);
        hotkeys.Controls.Add(StudioTheme.Stack(
            (StudioTheme.Label("GLOBAL SHORTCUTS", StudioTheme.Strong, StudioTheme.Accent), 28),
            (StudioTheme.Label("Click a shortcut, then press your new combination.", StudioTheme.Small, StudioTheme.Muted), 24),
            (hotkeyRows, -1)));

        var formatsCard = new SurfacePanel { Name = "FormatsCard", Margin = Padding.Empty };
        var formats = new TableLayoutPanel { ColumnCount = 1, RowCount = 5, Dock = DockStyle.Fill, Margin = Padding.Empty };
        int index = 0;
        foreach (ColorFormat format in Enum.GetValues<ColorFormat>())
        {
            formats.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            var check = new CheckBox
            {
                Name = $"Enable{format}", Text = GetFormatDescription(format), Checked = currentSettings.ActiveFormats.Contains(format),
                Font = StudioTheme.Body, ForeColor = StudioTheme.Text, AutoSize = true,
                Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2), Cursor = Cursors.Hand
            };
            check.CheckedChanged += (_, _) => OnFormatCheckboxChanged(check);
            _formatCheckboxes[format] = check;
            formats.Controls.Add(check, 0, index++);
        }
        formatsCard.Controls.Add(StudioTheme.Stack(
            (StudioTheme.Label("COLOR FORMATS", StudioTheme.Strong, StudioTheme.Mint), 28),
            (StudioTheme.Label("Keep at least one. The first enabled format is used for quick copy.", StudioTheme.Small, StudioTheme.Muted), 30),
            (formats, -1)));
        var body = StudioTheme.Stack((hotkeys, 188), (new Panel { Margin = Padding.Empty }, 16), (formatsCard, -1));
        _lblValidationNotice = StudioTheme.Label("", StudioTheme.Small, StudioTheme.Danger);
        _lblValidationNotice.Name = "ValidationNotice";
        var save = new StudioButton { Name = "SaveSettingsButton", Text = "Save changes", Primary = true, Dock = DockStyle.Fill, Margin = new Padding(8, 4, 0, 4) };
        save.Click += (_, _) => SaveAndClose();
        var cancel = new StudioButton { Name = "CancelSettingsButton", Text = "Cancel", Dock = DockStyle.Fill, Margin = new Padding(8, 4, 0, 4) };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton = cancel;
        var footer = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        footer.Controls.Add(StudioTheme.Label("Stored on this device.", StudioTheme.Small, StudioTheme.Muted), 0, 0);
        footer.Controls.Add(cancel, 1, 0);
        footer.Controls.Add(save, 2, 0);
        var root = StudioTheme.Stack((heading, 88), (body, -1), (_lblValidationNotice, 48), (footer, 48));
        root.LogicalPadding = new Padding(24);
        Controls.Add(root);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StudioTheme.ApplyWindowChrome(this);
    }

    private static TextBox CreateHotkeyCaptureBox(string text, string name) => new()
    {
        Name = name, Text = text, ReadOnly = true, BackColor = StudioTheme.Raised,
        ForeColor = StudioTheme.Accent, BorderStyle = BorderStyle.FixedSingle, Font = StudioTheme.Mono,
        TextAlign = HorizontalAlignment.Center, Cursor = Cursors.Hand,
        Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 8), AccessibleName = name
    };

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
        ColorFormat.Hex => "HEX  — Hexadecimal (#98DED9)",
        ColorFormat.Rgb => "RGB  — Red, Green, Blue (rgb(152, 222, 217))",
        ColorFormat.Hsl => "HSL  — Hue, Saturation, Lightness",
        ColorFormat.Hsv => "HSV  — Hue, Saturation, Value (HSB)",
        ColorFormat.Cmyk => "CMYK — Cyan, Magenta, Yellow, Key",
        _ => fmt.ToString()
    };
}
