using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

public class MainDashboardForm : Form
{
    private readonly AppSettings _settings;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _trayPointItem;
    private readonly ToolStripMenuItem _trayRegionItem;

    private readonly StudioButton _btnPoint;
    private readonly StudioButton _btnRegion;
    private readonly CheckBox _chkGroupSimilar;

    private readonly PaletteGrid _colorListView;
    private readonly Panel _rightCard;
    private readonly TableLayoutPanel _formatsContainer;
    private readonly ColorPreview _previewSwatch;
    private readonly Label _lblSelectedTitle;
    private readonly StudioSelect _cmbExportFormat;
    private readonly Label _lblStatus;

    private readonly Dictionary<ColorFormat, TextBox> _formatBoxes = new();
    private List<ColorDetails> _currentPalette = new();
    private Bitmap? _lastRegionBitmap;
    private bool _allowExit = false;
    private bool _isCapturing;
    private bool _settingsDialogOpen;
    private int _analysisVersion;
    private CancellationTokenSource? _analysisCancellation;
    private readonly Icon _appIcon;
    private readonly ToolTip _tips = new();
    private readonly TableLayoutPanel _header;
    private readonly TableLayoutPanel _workspace;
    private readonly StudioButton _settingsButton;
    private readonly DpiStackPanel _inspectorLayout;
    private readonly DpiStackPanel _dashboardLayout;
    private readonly DpiStackPanel _wordmark;
    private readonly DpiStackPanel _paletteLayout;
    private readonly SurfacePanel _paletteCard;
    private readonly TableLayoutPanel _workspaceTabs;
    private readonly StudioButton _paletteTab;
    private readonly StudioButton _inspectorTab;
    private bool _inspectorView;
    private bool _changingWorkspace;
    private readonly Image? _logoImage;
    private readonly Label _lblPaletteCount;
    private readonly PaletteStrip _paletteStrip;

    public MainDashboardForm() : this(AppSettings.Load()) { }

    internal MainDashboardForm(AppSettings initialSettings)
    {
        _settings = initialSettings;
        Text = "ChromaTray · Color studio";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 840);
        MinimumSize = new Size(1060, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = StudioTheme.Canvas;
        ForeColor = StudioTheme.Text;
        Font = StudioTheme.Body;
        _appIcon = CreateDynamicAppIcon();
        Icon = _appIcon;

        var trayMenu = new ContextMenuStrip { BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text, Renderer = new StudioMenuRenderer() };
        _trayPointItem = new ToolStripMenuItem("", null, (_, _) => StartCapture(CaptureMode.Point));
        _trayRegionItem = new ToolStripMenuItem("", null, (_, _) => StartCapture(CaptureMode.Region));
        trayMenu.Items.AddRange([_trayPointItem, _trayRegionItem, new ToolStripSeparator()]);
        trayMenu.Items.Add("Settings…", null, (_, _) => OpenSettingsDialog());
        trayMenu.Items.Add("Open dashboard", null, (_, _) => ShowMainWindow());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon = new NotifyIcon { Icon = Icon, ContextMenuStrip = trayMenu, Visible = true };
        _trayIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowMainWindow(); };

