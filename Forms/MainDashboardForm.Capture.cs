using System.Runtime.InteropServices;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

// Подготовка рабочего стола, выбор точки/региона и отменяемый анализ в фоне.
public partial class MainDashboardForm
{
    private Bitmap? _lastRegionBitmap;
    private bool _isCapturing;
    private int _analysisVersion;
    private CancellationTokenSource? _analysisCancellation;

    // Асинхронная точка входа для кнопок и Win32-хоткеев. Ожидаемые ошибки обрабатываются здесь.
    private async void StartCapture(CaptureMode mode)
    {
        if (_isCapturing || _isSettingsDialogOpen || _isExiting)
        {
            return;
        }
        _isCapturing = true;
        bool wasVisible = Visible;
        bool wasMinimized = WindowState == FormWindowState.Minimized;
        try
        {
            _trayIcon.ContextMenuStrip?.Close();
            _exportFormatSelect.CloseDropDown();
            ScreenOverlayForm overlay;
            using (var hidden = new DesktopCaptureScope(this))
            {
                // Сначала скрываем окно и ждём завершения кадра DWM, затем создаём снимок.
                await hidden.WaitForDesktopAsync();
                if (IsDisposed || _isExiting)
                {
                    return;
                }
                overlay = new ScreenOverlayForm(mode);
            }
            using var captured = overlay;
            var result = overlay.ShowDialog();
            if (result != DialogResult.OK)
            {
                if (wasVisible)
                {
                    Show();
                    if (!wasMinimized)
                    {
                        Activate();
                    }
                }
                return;
            }
            ShowMainWindow();
            if (mode == CaptureMode.Point && overlay.PickedPointColor.HasValue)
            {
                ApplyPointCapture(overlay.PickedPointColor.Value);
            }
            else if (mode == CaptureMode.Region && overlay.PickedRegionBitmap != null)
            {
                ApplyRegionCapture(overlay.PickedRegionBitmap);
            }
        }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException or ArgumentException)
        {
            if (!IsDisposed)
            {
                if (wasVisible)
                {
                    ShowMainWindow();
                }
                MessageBox.Show($"Could not capture the desktop: {ex.Message}", "ChromaTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _isCapturing = false;
        }
    }

    private async void RunRegionAnalysis(Bitmap bitmap)
    {
        int requestVersion = ++_analysisVersion;
        _analysisCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        _analysisCancellation = cancellation;
        // Фоновый анализ получает собственный снимок: новый захват может освободить предыдущую область.
        using var snapshot = (Bitmap)bitmap.Clone();
        bool groupSimilar = _settings.GroupSimilarColors;
        _statusLabel.Text = "Analyzing region…";
        try
        {
            var analysis = await Task.Run(() => PaletteAnalyzer.AnalyzeBitmap(snapshot, groupSimilar, cancellationToken: cancellation.Token));
            // Отменённый запрос может успеть завершиться: только последний запрос обновляет UI.
            if (IsDisposed || _isExiting || requestVersion != _analysisVersion)
            {
                return;
            }
            UpdatePaletteList(analysis.Palette,
                $"Region {snapshot.Width}×{snapshot.Height} px ({analysis.TotalPixels:N0} px) " +
                $"scanned in {analysis.ElapsedMilliseconds} ms. " +
                $"Found {analysis.UniqueColorsFound:N0} colors (showing top {analysis.Palette.Count}).");
        }
        catch (OperationCanceledException)
        {
            // Новый захват или смена группировки отменили предыдущий запрос.
        }
        catch (Exception ex) when (ex is ExternalException or ArgumentException or InvalidOperationException)
        {
            if (!IsDisposed && requestVersion == _analysisVersion)
            {
                _statusLabel.Text = $"Could not analyze region: {ex.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_analysisCancellation, cancellation))
            {
                _analysisCancellation = null;
            }
        }
    }

    private void ApplyPointCapture(Color color)
    {
        _analysisVersion++;
        _analysisCancellation?.Cancel();
        _lastRegionBitmap?.Dispose();
        _lastRegionBitmap = null;

        var details = ColorDetails.FromColor(color);
        var primaryFormat = _settings.ActiveFormats[0];
        string primaryValue = details.GetValue(primaryFormat);

        UpdatePaletteList([details], $"Point captured ({primaryValue}).");
        CopyTextToClipboard(primaryValue, $"Auto-copied {ColorDetails.GetFormatLabel(primaryFormat)}: {primaryValue}");
    }

    private void ApplyRegionCapture(Bitmap bitmap)
    {
        _lastRegionBitmap?.Dispose();
        // Overlay владеет своим Bitmap. Копия нужна для повторного анализа после закрытия overlay.
        _lastRegionBitmap = (Bitmap)bitmap.Clone();
        RunRegionAnalysis(_lastRegionBitmap);
    }
}
