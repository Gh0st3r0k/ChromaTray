using ChromaTray.Forms.Controls;
using ChromaTray.Models;

namespace ChromaTray.Forms;
// Interface construction and layout adjustments for DPI and available space.
public partial class MainDashboardForm
{
    private bool _isInspectorViewActive;
    private bool _isUpdatingWorkspace;

    // InitializeLayout creates controls once. Early WinForms events check that they are ready.
    private StudioButton _pointButton = null!;
    private StudioButton _regionButton = null!;
    private CheckBox _groupSimilarCheckBox = null!;
    private PaletteGrid _paletteGrid = null!;
    private Panel _inspectorCard = null!;
    private TableLayoutPanel _formatRows = null!;
    private ColorPreview _colorPreview = null!;
    private Label _inspectorTitleLabel = null!;
    private StudioSelect _exportFormatSelect = null!;
    private Label _statusLabel = null!;
    private TableLayoutPanel _header = null!;
    private TableLayoutPanel _workspace = null!;
    private StudioButton _settingsButton = null!;
    private DpiStackPanel _inspectorLayout = null!;
    private DpiStackPanel _dashboardLayout = null!;
    private DpiStackPanel _wordmark = null!;
    private DpiStackPanel _paletteLayout = null!;
    private DpiStackPanel _paletteHeader = null!;
    private SurfacePanel _paletteCard = null!;
    private TableLayoutPanel _workspaceTabs = null!;
    private StudioButton _paletteTab = null!;
    private StudioButton _inspectorTab = null!;
    private Image? _logoImage;
    private Label _paletteCountLabel = null!;
    private PaletteStrip _paletteStrip = null!;

    private void ConfigureWindow()
    {
        Text = "ChromaTray · Color studio";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 840);
        MinimumSize = new Size(1060, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = StudioTheme.Canvas;
        ForeColor = StudioTheme.Text;
        Font = StudioTheme.Body;
    }

    // Order matters: the workspace uses cards and header controls created earlier.
    private void InitializeLayout()
    {
        BuildHeader();
        var captureActions = CreateCaptureActions();
        BuildPaletteCard();
        BuildInspectorCard();
        BuildWorkspace();
        var footer = CreateFooter();
        _dashboardLayout = StudioTheme.Stack((_header, 80), (captureActions, 100), (new Panel
        {
            Margin = Padding.Empty
        }, 20), (_workspace, -1), (footer, 40));
        _dashboardLayout.Name = "DashboardLayout";
        _dashboardLayout.LogicalPadding = new Padding(24, 16, 24, 8);
        Controls.Add(_dashboardLayout);
        _toolTips.SetToolTip(_pointButton, "Pick a pixel. Use arrow keys to move one pixel and Enter to select.");
        _toolTips.SetToolTip(_groupSimilarCheckBox,
            "Combines nearby shades using the most frequent original RGB in each group. " +
            "Leave off to list every exact shade. Point picking always uses the original pixel.");
    }

