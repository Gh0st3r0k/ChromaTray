using ChromaTray.Forms.Controls;
using ChromaTray.Models;

namespace ChromaTray.Forms;

// Создание контролов отделено от проверки, редактирования и сохранения настроек.
public partial class SettingsForm
{
    private TextBox _pointHotkeyTextBox = null!;
    private TextBox _regionHotkeyTextBox = null!;
    private CheckBox _groupSimilarCheckBox = null!;
    private Label _validationLabel = null!;

    private void ConfigureWindow()
    {
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
    }

    private void InitializeLayout()
    {
        var heading = StudioTheme.Stack(
            (StudioTheme.Label("Make it yours.", StudioTheme.Title), 44),
            (StudioTheme.Label("Your shortcuts. Your color language.", StudioTheme.Body, StudioTheme.Muted), 28));

        var hotkeys = CreateHotkeyCard();
        var formatsCard = CreateFormatsCard();
        var body = StudioTheme.Stack((hotkeys, 188), (new Panel
        {
            Margin = Padding.Empty
        }, 16), (formatsCard, -1));
        _validationLabel = StudioTheme.Label("", StudioTheme.Small, StudioTheme.Danger);
        _validationLabel.Name = "ValidationNotice";
        var footer = CreateFooter();
        var root = StudioTheme.Stack((heading, 88), (body, -1), (_validationLabel, 48), (footer, 48));
        root.LogicalPadding = new Padding(24);
        Controls.Add(root);
    }

    private Control CreateHotkeyCard()
    {
        _pointHotkeyTextBox = CreateHotkeyCaptureBox(_editedPointHotkey.ToString(), "PointHotkey");
        _regionHotkeyTextBox = CreateHotkeyCaptureBox(_editedRegionHotkey.ToString(), "RegionHotkey");
        _pointHotkeyTextBox.KeyDown += (_, e) => CaptureKeyInput(e, _pointHotkeyTextBox, true);
        _regionHotkeyTextBox.KeyDown += (_, e) => CaptureKeyInput(e, _regionHotkeyTextBox, false);
        var hotkeys = new SurfacePanel
        {
            Name = "HotkeyCard",
            Margin = Padding.Empty
        };
        var hotkeyRows = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        hotkeyRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        hotkeyRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        for (int row = 0; row < 2; row++)
        {
            hotkeyRows.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        }
        hotkeyRows.Controls.Add(StudioTheme.Label("Pick Point", StudioTheme.Strong), 0, 0);
        hotkeyRows.Controls.Add(_pointHotkeyTextBox, 1, 0);
        hotkeyRows.Controls.Add(StudioTheme.Label("Scan Region", StudioTheme.Strong), 0, 1);
        hotkeyRows.Controls.Add(_regionHotkeyTextBox, 1, 1);
        hotkeys.Controls.Add(StudioTheme.Stack(
            (StudioTheme.Label("GLOBAL SHORTCUTS", StudioTheme.Strong, StudioTheme.Accent), 28),
            (StudioTheme.Label("Click a shortcut, then press your new combination.", StudioTheme.Small, StudioTheme.Muted), 24),
            (hotkeyRows, -1)));

        return hotkeys;
    }

    private Control CreateFormatsCard()
    {
        var formatsCard = new SurfacePanel
        {
            Name = "FormatsCard",
            Margin = Padding.Empty
        };
        var formats = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 5,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        int index = 0;
        foreach (ColorFormat format in Enum.GetValues<ColorFormat>())
        {
            formats.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            var check = new CheckBox
            {
                Name = $"Enable{format}",
                Text = GetFormatDescription(format),
                Checked = _settings.ActiveFormats.Contains(format),
                Font = StudioTheme.Body,
                ForeColor = StudioTheme.Text,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };
            check.CheckedChanged += (_, _) => OnFormatCheckboxChanged(check);
            _formatCheckboxes[format] = check;
            formats.Controls.Add(check, 0, index++);
        }
        _groupSimilarCheckBox = new CheckBox
        {
            Name = "SettingsGroupSimilarToggle",
            Text = "Group similar shades (region only)",
            Checked = _settings.GroupSimilarColors,
            ForeColor = StudioTheme.Accent,
            Font = StudioTheme.Small,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand,
            AccessibleDescription = "Region palette only. Leave off for exact screen RGB values. This does not remove ClearType from the screen."
        };
        formatsCard.Controls.Add(StudioTheme.Stack(
            (StudioTheme.Label("COLOR FORMATS", StudioTheme.Strong, StudioTheme.Mint), 28),
            (StudioTheme.Label("Keep at least one. The first enabled format is used for quick copy.", StudioTheme.Small, StudioTheme.Muted), 30),
            (formats, -1), (_groupSimilarCheckBox, 32)));
        return formatsCard;
    }

    private Control CreateFooter()
    {
        var save = new StudioButton
        {
            Name = "SaveSettingsButton",
            Text = "Save changes",
            Primary = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 4, 0, 4)
        };
        save.Click += (_, _) => SaveAndClose();
        var cancel = new StudioButton
        {
            Name = "CancelSettingsButton",
            Text = "Cancel",
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 4, 0, 4)
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        CancelButton = cancel;
        var footer = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        footer.Controls.Add(StudioTheme.Label("Stored on this device.", StudioTheme.Small, StudioTheme.Muted), 0, 0);
        footer.Controls.Add(cancel, 1, 0);
        footer.Controls.Add(save, 2, 0);
        return footer;
    }
}
