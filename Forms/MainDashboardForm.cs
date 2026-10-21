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

    private readonly ListView _colorListView;
    private readonly Panel _rightCard;
    private readonly TableLayoutPanel _formatsContainer;
    private readonly ColorPreview _previewSwatch;
    private readonly Label _lblSelectedTitle;
    private readonly ComboBox _cmbExportFormat;
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
    private readonly ImageList _rowImages;
    private Bitmap _rowSpacer;
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

        var trayMenu = new ContextMenuStrip { BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text };
        _trayPointItem = new ToolStripMenuItem("", null, (_, _) => StartCapture(CaptureMode.Point));
        _trayRegionItem = new ToolStripMenuItem("", null, (_, _) => StartCapture(CaptureMode.Region));
        trayMenu.Items.AddRange([_trayPointItem, _trayRegionItem, new ToolStripSeparator()]);
        trayMenu.Items.Add("Settings…", null, (_, _) => OpenSettingsDialog());
        trayMenu.Items.Add("Open dashboard", null, (_, _) => ShowMainWindow());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon = new NotifyIcon { Icon = Icon, ContextMenuStrip = trayMenu, Visible = true };
        _trayIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowMainWindow(); };

        var header = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, BackColor = Color.Transparent };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        var logo = new PictureBox { Image = _appIcon.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        _logoImage = logo.Image;
        header.Controls.Add(logo, 0, 0);
        var wordmark = StudioTheme.Stack(
            (StudioTheme.Label("ChromaTray", StudioTheme.Title), 42),
            (StudioTheme.Label("A little color. A lot of possibility.", StudioTheme.Small, StudioTheme.Muted), 24));
        header.Controls.Add(wordmark, 1, 0);
        var settings = new StudioButton { Name = "SettingsButton", Text = "Settings", Glyph = StudioGlyph.Settings, Dock = DockStyle.Fill, Margin = new Padding(0, 14, 0, 14) };
        settings.Click += (_, _) => OpenSettingsDialog();
        header.Controls.Add(settings, 2, 0);

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
        captureActions.Controls.Add(_btnPoint, 0, 0);
        captureActions.Controls.Add(_btnRegion, 1, 0);

        var paletteCard = new SurfacePanel { Name = "PaletteCard", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0) };
        _lblPaletteCount = StudioTheme.Label("YOUR PALETTE", StudioTheme.Strong);
        _chkGroupSimilar = new CheckBox
        {
            Name = "GroupSimilarToggle", Text = "Group similar shades", Checked = _settings.GroupSimilarColors,
            ForeColor = StudioTheme.Muted, Font = StudioTheme.Small, AutoSize = true, Dock = DockStyle.Right,
            Cursor = Cursors.Hand, Margin = Padding.Empty, AccessibleDescription = "Merge similar RGB shades and recalculate the last region."
        };
        _chkGroupSimilar.CheckedChanged += (_, _) =>
        {
            _settings.GroupSimilarColors = _chkGroupSimilar.Checked;
            try { _settings.Save(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"Could not save settings: {ex.Message}", "ChromaTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (_lastRegionBitmap != null) RunRegionAnalysis(_lastRegionBitmap);
        };
        var paletteHeader = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        paletteHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        paletteHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paletteHeader.Controls.Add(_lblPaletteCount, 0, 0);
        paletteHeader.Controls.Add(_chkGroupSimilar, 1, 0);
        _paletteStrip = new PaletteStrip { Name = "PaletteOverview", Margin = new Padding(0, 10, 0, 10) };

        _rowImages = new ImageList { ImageSize = new Size(1, 40), ColorDepth = ColorDepth.Depth32Bit };
        // ImageList can defer native image creation until the ListView gets a
        // handle. Retain the source bitmap for that deferred conversion.
        _rowSpacer = new Bitmap(1, 40);
        _rowImages.Images.Add(_rowSpacer);
        _colorListView = new PaletteListView
        {
            Name = "PaletteTable", Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            MultiSelect = false, HideSelection = false, BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text,
            BorderStyle = BorderStyle.None, OwnerDraw = true, SmallImageList = _rowImages, HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Font = StudioTheme.Mono, ShowItemToolTips = true, Margin = Padding.Empty,
            AccessibleName = "Captured color palette", AccessibleDescription = "Select a color to see all enabled formats. Double-click to copy."
        };
        _colorListView.DrawColumnHeader += (_, e) =>
        {
            using var bg = new SolidBrush(StudioTheme.Surface);
            e.Graphics.FillRectangle(bg, e.Bounds);
            var rect = e.Bounds; rect.Inflate(-8, 0);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text, StudioTheme.Small, rect, StudioTheme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using var line = new Pen(StudioTheme.Border);
            e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        };
        _colorListView.DrawItem += (_, _) => { };
        _colorListView.DrawSubItem += DrawColorListSubItem;
        _colorListView.Resize += (_, _) => ResizePaletteColumns();
        _colorListView.SelectedIndexChanged += (_, _) =>
        {
            if (_colorListView.SelectedItems.Count > 0 && _colorListView.SelectedItems[0].Tag is ColorDetails color)
                DisplaySelectedColor(color);
        };
        _colorListView.DoubleClick += (_, _) => CopyPrimaryColor();
        _colorListView.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C) { CopyPrimaryColor(); e.SuppressKeyPress = true; }
        };

        _cmbExportFormat = new ComboBox
        {
            Name = "ExportMode", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill,
            BackColor = StudioTheme.Raised, ForeColor = StudioTheme.Text, Font = StudioTheme.Small,
            FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 11, 12, 0), AccessibleName = "Palette export format"
        };
        _cmbExportFormat.Items.AddRange(["All active formats", "Primary format only", "CSS custom properties", "JSON palette array"]);
        _cmbExportFormat.SelectedIndex = 0;
        var exportButton = new StudioButton { Name = "ExportPaletteButton", Text = "Copy palette", Glyph = StudioGlyph.Copy, AccentColor = StudioTheme.Mint, Dock = DockStyle.Fill, Margin = new Padding(0, 7, 0, 0) };
        exportButton.Click += (_, _) => CopyEntirePalette();
        var exportRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        exportRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        exportRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154));
        exportRow.Controls.Add(_cmbExportFormat, 0, 0);
        exportRow.Controls.Add(exportButton, 1, 0);
        paletteCard.Controls.Add(StudioTheme.Stack(
            (paletteHeader, 32), (_paletteStrip, 32), (_colorListView, -1),
            (StudioTheme.Label("Select a shade to explore its formats. Double-click to copy.", StudioTheme.Small, StudioTheme.Muted), 32),
            (exportRow, 52)));

        _rightCard = new SurfacePanel { Name = "InspectorCard", Dock = DockStyle.Fill, Margin = Padding.Empty };
        _lblSelectedTitle = StudioTheme.Label("COLOR INSPECTOR", StudioTheme.Strong);
        _previewSwatch = new ColorPreview { Name = "SelectedColorPreview", Margin = new Padding(0, 8, 0, 10) };
        _formatsContainer = new TableLayoutPanel { Name = "ColorFormats", ColumnCount = 1, Margin = Padding.Empty, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _formatsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var copyFormats = new StudioButton { Name = "CopyFormatsButton", Text = "Copy active formats", Primary = true, Glyph = StudioGlyph.Copy, Margin = new Padding(0, 12, 0, 0) };
        copyFormats.Click += (_, _) => CopySingleColorSummary();
        var formatsViewport = new Panel { Name = "FormatsViewport", AutoScroll = true, Margin = Padding.Empty };
        _formatsContainer.Dock = DockStyle.Top;
        formatsViewport.Controls.Add(_formatsContainer);
        _rightCard.Controls.Add(StudioTheme.Stack(
            (_lblSelectedTitle, 30), (_previewSwatch, 128), (formatsViewport, -1), (copyFormats, 50),
            (StudioTheme.Label("All enabled formats. One click to copy.", StudioTheme.Small, StudioTheme.Muted), 28)));
        var workspace = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 376));
        workspace.Controls.Add(paletteCard, 0, 0);
        workspace.Controls.Add(_rightCard, 1, 0);

        _lblStatus = StudioTheme.Label("Ready to capture.", StudioTheme.Small, StudioTheme.Muted);
        _lblStatus.Name = "StatusText";
        _lblStatus.TextChanged += (_, _) => _tips.SetToolTip(_lblStatus, _lblStatus.Text);
        var trayHint = StudioTheme.Label("Closing the window keeps ChromaTray in your tray.", StudioTheme.Small, StudioTheme.Muted);
        trayHint.TextAlign = ContentAlignment.MiddleRight;
        var footer = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        footer.Controls.Add(_lblStatus, 0, 0);
        footer.Controls.Add(trayHint, 1, 0);
        var root = StudioTheme.Stack((header, 72), (captureActions, 100), (new Panel { Margin = Padding.Empty }, 20), (workspace, -1), (footer, 40));
        root.Padding = new Padding(24, 16, 24, 8);
        Controls.Add(root);
        _tips.SetToolTip(_btnPoint, "Pick a pixel. Use arrow keys to move one pixel and Enter to select.");
        _tips.SetToolTip(_chkGroupSimilar, "Round RGB channels in steps of 8 to combine nearby shades.");
        UpdateHotkeyLabels();
        RebuildDynamicFormatLayout();
        var initial = ColorDetails.FromColor(Color.FromArgb(181, 166, 255));
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
        UpdateRowHeight();
        ResizePaletteColumns();
        RegisterConfiguredHotkeys();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateRowHeight();
        ResizePaletteColumns();
    }

    private void UpdateRowHeight()
    {
        if (_rowImages is null) return;
        int height = Math.Clamp((int)Math.Round(40 * DeviceDpi / 96f), 24, 256);
        if (_rowImages.ImageSize.Height == height) return;
        _rowImages.Images.Clear();
        _rowSpacer.Dispose();
        _rowImages.ImageSize = new Size(1, height);
        _rowSpacer = new Bitmap(1, height);
        _rowImages.Images.Add(_rowSpacer);
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
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        _formatsContainer.SuspendLayout();
        foreach (Control control in _formatsContainer.Controls.Cast<Control>().ToArray()) control.Dispose();
        _formatsContainer.RowStyles.Clear();
        _formatsContainer.RowCount = _settings.ActiveFormats.Count;
        _formatBoxes.Clear();
        int row = 0;
        foreach (var format in _settings.ActiveFormats)
        {
            _formatsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, S(40)));
            var line = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(45)));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(52)));
            var label = StudioTheme.Label(ColorDetails.GetFormatLabel(format), StudioTheme.Small, StudioTheme.Muted);
            var box = new TextBox
            {
                Name = $"Value{format}", ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = StudioTheme.Surface, ForeColor = StudioTheme.Text, Font = StudioTheme.Mono,
                Dock = DockStyle.Fill, Margin = new Padding(0, S(11), S(5), 0), AccessibleName = $"{format} color value"
            };
            var copy = new StudioButton { Text = "Copy", Name = $"Copy{format}", Font = StudioTheme.Small, Dock = DockStyle.Fill, Margin = new Padding(0, S(4), 0, S(4)), AccessibleName = $"Copy {format}" };
            copy.Click += (_, _) => CopyTextToClipboard(box.Text, $"Copied {ColorDetails.GetFormatLabel(format)}");
            line.Controls.Add(label, 0, 0);
            line.Controls.Add(box, 1, 0);
            line.Controls.Add(copy, 2, 0);
            _formatBoxes[format] = box;
            _formatsContainer.Controls.Add(line, 0, row++);
        }
        _formatsContainer.ResumeLayout(true);
        _colorListView.BeginUpdate();
        _colorListView.Columns.Clear();
        _colorListView.Columns.Add("", 52);
        _colorListView.Columns.Add(ColorDetails.GetFormatLabel(_settings.ActiveFormats[0]), 220);
        _colorListView.Columns.Add("SHARE", 146);
        _colorListView.Columns.Add("PIXELS", 100, HorizontalAlignment.Right);
        ResizePaletteColumns();
        _colorListView.EndUpdate();
        if (_currentPalette.Count > 0) UpdatePaletteList(_currentPalette, _lblStatus.Text);
    }

    private void ResizePaletteColumns()
    {
        if (_colorListView.Columns.Count != 4) return;
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        _colorListView.Columns[0].Width = S(52);
        _colorListView.Columns[2].Width = S(146);
        _colorListView.Columns[3].Width = S(100);
        _colorListView.Columns[1].Width = Math.Max(S(150), _colorListView.ClientSize.Width - S(298) - 4);
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
            if (wasVisible) Hide();
            // Let the compositor remove the dashboard before taking the frozen snapshot.
            await Task.Delay(150);
            if (IsDisposed || _allowExit) return;
            using var overlay = new ScreenOverlayForm(mode);
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
        _colorListView.BeginUpdate();
        _colorListView.Items.Clear();
        foreach (var color in palette)
        {
            var item = new ListViewItem("") { Tag = color, ImageIndex = 0 };
            item.SubItems.Add(color.GetValue(_settings.ActiveFormats[0]));
            item.SubItems.Add($"{color.Percentage:F1}%");
            item.SubItems.Add($"{color.PixelCount:N0}");
            item.ToolTipText = PaletteExporter.FormatSingleColorSummary(color, _settings.ActiveFormats);
            _colorListView.Items.Add(item);
        }
        _colorListView.EndUpdate();
        _lblPaletteCount.Text = $"YOUR PALETTE   /   {palette.Count:N0} {(palette.Count == 1 ? "COLOR" : "COLORS")}";
        _paletteStrip.Colors = palette;
        _paletteStrip.Invalidate();
        if (palette.Count > 0)
        {
            _colorListView.Items[0].Selected = true;
            _colorListView.Items[0].Focused = true;
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

    private void DrawColorListSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item?.Tag is not ColorDetails color) return;
        int S(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        var g = e.Graphics;
        bool selected = e.Item.Selected;
        using var background = new SolidBrush(selected ? Color.FromArgb(43, 39, 61) : e.ItemIndex % 2 == 0 ? StudioTheme.Surface : Color.FromArgb(25, 28, 38));
        g.FillRectangle(background, e.Bounds);
        if (e.ColumnIndex == 0)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (selected)
            {
                using var indicator = new SolidBrush(StudioTheme.Accent);
                g.FillRectangle(indicator, e.Bounds.X, e.Bounds.Y + S(9), S(3), e.Bounds.Height - S(18));
            }
            var swatch = new Rectangle(e.Bounds.X + S(13), e.Bounds.Y + (e.Bounds.Height - S(26)) / 2, S(26), S(26));
            using var path = StudioTheme.Round(swatch, S(6));
            using var fill = new SolidBrush(color.RawColor);
            using var border = new Pen(Color.FromArgb(100, StudioTheme.Muted));
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }
        else if (e.ColumnIndex == 2)
        {
            var label = new Rectangle(e.Bounds.X + S(8), e.Bounds.Y, S(51), e.Bounds.Height);
            TextRenderer.DrawText(g, e.SubItem?.Text, StudioTheme.Small, label, StudioTheme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            var track = new Rectangle(e.Bounds.X + S(65), e.Bounds.Y + e.Bounds.Height / 2 - S(2), Math.Max(1, e.Bounds.Width - S(82)), S(4));
            using var trackBrush = new SolidBrush(StudioTheme.Border);
            using var fillBrush = new SolidBrush(selected ? StudioTheme.Accent : StudioTheme.Mint);
            g.FillRectangle(trackBrush, track);
            g.FillRectangle(fillBrush, track.X, track.Y, (int)(track.Width * Math.Clamp(color.Percentage, 0, 100) / 100), track.Height);
        }
        else
        {
            var rect = e.Bounds; rect.Inflate(-S(8), 0);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            flags |= e.ColumnIndex == 3 ? TextFormatFlags.Right : TextFormatFlags.Left;
            TextRenderer.DrawText(g, e.SubItem?.Text, StudioTheme.Mono, rect,
                e.ColumnIndex == 3 ? StudioTheme.Muted : StudioTheme.Text, flags);
        }
    }

    private void CopyPrimaryColor()
    {
        if (_colorListView.SelectedItems.Count == 0 || _colorListView.SelectedItems[0].Tag is not ColorDetails color) return;
        var format = _settings.ActiveFormats[0];
        CopyTextToClipboard(color.GetValue(format), $"Copied {ColorDetails.GetFormatLabel(format)}: {color.GetValue(format)}");
    }

    private void CopySingleColorSummary()
    {
        if (_colorListView.SelectedIndices.Count == 0 && _currentPalette.Count == 0) return;
        int idx = _colorListView.SelectedIndices.Count > 0 ? _colorListView.SelectedIndices[0] : 0;
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
        using var b3 = new SolidBrush(StudioTheme.Danger);
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
            _rowImages.Dispose();
            _rowSpacer.Dispose();
            _logoImage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