    private void BuildHeader()
    {
        _header = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };
        _header.Name = "MainHeader";
        _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
        var logo = new PictureBox
        {
            Image = _appIcon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0)
        };
        _logoImage = logo.Image;
        _header.Controls.Add(logo, 0, 0);
        _wordmark = StudioTheme.Stack(
            (StudioTheme.Label("ChromaTray", StudioTheme.Title), 42),
            (StudioTheme.Label("A little color. A lot of possibility.", StudioTheme.Small, StudioTheme.Muted), 24));
        _paletteTab = new StudioButton
        {
            Name = "PaletteViewButton",
            Text = "Palette",
            Primary = true,
            Dock = DockStyle.Fill
        };
        _inspectorTab = new StudioButton
        {
            Name = "InspectorViewButton",
            Text = "Inspector",
            Dock = DockStyle.Fill
        };
        _paletteTab.Click += (_, _) =>
        {
            _isInspectorViewActive = false;
            FitWorkspaceColumns();
        };
        _inspectorTab.Click += (_, _) =>
        {
            _isInspectorViewActive = true;
            FitWorkspaceColumns();
        };
        _workspaceTabs = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Visible = false,
            Margin = Padding.Empty
        };
        _workspaceTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _workspaceTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _workspaceTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _workspaceTabs.Controls.Add(_paletteTab, 0, 0);
        _workspaceTabs.Controls.Add(_inspectorTab, 1, 0);
        var brandingSlot = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        brandingSlot.Controls.Add(_wordmark);
        brandingSlot.Controls.Add(_workspaceTabs);
        _header.Controls.Add(brandingSlot, 1, 0);
        _settingsButton = new StudioButton
        {
            Name = "SettingsButton",
            Text = "Settings",
            Glyph = StudioGlyph.Settings,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 14, 0, 14)
        };
        _settingsButton.Click += (_, _) => OpenSettingsDialog();
        _header.Controls.Add(_settingsButton, 2, 0);
    }

    private Control CreateCaptureActions()
    {
        _pointButton = new StudioButton
        {
            Name = "PickPointButton",
            Text = "Pick Point",
            Primary = true,
            Glyph = StudioGlyph.Point,
            Description = "Find the perfect pixel. Arrow keys for precision.",
            Margin = new Padding(0, 0, 8, 0)
        };
        _pointButton.Click += (_, _) => StartCapture(CaptureMode.Point);
        _regionButton = new StudioButton
        {
            Name = "ScanRegionButton",
            Text = "Scan Region",
            Glyph = StudioGlyph.Region,
            AccentColor = StudioTheme.Mint,
            Description = "Turn any part of your screen into a palette.",
            Margin = new Padding(8, 0, 0, 0)
        };
        _regionButton.Click += (_, _) => StartCapture(CaptureMode.Region);
        var captureActions = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0),
            BackColor = Color.Transparent
        };
        captureActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        captureActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _pointButton.Dock = _regionButton.Dock = DockStyle.Fill;
        captureActions.Name = "CaptureActions";
        captureActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        captureActions.Controls.Add(_pointButton, 0, 0);
        captureActions.Controls.Add(_regionButton, 1, 0);
        return captureActions;
    }

    private void BuildPaletteCard()
    {
        _paletteCard = new SurfacePanel
        {
            Name = "PaletteCard",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 16, 0)
        };
        _paletteCountLabel = StudioTheme.Label("YOUR PALETTE", StudioTheme.Strong);
        _groupSimilarCheckBox = new CheckBox
        {
            Name = "GroupSimilarToggle",
            Text = "Group similar shades",
            Checked = _settings.GroupSimilarColors,
            ForeColor = StudioTheme.Accent,
            Font = StudioTheme.Small,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty,
            AccessibleDescription = "Merge similar RGB shades and recalculate the last region."
        };
        _groupSimilarCheckBox.CheckedChanged += OnGroupingChanged;
        // A separate row keeps the toggle visible with a long heading or high DPI.
        _paletteHeader = StudioTheme.Stack((_paletteCountLabel, 24), (_groupSimilarCheckBox, 28));
        _paletteStrip = new PaletteStrip
        {
            Name = "PaletteOverview",
            Margin = new Padding(0, 10, 0, 10)
        };
        _paletteGrid = new PaletteGrid
        {
            Name = "PaletteTable",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        _paletteGrid.SelectedIndexChanged += OnPaletteSelectionChanged;
        _paletteGrid.ColorActivated += (_, _) => CopyPrimaryColor();
        _paletteGrid.KeyDown += OnPaletteKeyDown;
        _exportFormatSelect = new StudioSelect
        {
            Name = "ExportMode",
            Dock = DockStyle.Fill,
            Font = StudioTheme.Small,
            Margin = new Padding(0, 6, 12, 0),
            AccessibleName = "Palette export format"
        };
        _exportFormatSelect.SetItems("All active formats", "Primary format only", "CSS custom properties", "JSON palette array");
        var exportButton = new StudioButton
        {
            Name = "ExportPaletteButton",
            Text = "Copy palette",
            Glyph = StudioGlyph.Copy,
            AccentColor = StudioTheme.Mint,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 0)
        };
        exportButton.Click += (_, _) => CopyEntirePalette();
        var exportRow = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        exportRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        exportRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        exportRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154));
        exportRow.Controls.Add(_exportFormatSelect, 0, 0);
        exportRow.Controls.Add(exportButton, 1, 0);
        _paletteLayout = StudioTheme.Stack(
(_paletteHeader, 56),
(_paletteStrip, 32),
(_paletteGrid, -1),
(StudioTheme.Label("Select a shade to explore its formats. Double-click to copy.", StudioTheme.Small, StudioTheme.Muted), 32),
(exportRow, 52)
);
        _paletteCard.Controls.Add(_paletteLayout);
    }

    private void BuildInspectorCard()
    {
        _inspectorCard = new SurfacePanel
        {
            Name = "InspectorCard",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        _inspectorTitleLabel = StudioTheme.Label("COLOR INSPECTOR", StudioTheme.Strong);
        _colorPreview = new ColorPreview
        {
            Name = "SelectedColorPreview",
            Margin = new Padding(0, 8, 0, 10)
        };
        _formatRows = new TableLayoutPanel
        {
            Name = "ColorFormats",
            ColumnCount = 1,
            Margin = Padding.Empty,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        _formatRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var copyFormats = new StudioButton
        {
            Name = "CopyFormatsButton",
            Text = "Copy active formats",
            Primary = true,
            Glyph = StudioGlyph.Copy,
            Margin = new Padding(0, 8, 0, 0)
        };
        copyFormats.Click += (_, _) => CopySingleColorSummary();
        var formatsViewport = new StudioScrollViewport
        {
            Name = "FormatsViewport",
            Content = _formatRows
        };
        _inspectorLayout = StudioTheme.Stack(
(_inspectorTitleLabel, 30),
(_colorPreview, 128),
(formatsViewport, -1),
(copyFormats, 58),
(StudioTheme.Label("All enabled formats. One click to copy.", StudioTheme.Small, StudioTheme.Muted), 28)
);
        _inspectorLayout.Name = "InspectorLayout";
        _inspectorLayout.SizeChanged += (_, _) => FitInspectorContent();
        _inspectorCard.Controls.Add(_inspectorLayout);
    }

    private void BuildWorkspace()
    {
        _workspace = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        _workspace.Name = "PaletteWorkspace";
        _workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 408));
        _workspace.Controls.Add(_paletteCard, 0, 0);
        _workspace.Controls.Add(_inspectorCard, 1, 0);
        _workspace.SizeChanged += (_, _) =>
        {
            FitWorkspaceColumns();
            FitInspectorContent();
        };
    }

    private Control CreateFooter()
    {
        _statusLabel = StudioTheme.Label("Ready to capture.", StudioTheme.Small, StudioTheme.Muted);
        _statusLabel.Name = "StatusText";
        _statusLabel.TextChanged += (_, _) => _toolTips.SetToolTip(_statusLabel, _statusLabel.Text);
        var trayHint = StudioTheme.Label("Closing the window keeps ChromaTray in your tray.", StudioTheme.Small, StudioTheme.Muted);
        trayHint.TextAlign = ContentAlignment.MiddleRight;
        var footer = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        footer.Controls.Add(_statusLabel, 0, 0);
        footer.Controls.Add(trayHint, 1, 0);
        return footer;
    }

    private void ApplyDpiMetrics()
    {
        int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        _header.ColumnStyles[0].Width = Scale(54);
        _header.ColumnStyles[2].Width = Scale(152);
        _settingsButton.Margin = new Padding(0, Scale(14), 0, Scale(14));
        RebuildDynamicFormatLayout();
        ApplyResponsiveLayout();
        PerformLayout();
        FitInspectorContent();
    }

    // All dimensions below are logical units at 96 DPI; DpiStackPanel scales them.
    private void ApplyResponsiveLayout()
    {
        bool compact = ClientSize.Height * 96.0 / DeviceDpi < 760;
        _dashboardLayout.SetRowHeight(0, compact ? 56 : 80);
        _dashboardLayout.SetRowHeight(1, compact ? 64 : 100);
        _dashboardLayout.SetRowHeight(2, compact ? 8 : 20);
        _dashboardLayout.SetRowHeight(4, compact ? 24 : 40);
        _dashboardLayout.LogicalPadding = new Padding(24, compact ? 12 : 16, 24, compact ? 4 : 8);
        _wordmark.SetRowHeight(1, compact ? 0 : 24);
        var subtitle = _wordmark.GetControlFromPosition(0, 1);
        if (subtitle is not null)
        {
            subtitle.Visible = !compact;
        }

        _pointButton.Description = compact ? "" : "Find the perfect pixel. Arrow keys for precision.";
        _regionButton.Description = compact ? "" : "Turn any part of your screen into a palette.";
        _pointButton.Invalidate();
        _regionButton.Invalidate();
        _paletteLayout.SetRowHeight(0, compact ? 44 : 56);
        _paletteHeader.SetRowHeight(0, compact ? 20 : 24);
        _paletteHeader.SetRowHeight(1, compact ? 24 : 28);
        _paletteLayout.SetRowHeight(1, compact ? 0 : 32);
        _paletteLayout.SetRowHeight(3, compact ? 0 : 32);
        _paletteLayout.SetRowHeight(4, compact ? 48 : 52);
        int verticalMargin = (int)Math.Round((compact ? 6 : 14) * DeviceDpi / 96f);
        _settingsButton.Margin = new Padding(0, verticalMargin, 0, verticalMargin);
        FitWorkspaceColumns();
    }

    private void FitWorkspaceColumns()
    {
        if (_workspace is null || _isUpdatingWorkspace)
        {
            return;
        }

        _isUpdatingWorkspace = true;
        _workspace.SuspendLayout();
        try
        {
            int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
            // Use tabs on narrow windows so each card has enough room.
            bool narrow = _workspace.ClientSize.Width < Scale(800);
            _wordmark.Visible = !narrow;
            _workspaceTabs.Visible = narrow;
            _paletteTab.Margin = new Padding(0, Scale(6), Scale(4), Scale(6));
            _inspectorTab.Margin = new Padding(Scale(4), Scale(6), 0, Scale(6));
            _paletteTab.Primary = !_isInspectorViewActive;
            _inspectorTab.Primary = _isInspectorViewActive;
            _paletteTab.Invalidate();
            _inspectorTab.Invalidate();
            _paletteCard.Visible = !narrow || !_isInspectorViewActive;
            _inspectorCard.Visible = !narrow || _isInspectorViewActive;
            _paletteCard.Margin = new Padding(0, 0, narrow ? 0 : Scale(16), 0);
            _workspace.SetCellPosition(_inspectorCard, new TableLayoutPanelCellPosition(narrow ? 0 : 1, 0));
            _workspace.ColumnStyles[1].Width = narrow ? 0 : Math.Min(Scale(408), _workspace.ClientSize.Width * .43f);
        }
        finally
        {
            _workspace.ResumeLayout(true);
            _isUpdatingWorkspace = false;
        }

        FitInspectorContent();
    }

    private void FitInspectorContent()
    {
        if (_inspectorLayout is null)
        {
            return;
        }

        // Nested panels finish scaling after the workspace.
        // Use the actual content height when calculating available space.
        int available = (int)Math.Floor(Math.Max(0, _inspectorLayout.ClientSize.Height) * 96.0 / DeviceDpi);
        int copyHeight = available < 136 ? 44 : 58;
        int titleHeight = available < 30 + copyHeight + 48 ? 0 : 30;
        int hintHeight = available >= titleHeight + copyHeight + 48 + 56 + 28 ? 28 : 0;
        int previewHeight = Math.Min(128, Math.Max(0, available - titleHeight - copyHeight - hintHeight - 48));
        if (previewHeight < 56)
        {
            previewHeight = 0;
        }

        _inspectorLayout.SetRowHeight(0, titleHeight);
        _inspectorLayout.SetRowHeight(1, previewHeight);
        _inspectorLayout.SetRowHeight(3, copyHeight);
        _inspectorLayout.SetRowHeight(4, hintHeight);
    }

    private void RebuildDynamicFormatLayout()
    {
        int selectedIndex = _paletteGrid.SelectedIndex;
        int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        _formatRows.SuspendLayout();
        foreach (Control control in _formatRows.Controls.Cast<Control>().ToArray())
        {
            control.Dispose();
        }

        _formatRows.RowStyles.Clear();
        _formatRows.RowCount = _settings.ActiveFormats.Count;
        _formatTextBoxes.Clear();
        int row = 0;
        foreach (var format in _settings.ActiveFormats)
        {
            _formatRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(48)));
            var line = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            line.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(48)));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(80)));
            var label = StudioTheme.Label(ColorDetails.GetFormatLabel(format), StudioTheme.Small, StudioTheme.Muted);
            var box = new TextBox
            {
                Name = $"Value{format}",
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = StudioTheme.Surface,
                ForeColor = StudioTheme.Text,
                Font = StudioTheme.Mono,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, Scale(14), Scale(5), 0),
                AccessibleName = $"{format} color value"
            };
            var copy = new StudioButton
            {
                Text = "Copy",
                Name = $"Copy{format}",
                Font = StudioTheme.Small,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, Scale(5), 0, Scale(5)),
                AccessibleName = $"Copy {format}"
            };
            copy.Click += (_, _) => CopyTextToClipboard(box.Text, $"Copied {ColorDetails.GetFormatLabel(format)}");
            line.Controls.Add(label, 0, 0);
            line.Controls.Add(box, 1, 0);
            line.Controls.Add(copy, 2, 0);
            _formatTextBoxes[format] = box;
            _formatRows.Controls.Add(line, 0, row++);
        }

        _formatRows.ResumeLayout(true);
        if (_currentPalette.Count > 0)
        {
            UpdatePaletteList(_currentPalette, _statusLabel.Text);
            _paletteGrid.SelectedIndex = Math.Clamp(selectedIndex, 0, _currentPalette.Count - 1);
        }
    }
}
