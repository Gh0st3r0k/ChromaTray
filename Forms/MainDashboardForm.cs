using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ChromaTray.Forms.Controls;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

/// <summary>
/// Главное окно связывает настройки, трей и действия пользователя.
/// Создание интерфейса находится в .Layout.cs, захват — в .Capture.cs, палитра и буфер — в .Palette.cs.
/// </summary>
public partial class MainDashboardForm : Form
{
    private readonly AppSettings _settings;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _trayPointItem = new();
    private readonly ToolStripMenuItem _trayRegionItem = new();

    private bool _isExiting;
    private bool _isSettingsDialogOpen;
    private readonly Icon _appIcon;
    private readonly ToolTip _toolTips = new();

    public MainDashboardForm() : this(AppSettings.Load())
    {
    }

    internal MainDashboardForm(AppSettings initialSettings)
    {
        _settings = initialSettings;
        ConfigureWindow();
        _appIcon = CreateDynamicAppIcon();
        Icon = _appIcon;
        _trayIcon = CreateTrayIcon();
        InitializeLayout();
        UpdateHotkeyLabels();
        RebuildDynamicFormatLayout();
        var initialColor = ColorDetails.FromColor(StudioTheme.Accent);
        UpdatePaletteList([initialColor], "Ready. Pick a point or scan a region to start.");
    }

    private NotifyIcon CreateTrayIcon()
    {
        var trayMenu = new ContextMenuStrip
        {
            BackColor = StudioTheme.Surface,
            ForeColor = StudioTheme.Text,
            Renderer = new StudioMenuRenderer()
        };
        _trayPointItem.Click += (_, _) => StartCapture(CaptureMode.Point);
        _trayRegionItem.Click += (_, _) => StartCapture(CaptureMode.Region);
        trayMenu.Items.AddRange([_trayPointItem, _trayRegionItem, new ToolStripSeparator()]);
        trayMenu.Items.Add("Settings…", null, (_, _) => OpenSettingsDialog());
        trayMenu.Items.Add("Open dashboard", null, (_, _) => ShowMainWindow());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        var trayIcon = new NotifyIcon
        {
            Icon = Icon,
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };

        return trayIcon;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // WinForms может создать HWND ещё до конца инициализации контролов.
        StudioTheme.ApplyWindowChrome(this);
        if (_statusLabel is not null)
        {
            RegisterConfiguredHotkeys();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
        var area = Screen.FromControl(this).WorkingArea;
        MinimumSize = new Size(Math.Min(Scale(980), area.Width), Math.Min(Scale(660), area.Height));
        if (Width > area.Width || Height > area.Height)
        {
            Bounds = new Rectangle(area.Left, area.Top, Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        }
        ApplyDpiMetrics();
        RegisterConfiguredHotkeys();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (_formatRows is not null)
        {
            ApplyDpiMetrics();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_dashboardLayout is not null)
        {
            ApplyResponsiveLayout();
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        NativeHotkeyManager.UnregisterAll(Handle);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeHotkeyManager.HotkeyMessage)
        {
            int id = m.WParam.ToInt32();
            if (id == NativeHotkeyManager.PointHotkeyId)
            {
                StartCapture(CaptureMode.Point);
            }
            else if (id == NativeHotkeyManager.RegionHotkeyId)
            {
                StartCapture(CaptureMode.Region);
            }
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Крестик скрывает окно; только команда Exit завершает приложение.
        if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
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
        if (!IsHandleCreated)
        {
            return;
        }
        bool pointRegistered = NativeHotkeyManager.Register(Handle, NativeHotkeyManager.PointHotkeyId, _settings.PointHotkey);
        bool regionRegistered = NativeHotkeyManager.Register(Handle, NativeHotkeyManager.RegionHotkeyId, _settings.RegionHotkey);

        if (!pointRegistered || !regionRegistered)
        {
            _statusLabel.Text = $"Hotkey unavailable: {(!pointRegistered ? "Point " : "")}{(!regionRegistered ? "Region" : "")}. Choose another shortcut in Settings; capture buttons still work.";
        }
    }

    private void UpdateHotkeyLabels()
    {
        _pointButton.Shortcut = _settings.PointHotkey.ToString();
        _pointButton.Invalidate();
        _regionButton.Shortcut = _settings.RegionHotkey.ToString();
        _regionButton.Invalidate();
        _trayPointItem.Text = $"Pick Point ({_settings.PointHotkey})";
        _trayRegionItem.Text = $"Scan Region ({_settings.RegionHotkey})";
        string tooltip = $"ChromaTray ({_settings.PointHotkey} | {_settings.RegionHotkey})";
        _trayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    private void OpenSettingsDialog()
    {
        if (_isCapturing || _isSettingsDialogOpen)
        {
            return;
        }
        _isSettingsDialogOpen = true;
        NativeHotkeyManager.UnregisterAll(Handle);

        try
        {
            ShowMainWindow();
            using var dialog = new SettingsForm(_settings);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _groupSimilarCheckBox.Checked = _settings.GroupSimilarColors;
                UpdateHotkeyLabels();
                RebuildDynamicFormatLayout();
                _statusLabel.Text = $"✔ Settings saved. Active color formats: {_settings.ActiveFormats.Count}.";
            }
        }
        finally
        {
            _isSettingsDialogOpen = false;
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
        _isExiting = true;
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private static Icon CreateDynamicAppIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var accentBrush = new SolidBrush(StudioTheme.Accent);
        using var mintBrush = new SolidBrush(StudioTheme.Mint);
        using var blueBrush = new SolidBrush(StudioTheme.Raised);
        using var pen = new Pen(Color.White, 2f);

        graphics.FillEllipse(accentBrush, 2, 2, 18, 18);
        graphics.FillEllipse(mintBrush, 12, 4, 18, 18);
        graphics.FillEllipse(blueBrush, 7, 12, 18, 18);
        graphics.DrawEllipse(pen, 7, 12, 18, 18);

        IntPtr handle = bitmap.GetHicon();
        try
        {
            // FromHandle не владеет HICON. Клонируем и освобождаем исходный native-ресурс ниже.
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
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
            _toolTips.Dispose();
            _logoImage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
