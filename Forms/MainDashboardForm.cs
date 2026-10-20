using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

public class MainDashboardForm : Form
{
    private readonly AppSettings _settings;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _trayPointItem;
    private readonly ToolStripMenuItem _trayRegionItem;

    private readonly Button _btnPoint;
    private readonly Button _btnRegion;
    private readonly CheckBox _chkGroupSimilar;

    private readonly ListView _colorListView;
    private readonly Panel _rightCard;
    private readonly Panel _formatsContainer;
    private readonly Panel _previewSwatch;
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
    private readonly Font _listHeaderFont = new("Segoe UI Semibold", 9f);
    private readonly Font _listValueFont = new("Consolas", 9.5f);

    public MainDashboardForm()
    {
        _settings = AppSettings.Load();

        Text = "ChromaTray — Screen Color & Palette Inspector";
        Size = new Size(1160, 660);
        MinimumSize = new Size(980, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.FromArgb(235, 235, 240);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        _appIcon = CreateDynamicAppIcon();
        Icon = _appIcon;

        // --- System Tray Setup ---
        var trayMenu = new ContextMenuStrip();
        _trayPointItem = new ToolStripMenuItem("", null, (_, _) => StartCapture(CaptureMode.Point));
        _trayRegionItem = new ToolStripMenuItem("", null, (_, _) => StartCapture(CaptureMode.Region));
        trayMenu.Items.Add(_trayPointItem);
        trayMenu.Items.Add(_trayRegionItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("⚙ Settings...", null, (_, _) => OpenSettingsDialog());
        trayMenu.Items.Add("Open Dashboard", null, (_, _) => ShowMainWindow());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Icon = this.Icon,
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowMainWindow();
        };

        // --- Top Toolbar ---
        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Color.FromArgb(32, 32, 38)
        };

        _btnPoint = CreateStyledButton("", Color.FromArgb(59, 130, 246), 235);
        _btnPoint.Location = new Point(16, 16);
        _btnPoint.Click += (_, _) => StartCapture(CaptureMode.Point);

        _btnRegion = CreateStyledButton("", Color.FromArgb(16, 185, 129), 245);
        _btnRegion.Location = new Point(262, 16);
        _btnRegion.Click += (_, _) => StartCapture(CaptureMode.Region);

        _chkGroupSimilar = new CheckBox
        {
            Text = "Group similar shades",
            Checked = _settings.GroupSimilarColors,
            AutoSize = true,
            Location = new Point(522, 26),
            ForeColor = Color.FromArgb(205, 205, 215)
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

        var btnSettings = CreateStyledButton("⚙ Settings", Color.FromArgb(63, 63, 75), 115);
        btnSettings.Location = new Point(765, 16);
        btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSettings.Click += (_, _) => OpenSettingsDialog();

        topPanel.Controls.AddRange(new Control[] { _btnPoint, _btnRegion, _chkGroupSimilar, btnSettings });

        // --- Bottom Status Bar ---
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Color.FromArgb(18, 18, 22),
            Padding = new Padding(16, 6, 16, 6)
        };
        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Ready. Closing this window [X] minimizes ChromaTray to the system tray.",
            ForeColor = Color.FromArgb(150, 150, 165)
        };
        bottomPanel.Controls.Add(_lblStatus);

        // --- Main Split Workspace ---
        var mainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

        _rightCard = new Panel
        {
            Dock = DockStyle.Right,
            Width = 340,
            BackColor = Color.FromArgb(32, 32, 38),
            Padding = new Padding(16)
        };

        _lblSelectedTitle = new Label
        {
            Text = "Selected Color",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Location = new Point(16, 14),
            AutoSize = true
        };

        _previewSwatch = new Panel
        {
            Location = new Point(16, 44),
            Size = new Size(308, 72),
            BackColor = Color.FromArgb(59, 130, 246),
            BorderStyle = BorderStyle.FixedSingle
        };

        _formatsContainer = new Panel
        {
            Location = new Point(16, 130),
            Size = new Size(308, 200)
        };

