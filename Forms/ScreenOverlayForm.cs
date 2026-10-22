using System.Drawing.Imaging;
using ChromaTray.Models;
using ChromaTray.Services;

namespace ChromaTray.Forms;

/// <summary>
/// Полноэкранный выбор по замороженному снимку рабочего стола.
/// Выбранные значения читаются из снимка, а не из нарисованных поверх него подсказок.
/// </summary>
public partial class ScreenOverlayForm : Form
{
    private const int MagnifierCellsPerSide = 11;
    private const int MagnifierCenterIndex = MagnifierCellsPerSide / 2;

    private readonly CaptureMode _mode;
    private readonly Bitmap _screenSnapshot;
    // Границы всех мониторов в экранных координатах; позиции внутри Bitmap отсчитываются от их левого верхнего угла.
    private readonly Rectangle _virtualBounds;
    private readonly Rectangle[] _displayBounds;
    private readonly Action<Point> _moveCursor;
    private readonly Cursor? _pickerCursor;
    private Point _cursorPosition;
    private Point _dragStart;
    private bool _isDragging;

    public Color? PickedPointColor
    {
        get; private set;
    }

    public Bitmap? PickedRegionBitmap
    {
        get; private set;
    }

    internal Point SelectedScreenPoint => new(_cursorPosition.X + _virtualBounds.X, _cursorPosition.Y + _virtualBounds.Y);

    public ScreenOverlayForm(CaptureMode mode) : this(mode, CaptureDesktop())
    {
    }

    private ScreenOverlayForm(CaptureMode mode, (Bitmap Bitmap, Rectangle Bounds, Rectangle[] Displays) desktop)
        : this(mode, desktop.Bitmap, desktop.Bounds, desktop.Displays)
    {
    }

    // Форма получает Bitmap во владение. Тесты передают искусственный снимок,
    // чтобы проверять клавиатуру и рисование без захвата рабочего стола пользователя.
    internal ScreenOverlayForm(CaptureMode mode, Bitmap snapshot, Rectangle virtualBounds,
        Rectangle[] displayBounds, Action<Point>? moveCursor = null, Point? initialScreenPosition = null)
    {
        _mode = mode;
        _screenSnapshot = snapshot;
        _virtualBounds = virtualBounds;
        _displayBounds = displayBounds;
        _moveCursor = moveCursor ?? (point => Cursor.Position = point);
        var start = initialScreenPosition ?? Cursor.Position;
        if (!displayBounds.Any(display => display.Contains(start)))
        {
            start = displayBounds[0].Location;
        }
        _cursorPosition = new Point(start.X - virtualBounds.X, start.Y - virtualBounds.Y);
        // Координаты снимка — физические пиксели. Автомасштабирование изменило бы позицию выбора.
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = virtualBounds;
        TopMost = true;
        DoubleBuffered = true;
        if (mode == CaptureMode.Point)
        {
            using var stream = typeof(ScreenOverlayForm).Assembly.GetManifestResourceStream("ChromaTray.Picker.cur")
                ?? throw new InvalidOperationException("The point picker cursor resource is missing.");
            _pickerCursor = new Cursor(stream);
        }
        Cursor = _pickerCursor ?? Cursors.Cross;
        KeyPreview = true;
        ShowInTaskbar = false;
        AccessibleName = mode == CaptureMode.Point ? "Point picker. Arrow keys move one pixel. Enter selects. Escape cancels." : "Region picker. Drag to select. Escape cancels.";
    }

    private static (Bitmap, Rectangle, Rectangle[]) CaptureDesktop()
    {
        var bounds = SystemInformation.VirtualScreen;
        // Несжатый Bitmap сохраняет исходные RGB: здесь нет JPEG, масштабирования или фильтрации.
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            return (bitmap, bounds, Screen.AllScreens.Select(screen => screen.Bounds).ToArray());
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    // WinForms считает стрелки и Enter клавишами навигации. Перехватываем их
    // до переключения фокуса, иначе KeyDown может не получить нажатие.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key == Keys.Escape)
        {
            CancelCapture();
            return true;
        }
        if (_mode == CaptureMode.Point)
        {
            if (key == Keys.Enter)
            {
                ConfirmPoint();
                return true;
            }
            (int dx, int dy) = key switch
            {
                Keys.Left => (-1, 0),
                Keys.Right => (1, 0),
                Keys.Up => (0, -1),
                Keys.Down => (0, 1),
                _ => (0, 0)
            };
            if (dx != 0 || dy != 0)
            {
                var next = PixelNavigator.Move(SelectedScreenPoint, dx, dy, _displayBounds);
                _cursorPosition = new Point(next.X - _virtualBounds.X, next.Y - _virtualBounds.Y);
                _moveCursor(next);
                Invalidate();
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ConfirmPoint()
    {
        PickedPointColor = _screenSnapshot.GetPixel(_cursorPosition.X, _cursorPosition.Y);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelCapture()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            CancelCapture();
            return;
        }
        if (e.Button == MouseButtons.Left)
        {
            UpdatePointer(e.Location);
            if (_mode == CaptureMode.Point)
            {
                ConfirmPoint();
            }
            else
            {
                _isDragging = true;
                Capture = true;
                _dragStart = _cursorPosition;
                Invalidate();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        UpdatePointer(e.Location);
        Invalidate();
        base.OnMouseMove(e);
    }

    private void UpdatePointer(Point point) => _cursorPosition = new(
        Math.Clamp(point.X, 0, _screenSnapshot.Width - 1), Math.Clamp(point.Y, 0, _screenSnapshot.Height - 1));

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_mode == CaptureMode.Region && _isDragging && e.Button == MouseButtons.Left)
        {
            UpdatePointer(e.Location);
            _isDragging = false;
            Capture = false;
            var selection = GetNormalizedRect(_dragStart, _cursorPosition);
            selection.Intersect(new Rectangle(Point.Empty, _screenSnapshot.Size));
            if (selection.Width > 2 && selection.Height > 2)
            {
                PickedRegionBitmap = _screenSnapshot.Clone(selection, PixelFormat.Format32bppArgb);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                Invalidate();
            }
        }
        base.OnMouseUp(e);
    }

    private static Rectangle GetNormalizedRect(Point start, Point end) =>
        new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screenSnapshot.Dispose();
            PickedRegionBitmap?.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
        {
            _pickerCursor?.Dispose();
        }
    }
}