        _header = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, BackColor = Color.Transparent };
        _header.Name = "MainHeader";
        _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
        var logo = new PictureBox { Image = _appIcon.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        _logoImage = logo.Image;
        _header.Controls.Add(logo, 0, 0);
        _wordmark = StudioTheme.Stack(
            (StudioTheme.Label("ChromaTray", StudioTheme.Title), 42),
            (StudioTheme.Label("A little color. A lot of possibility.", StudioTheme.Small, StudioTheme.Muted), 24));
        _paletteTab = new StudioButton { Name = "PaletteViewButton", Text = "Palette", Primary = true, Dock = DockStyle.Fill };
        _inspectorTab = new StudioButton { Name = "InspectorViewButton", Text = "Inspector", Dock = DockStyle.Fill };
        _paletteTab.Click += (_, _) => { _inspectorView = false; FitWorkspaceColumns(); };
        _inspectorTab.Click += (_, _) => { _inspectorView = true; FitWorkspaceColumns(); };
        _workspaceTabs = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill, Visible = false, Margin = Padding.Empty };
        _workspaceTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _workspaceTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _workspaceTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _workspaceTabs.Controls.Add(_paletteTab, 0, 0);
        _workspaceTabs.Controls.Add(_inspectorTab, 1, 0);
        var brandingSlot = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        brandingSlot.Controls.Add(_wordmark);
        brandingSlot.Controls.Add(_workspaceTabs);
        _header.Controls.Add(brandingSlot, 1, 0);
        _settingsButton = new StudioButton { Name = "SettingsButton", Text = "Settings", Glyph = StudioGlyph.Settings, Dock = DockStyle.Fill, Margin = new Padding(0, 14, 0, 14) };
        _settingsButton.Click += (_, _) => OpenSettingsDialog();
        _header.Controls.Add(_settingsButton, 2, 0);

        _btnPoint = new StudioButton
        {
            Name = "PickPointButton", Text = "Pick Point", Primary = true, Glyph = StudioGlyph.Point,
            Description = "Find the perfect pixel. Arrow keys for precision.", Margin = new Padding(0, 0, 8, 0)
        };
        _btnPoint.Click += (_, _) => StartCapture(CaptureMode.Point);
        _btnRegion = new StudioButton
        {
            Name = "ScanRegionButton", Text = "Scan Region", Glyph = StudioGlyph.Region, AccentColor = StudioTheme.Mint,
            Description = "Turn any part of your screen into a palette.", Margin = new Padding(8, 0, 0, 0)
        };
        _btnRegion.Click += (_, _) => StartCapture(CaptureMode.Region);
        var captureActions = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 12, 0, 0), BackColor = Color.Transparent };
        captureActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        captureActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _btnPoint.Dock = _btnRegion.Dock = DockStyle.Fill;
        captureActions.Name = "CaptureActions";
        captureActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        captureActions.Controls.Add(_btnPoint, 0, 0);
        captureActions.Controls.Add(_btnRegion, 1, 0);

        _paletteCard = new SurfacePanel { Name = "PaletteCard", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0) };
        _lblPaletteCount = StudioTheme.Label("YOUR PALETTE", StudioTheme.Strong);
        _chkGroupSimilar = new CheckBox
        {
            Name = "GroupSimilarToggle", Text = "Group similar shades", Checked = _settings.GroupSimilarColors,
            ForeColor = StudioTheme.Accent, Font = StudioTheme.Small, AutoSize = false, Dock = DockStyle.Fill,
            Cursor = Cursors.Hand, Margin = Padding.Empty, AccessibleDescription = "Merge similar RGB shades and recalculate the last region."
        };
        _chkGroupSimilar.CheckedChanged += (_, _) =>
        {
            if (_settings.GroupSimilarColors != _chkGroupSimilar.Checked)
            {
                _settings.GroupSimilarColors = _chkGroupSimilar.Checked;
                try { _settings.Save(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show(this, $"Could not save settings: {ex.Message}", "ChromaTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            if (_lastRegionBitmap != null) RunRegionAnalysis(_lastRegionBitmap);
        };
        // Give this option its own full-width row. An AutoSize column beside a
        // long palette title could place it outside the visible card.
        var paletteHeader = StudioTheme.Stack((_lblPaletteCount, 24), (_chkGroupSimilar, 28));
        _paletteStrip = new PaletteStrip { Name = "PaletteOverview", Margin = new Padding(0, 10, 0, 10) };

        _colorListView = new PaletteGrid { Name = "PaletteTable", Dock = DockStyle.Fill, Margin = Padding.Empty };
        _colorListView.SelectedIndexChanged += (_, _) =>
        {
            if (_colorListView.SelectedColor is { } color) DisplaySelectedColor(color);
        };
        _colorListView.ColorActivated += (_, _) => CopyPrimaryColor();
        _colorListView.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C) { CopyPrimaryColor(); e.SuppressKeyPress = true; }
        };

        _cmbExportFormat = new StudioSelect
        {
            Name = "ExportMode", Dock = DockStyle.Fill, Font = StudioTheme.Small,
            Margin = new Padding(0, 6, 12, 0), AccessibleName = "Palette export format"
        };
        _cmbExportFormat.SetItems("All active formats", "Primary format only", "CSS custom properties", "JSON palette array");
        var exportButton = new StudioButton { Name = "ExportPaletteButton", Text = "Copy palette", Glyph = StudioGlyph.Copy, AccentColor = StudioTheme.Mint, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
        exportButton.Click += (_, _) => CopyEntirePalette();
        var exportRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        exportRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        exportRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        exportRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154));
        exportRow.Controls.Add(_cmbExportFormat, 0, 0);
        exportRow.Controls.Add(exportButton, 1, 0);
        _paletteLayout = StudioTheme.Stack(
            (paletteHeader, 56), (_paletteStrip, 32), (_colorListView, -1),
            (StudioTheme.Label("Select a shade to explore its formats. Double-click to copy.", StudioTheme.Small, StudioTheme.Muted), 32),
            (exportRow, 52));
        _paletteCard.Controls.Add(_paletteLayout);

        _rightCard = new SurfacePanel { Name = "InspectorCard", Dock = DockStyle.Fill, Margin = Padding.Empty };
        _lblSelectedTitle = StudioTheme.Label("COLOR INSPECTOR", StudioTheme.Strong);
        _previewSwatch = new ColorPreview { Name = "SelectedColorPreview", Margin = new Padding(0, 8, 0, 10) };
        _formatsContainer = new TableLayoutPanel { Name = "ColorFormats", ColumnCount = 1, Margin = Padding.Empty, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _formatsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var copyFormats = new StudioButton { Name = "CopyFormatsButton", Text = "Copy active formats", Primary = true, Glyph = StudioGlyph.Copy, Margin = new Padding(0, 8, 0, 0) };
        copyFormats.Click += (_, _) => CopySingleColorSummary();
        var formatsViewport = new StudioScrollViewport { Name = "FormatsViewport", Content = _formatsContainer };
        _inspectorLayout = StudioTheme.Stack(
            (_lblSelectedTitle, 30), (_previewSwatch, 128), (formatsViewport, -1), (copyFormats, 58),
            (StudioTheme.Label("All enabled formats. One click to copy.", StudioTheme.Small, StudioTheme.Muted), 28));
        _inspectorLayout.Name = "InspectorLayout";
        _inspectorLayout.SizeChanged += (_, _) => FitInspectorContent();
        _rightCard.Controls.Add(_inspectorLayout);
        _workspace = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
        _workspace.Name = "PaletteWorkspace";
        _workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 408));
        _workspace.Controls.Add(_paletteCard, 0, 0);
        _workspace.Controls.Add(_rightCard, 1, 0);
        _workspace.SizeChanged += (_, _) => { FitWorkspaceColumns(); FitInspectorContent(); };

        _lblStatus = StudioTheme.Label("Ready to capture.", StudioTheme.Small, StudioTheme.Muted);
        _lblStatus.Name = "StatusText";
        _lblStatus.TextChanged += (_, _) => _tips.SetToolTip(_lblStatus, _lblStatus.Text);
        var trayHint = StudioTheme.Label("Closing the window keeps ChromaTray in your tray.", StudioTheme.Small, StudioTheme.Muted);
        trayHint.TextAlign = ContentAlignment.MiddleRight;
        var footer = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        footer.Controls.Add(_lblStatus, 0, 0);
        footer.Controls.Add(trayHint, 1, 0);
        _dashboardLayout = StudioTheme.Stack((_header, 80), (captureActions, 100), (new Panel { Margin = Padding.Empty }, 20), (_workspace, -1), (footer, 40));
        _dashboardLayout.Name = "DashboardLayout";
        _dashboardLayout.LogicalPadding = new Padding(24, 16, 24, 8);
        Controls.Add(_dashboardLayout);
        _tips.SetToolTip(_btnPoint, "Pick a pixel. Use arrow keys to move one pixel and Enter to select.");
        _tips.SetToolTip(_chkGroupSimilar, "Combines nearby shades using the most frequent original RGB in each group. Leave off to list every exact shade. Point picking always uses the original pixel.");
        UpdateHotkeyLabels();
        RebuildDynamicFormatLayout();
        var initial = ColorDetails.FromColor(StudioTheme.Accent);
        UpdatePaletteList([initial], "Ready. Pick a point or scan a region to start.");
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Handle creation can happen while controls are still being initialized.
        StudioTheme.ApplyWindowChrome(this);
        if (_lblStatus is not null) RegisterConfiguredHotkeys();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        var area = Screen.FromControl(this).WorkingArea;
        MinimumSize = new Size(Math.Min(S(980), area.Width), Math.Min(S(660), area.Height));
        if (Width > area.Width || Height > area.Height)
            Bounds = new Rectangle(area.Left, area.Top, Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        ApplyDpiMetrics();
        RegisterConfiguredHotkeys();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (_formatsContainer is not null) ApplyDpiMetrics();
    }

    private void ApplyDpiMetrics()
    {
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        _header.ColumnStyles[0].Width = S(54);
        _header.ColumnStyles[2].Width = S(152);
        _settingsButton.Margin = new Padding(0, S(14), 0, S(14));
        RebuildDynamicFormatLayout();
        ApplyResponsiveLayout();
        PerformLayout();
        FitInspectorContent();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_dashboardLayout is not null) ApplyResponsiveLayout();
    }

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
        if (subtitle is not null) subtitle.Visible = !compact;
        _btnPoint.Description = compact ? "" : "Find the perfect pixel. Arrow keys for precision.";
        _btnRegion.Description = compact ? "" : "Turn any part of your screen into a palette.";
        _btnPoint.Invalidate();
        _btnRegion.Invalidate();
        _paletteLayout.SetRowHeight(0, 56);
        _paletteLayout.SetRowHeight(1, compact ? 0 : 32);
        _paletteLayout.SetRowHeight(3, compact ? 0 : 32);
        _paletteLayout.SetRowHeight(4, compact ? 48 : 52);
        int verticalMargin = (int)Math.Round((compact ? 6 : 14) * DeviceDpi / 96f);
        _settingsButton.Margin = new Padding(0, verticalMargin, 0, verticalMargin);
        FitWorkspaceColumns();
    }

    private void FitWorkspaceColumns()
    {
        if (_workspace is null || _changingWorkspace) return;
        _changingWorkspace = true;
        _workspace.SuspendLayout();
        try
        {
            int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
            bool narrow = _workspace.ClientSize.Width < S(800);
            _wordmark.Visible = !narrow;
            _workspaceTabs.Visible = narrow;
            _paletteTab.Margin = new Padding(0, S(6), S(4), S(6));
            _inspectorTab.Margin = new Padding(S(4), S(6), 0, S(6));
            _paletteTab.Primary = !_inspectorView;
            _inspectorTab.Primary = _inspectorView;
            _paletteTab.Invalidate();
            _inspectorTab.Invalidate();
            _paletteCard.Visible = !narrow || !_inspectorView;
            _rightCard.Visible = !narrow || _inspectorView;
            _paletteCard.Margin = new Padding(0, 0, narrow ? 0 : S(16), 0);
            _workspace.SetCellPosition(_rightCard, new TableLayoutPanelCellPosition(narrow ? 0 : 1, 0));
            _workspace.ColumnStyles[1].Width = narrow ? 0 : Math.Min(S(408), _workspace.ClientSize.Width * .43f);
        }
        finally
        {
            _workspace.ResumeLayout(true);
            _changingWorkspace = false;
        }
        FitInspectorContent();
    }

    private void FitInspectorContent()
    {
        if (_inspectorLayout is null) return;
        // The padded card can finish scaling after the workspace's SizeChanged.
        // Use the actual content bounds and react when those bounds settle.
        int available = (int)Math.Floor(Math.Max(0, _inspectorLayout.ClientSize.Height) * 96.0 / DeviceDpi);
        int copyHeight = available < 136 ? 44 : 58;
        int titleHeight = available < 30 + copyHeight + 48 ? 0 : 30;
        int hintHeight = available >= titleHeight + copyHeight + 48 + 56 + 28 ? 28 : 0;
        int previewHeight = Math.Min(128, Math.Max(0, available - titleHeight - copyHeight - hintHeight - 48));
        if (previewHeight < 56) previewHeight = 0;
        _inspectorLayout.SetRowHeight(0, titleHeight);
        _inspectorLayout.SetRowHeight(1, previewHeight);
        _inspectorLayout.SetRowHeight(3, copyHeight);
        _inspectorLayout.SetRowHeight(4, hintHeight);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        NativeHotkeyManager.UnregisterAll(Handle);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeHotkeyManager.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (id == NativeHotkeyManager.HOTKEY_ID_POINT) StartCapture(CaptureMode.Point);
            else if (id == NativeHotkeyManager.HOTKEY_ID_REGION) StartCapture(CaptureMode.Region);
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(
                1500,
                "ChromaTray is running in the background",
                $"Point: {_settings.PointHotkey} | Region: {_settings.RegionHotkey}",
                ToolTipIcon.Info);
            return;
        }
        _trayIcon.Visible = false;
        base.OnFormClosing(e);
    }

    private void RegisterConfiguredHotkeys()
    {
        if (!IsHandleCreated) return;
        bool okPoint = NativeHotkeyManager.Register(Handle, NativeHotkeyManager.HOTKEY_ID_POINT, _settings.PointHotkey);
        bool okRegion = NativeHotkeyManager.Register(Handle, NativeHotkeyManager.HOTKEY_ID_REGION, _settings.RegionHotkey);

        if (!okPoint || !okRegion)
        {
            _lblStatus.Text = $"Hotkey unavailable: {(!okPoint ? "Point " : "")}{(!okRegion ? "Region" : "")}. Choose another shortcut in Settings; capture buttons still work.";
        }
    }

    private void UpdateHotkeyLabels()
    {
        _btnPoint.Shortcut = _settings.PointHotkey.ToString();
        _btnPoint.Invalidate();
        _btnRegion.Shortcut = _settings.RegionHotkey.ToString();
        _btnRegion.Invalidate();
        _trayPointItem.Text = $"Pick Point ({_settings.PointHotkey})";
        _trayRegionItem.Text = $"Scan Region ({_settings.RegionHotkey})";
        string tooltip = $"ChromaTray ({_settings.PointHotkey} | {_settings.RegionHotkey})";
        _trayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    private void RebuildDynamicFormatLayout()
    {
        int selectedIndex = _colorListView.SelectedIndex;
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        _formatsContainer.SuspendLayout();
        foreach (Control control in _formatsContainer.Controls.Cast<Control>().ToArray()) control.Dispose();
        _formatsContainer.RowStyles.Clear();
        _formatsContainer.RowCount = _settings.ActiveFormats.Count;
        _formatBoxes.Clear();
        int row = 0;
        foreach (var format in _settings.ActiveFormats)
        {
            _formatsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, S(48)));
            var line = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
            line.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(48)));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(80)));
            var label = StudioTheme.Label(ColorDetails.GetFormatLabel(format), StudioTheme.Small, StudioTheme.Muted);
            var box = new TextBox
            {
                Name = $"Value{format}", ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text, Font = StudioTheme.Mono,
                Dock = DockStyle.Fill, Margin = new Padding(0, S(14), S(5), 0), AccessibleName = $"{format} color value"
            };
            var copy = new StudioButton { Text = "Copy", Name = $"Copy{format}", Font = StudioTheme.Small, Dock = DockStyle.Fill, Margin = new Padding(0, S(5), 0, S(5)), AccessibleName = $"Copy {format}" };
            copy.Click += (_, _) => CopyTextToClipboard(box.Text, $"Copied {ColorDetails.GetFormatLabel(format)}");
            line.Controls.Add(label, 0, 0);
            line.Controls.Add(box, 1, 0);
            line.Controls.Add(copy, 2, 0);
            _formatBoxes[format] = box;
            _formatsContainer.Controls.Add(line, 0, row++);
        }
        _formatsContainer.ResumeLayout(true);
        if (_currentPalette.Count > 0)
        {
            UpdatePaletteList(_currentPalette, _lblStatus.Text);
            _colorListView.SelectedIndex = Math.Clamp(selectedIndex, 0, _currentPalette.Count - 1);
        }
    }

    private void OpenSettingsDialog()
    {
        if (_isCapturing || _settingsDialogOpen) return;
        _settingsDialogOpen = true;
        NativeHotkeyManager.UnregisterAll(Handle);

        try
        {
            ShowMainWindow();
            using var dlg = new SettingsForm(_settings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _chkGroupSimilar.Checked = _settings.GroupSimilarColors;
                UpdateHotkeyLabels();
                RebuildDynamicFormatLayout();
                _lblStatus.Text = $"✔ Settings saved. Active color formats: {_settings.ActiveFormats.Count}.";
            }
        }
        finally
        {
            _settingsDialogOpen = false;
            RegisterConfiguredHotkeys();
        }
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private async void StartCapture(CaptureMode mode)
    {
        if (_isCapturing || _settingsDialogOpen || _allowExit) return;
        _isCapturing = true;
        bool wasVisible = Visible;
        bool wasMinimized = WindowState == FormWindowState.Minimized;
        try
        {
            _trayIcon.ContextMenuStrip?.Close();
            _cmbExportFormat.CloseDropDown();
            ScreenOverlayForm overlay;
            using (var hidden = new DesktopCaptureScope(this))
            {
                await hidden.WaitForDesktopAsync();
                if (IsDisposed || _allowExit) return;
                overlay = new ScreenOverlayForm(mode);
            }
            using var captured = overlay;
            var result = overlay.ShowDialog();
            if (result != DialogResult.OK)
            {
                if (wasVisible) { Show(); if (!wasMinimized) Activate(); }
                return;
            }
            ShowMainWindow();
            if (mode == CaptureMode.Point && overlay.PickedPointColor.HasValue)
            {
                _analysisVersion++;
                _analysisCancellation?.Cancel();
                _lastRegionBitmap?.Dispose();
                _lastRegionBitmap = null;

                var details = ColorDetails.FromColor(overlay.PickedPointColor.Value);
                var primaryFmt = _settings.ActiveFormats[0];
                string primaryVal = details.GetValue(primaryFmt);

                UpdatePaletteList([details], $"Point captured ({primaryVal}).");
                CopyTextToClipboard(primaryVal, $"Auto-copied {ColorDetails.GetFormatLabel(primaryFmt)}: {primaryVal}");
            }
            else if (mode == CaptureMode.Region && overlay.PickedRegionBitmap != null)
            {
                _lastRegionBitmap?.Dispose();
                _lastRegionBitmap = (Bitmap)overlay.PickedRegionBitmap.Clone();
                RunRegionAnalysis(_lastRegionBitmap);
            }
        }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException or ArgumentException)
        {
            if (!IsDisposed)
            {
                if (wasVisible) ShowMainWindow();
                MessageBox.Show($"Could not capture the desktop: {ex.Message}", "ChromaTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally { _isCapturing = false; }
    }

    private async void RunRegionAnalysis(Bitmap bmp)
    {
        int version = ++_analysisVersion;
        _analysisCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        _analysisCancellation = cancellation;
        using var snapshot = (Bitmap)bmp.Clone();
        bool groupSimilar = _settings.GroupSimilarColors;
        _lblStatus.Text = "Analyzing region…";
        try
        {
            var analysis = await Task.Run(() => PaletteAnalyzer.AnalyzeBitmap(snapshot, groupSimilar, cancellationToken: cancellation.Token));
            if (IsDisposed || _allowExit || version != _analysisVersion) return;
            UpdatePaletteList(analysis.Palette,
                $"Region {snapshot.Width}×{snapshot.Height} px ({analysis.TotalPixels:N0} px) scanned in {analysis.ElapsedMilliseconds} ms. Found {analysis.UniqueColorsFound:N0} colors (showing top {analysis.Palette.Count}).");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is ExternalException or ArgumentException or InvalidOperationException)
        {
            if (!IsDisposed && version == _analysisVersion) _lblStatus.Text = $"Could not analyze region: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_analysisCancellation, cancellation)) _analysisCancellation = null;
        }
    }

    internal void UpdatePaletteList(List<ColorDetails> palette, string statusMessage)
    {
        _currentPalette = palette;
        _colorListView.SetPalette(palette, _settings.ActiveFormats);
        _lblPaletteCount.Text = $"YOUR PALETTE   /   {palette.Count:N0} {(palette.Count == 1 ? "COLOR" : "COLORS")}";
        _paletteStrip.Colors = palette;
        _paletteStrip.Invalidate();
        if (palette.Count > 0)
        {
            DisplaySelectedColor(palette[0]);
        }
        _lblStatus.Text = statusMessage;
    }

    private void DisplaySelectedColor(ColorDetails color)
    {
        _previewSwatch.ColorDetails = color;
        _previewSwatch.AccessibleName = $"Selected color {color.Hex}, {color.Percentage:F1} percent";
        foreach (var format in _settings.ActiveFormats)
            if (_formatBoxes.TryGetValue(format, out var box))
            {
                box.Text = color.GetValue(format);
                _tips.SetToolTip(box, box.Text);
            }
    }

    private void CopyPrimaryColor()
    {
        if (_colorListView.SelectedColor is not { } color) return;
        var format = _settings.ActiveFormats[0];
        CopyTextToClipboard(color.GetValue(format), $"Copied {ColorDetails.GetFormatLabel(format)}: {color.GetValue(format)}");
    }

    private void CopySingleColorSummary()
    {
        if (_currentPalette.Count == 0) return;
        int idx = Math.Max(0, _colorListView.SelectedIndex);
        var color = _currentPalette[idx];

        string summary = PaletteExporter.FormatSingleColorSummary(color, _settings.ActiveFormats);
        CopyTextToClipboard(summary, $"Copied {_settings.ActiveFormats.Count} active format(s) to clipboard!");
    }

    private void CopyEntirePalette()
    {
        if (_currentPalette.Count == 0) return;
        var exportMode = (PaletteExportMode)_cmbExportFormat.SelectedIndex;
        string exportedText = PaletteExporter.FormatEntirePalette(_currentPalette, _settings.ActiveFormats, exportMode);

        CopyTextToClipboard(exportedText, $"Copied {_currentPalette.Count} colors to clipboard!");
    }

    private void CopyTextToClipboard(string text, string statusNotification)
    {
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            Clipboard.SetText(text);
            _lblStatus.Text = $"✔ {statusNotification}";
        }
        catch (ExternalException)
        {
            _lblStatus.Text = "Clipboard is busy. Please try copying again.";
        }
    }

    private static Icon CreateDynamicAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var b1 = new SolidBrush(StudioTheme.Accent);
        using var b2 = new SolidBrush(StudioTheme.Mint);
        using var b3 = new SolidBrush(StudioTheme.Raised);
        using var pen = new Pen(Color.White, 2f);

        g.FillEllipse(b1, 2, 2, 18, 18);
        g.FillEllipse(b2, 12, 4, 18, 18);
        g.FillEllipse(b3, 7, 12, 18, 18);
        g.DrawEllipse(pen, 7, 12, 18, 18);

        IntPtr handle = bmp.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _analysisVersion++;
            _analysisCancellation?.Cancel();
            _trayIcon.Visible = false;
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
            _lastRegionBitmap?.Dispose();
            _appIcon.Dispose();
            _tips.Dispose();
            _logoImage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