        var btnCopyAllFormats = CreateStyledButton("📋 Copy Active Formats of This Color", Color.FromArgb(79, 70, 229), 308, 36);
        btnCopyAllFormats.Location = new Point(16, 340);
        btnCopyAllFormats.Click += (_, _) => CopySingleColorSummary();

        _rightCard.Controls.AddRange(new Control[] { _lblSelectedTitle, _previewSwatch, _formatsContainer, btnCopyAllFormats });

        // --- Left Palette List Panel ---
        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 16, 0)
        };

        var leftFooter = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 8, 0, 0)
        };

        _cmbExportFormat = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 230,
            Location = new Point(0, 10),
            BackColor = Color.FromArgb(32, 32, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _cmbExportFormat.Items.AddRange(new object[]
        {
            "All Active Formats (Pipe-separated)",
            "Primary Format Only (Line-by-line)",
            "CSS Custom Properties (--color-1)",
            "JSON Palette Array"
        });
        _cmbExportFormat.SelectedIndex = 0;

        var btnExportAll = CreateStyledButton("📦 Copy Entire Palette", Color.FromArgb(16, 185, 129), 210, 32);
        btnExportAll.Location = new Point(242, 8);
        btnExportAll.Click += (_, _) => CopyEntirePalette();

        leftFooter.Controls.AddRange(new Control[] { _cmbExportFormat, btnExportAll });

        _colorListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            MultiSelect = false,
            BackColor = Color.FromArgb(32, 32, 38),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            OwnerDraw = true
        };

        _colorListView.DrawColumnHeader += (_, e) =>
        {
            using var bg = new SolidBrush(Color.FromArgb(42, 42, 50));
            e.Graphics.FillRectangle(bg, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text, _listHeaderFont,
                e.Bounds, Color.FromArgb(200, 200, 215), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        };
        _colorListView.DrawItem += (_, _) => { };
        _colorListView.DrawSubItem += DrawColorListSubItem;
        _colorListView.SelectedIndexChanged += (_, _) =>
        {
            if (_colorListView.SelectedIndices.Count > 0)
            {
                int idx = _colorListView.SelectedIndices[0];
                if (idx >= 0 && idx < _currentPalette.Count)
                    DisplaySelectedColor(_currentPalette[idx]);
            }
        };
        _colorListView.DoubleClick += (_, _) =>
        {
            if (_colorListView.SelectedIndices.Count > 0)
            {
                var item = _currentPalette[_colorListView.SelectedIndices[0]];
                var primaryFormat = _settings.ActiveFormats[0];
                string val = item.GetValue(primaryFormat);
                CopyTextToClipboard(val, $"Copied {ColorDetails.GetFormatLabel(primaryFormat)}: {val}");
            }
        };

        leftPanel.Controls.Add(_colorListView);
        leftPanel.Controls.Add(leftFooter);

        mainContainer.Controls.Add(leftPanel);
        mainContainer.Controls.Add(_rightCard);

        Controls.Add(mainContainer);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);

        UpdateHotkeyLabels();
        RebuildDynamicFormatLayout();

        var initial = ColorDetails.FromColor(Color.FromArgb(59, 130, 246));
        UpdatePaletteList([initial], "Ready. Use the buttons above or global hotkeys to inspect screen colors.");
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Handle creation can happen while controls are still being initialized.
        if (_lblStatus is not null) RegisterConfiguredHotkeys();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RegisterConfiguredHotkeys();
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
        _btnPoint.Text = $"🎯 Pick Point ({_settings.PointHotkey})";
        _btnRegion.Text = $"📐 Scan Region ({_settings.RegionHotkey})";
        _trayPointItem.Text = $"🎯 Pick Point ({_settings.PointHotkey})";
        _trayRegionItem.Text = $"📐 Scan Region ({_settings.RegionHotkey})";
        string tooltip = $"ChromaTray ({_settings.PointHotkey} | {_settings.RegionHotkey})";
        _trayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    private void RebuildDynamicFormatLayout()
    {
        foreach (Control control in _formatsContainer.Controls.Cast<Control>().ToArray()) control.Dispose();
        _formatBoxes.Clear();

        int yOffset = 0;
        foreach (var fmt in _settings.ActiveFormats)
        {
            string labelText = ColorDetails.GetFormatLabel(fmt);

            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(0, yOffset + 5),
                Width = 54,
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(170, 170, 185)
            };

            var txt = new TextBox
            {
                Location = new Point(56, yOffset + 2),
                Width = 172,
                ReadOnly = true,
                BackColor = Color.FromArgb(22, 22, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5f)
            };
            _formatBoxes[fmt] = txt;

            var btnCopy = CreateStyledButton("Copy", Color.FromArgb(63, 63, 75), 72, 26);
            btnCopy.Location = new Point(234, yOffset);
            ColorFormat capturedFmt = fmt;
            btnCopy.Click += (_, _) => CopyTextToClipboard(_formatBoxes[capturedFmt].Text, $"Copied {labelText}");

            _formatsContainer.Controls.AddRange(new Control[] { lbl, txt, btnCopy });
            yOffset += 38;
        }

        _colorListView.BeginUpdate();
        _colorListView.Columns.Clear();
        _colorListView.Columns.Add("Swatch", 62);

        foreach (var fmt in _settings.ActiveFormats)
        {
            int width = fmt == ColorFormat.Hex ? 95 : 145;
            _colorListView.Columns.Add(ColorDetails.GetFormatLabel(fmt), width);
        }

        _colorListView.Columns.Add("Share", 70);
        _colorListView.EndUpdate();

        if (_currentPalette.Count > 0)
        {
            UpdatePaletteList(_currentPalette, _lblStatus.Text);
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

    private void UpdatePaletteList(List<ColorDetails> palette, string statusMessage)
    {
        _currentPalette = palette;
        _colorListView.BeginUpdate();
        _colorListView.Items.Clear();

        foreach (var item in palette)
        {
            var lvi = new ListViewItem("");
            foreach (var fmt in _settings.ActiveFormats)
            {
                lvi.SubItems.Add(item.GetValue(fmt));
            }
            lvi.SubItems.Add($"{item.Percentage:F1}%");
            lvi.Tag = item;
            _colorListView.Items.Add(lvi);
        }

        _colorListView.EndUpdate();

        if (palette.Count > 0)
        {
            _colorListView.Items[0].Selected = true;
            DisplaySelectedColor(palette[0]);
        }

        _lblStatus.Text = statusMessage;
    }

    private void DisplaySelectedColor(ColorDetails d)
    {
        _previewSwatch.BackColor = d.RawColor;
        _lblSelectedTitle.Text = $"Color {d.Hex} ({d.Percentage:F1}%)";

        foreach (var fmt in _settings.ActiveFormats)
        {
            if (_formatBoxes.TryGetValue(fmt, out var box))
                box.Text = d.GetValue(fmt);
        }
    }

    private void DrawColorListSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item?.Tag is not ColorDetails details) return;

        bool selected = e.Item.Selected;
        using var bgBrush = new SolidBrush(selected ? Color.FromArgb(55, 65, 85) : Color.FromArgb(32, 32, 38));
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        if (e.ColumnIndex == 0)
        {
            var swatchRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 3, e.Bounds.Width - 16, e.Bounds.Height - 6);
            using var colorBrush = new SolidBrush(details.RawColor);
            using var borderPen = new Pen(Color.FromArgb(90, 90, 100));
            e.Graphics.FillRectangle(colorBrush, swatchRect);
            e.Graphics.DrawRectangle(borderPen, swatchRect);
        }
        else
        {
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text, _listValueFont,
                e.Bounds, Color.FromArgb(230, 230, 240), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
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

    private static Button CreateStyledButton(string text, Color bgColor, int width, int height = 42)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = bgColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9f)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Icon CreateDynamicAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var b1 = new SolidBrush(Color.FromArgb(59, 130, 246));
        using var b2 = new SolidBrush(Color.FromArgb(16, 185, 129));
        using var b3 = new SolidBrush(Color.FromArgb(244, 63, 94));
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
            _listHeaderFont.Dispose();
            _listValueFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
