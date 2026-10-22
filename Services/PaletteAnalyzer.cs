using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ChromaTray.Services;

/// <summary>
/// Адаптер Bitmap: читает пиксели построчно и передаёт их независимому от Windows счётчику PaletteHistogram.
/// </summary>
public static class PaletteAnalyzer
{
    public static PaletteAnalysisResult AnalyzeBitmap(Bitmap bitmap, bool groupSimilar, int maxResults = 500,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        cancellationToken.ThrowIfCancellationRequested();
        var timer = Stopwatch.StartNew();
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        // Скриншоты уже имеют формат 32bpp. Другой Bitmap преобразуем, не меняя оригинал.
        using var converted = bitmap.PixelFormat == PixelFormat.Format32bppArgb
            ? null : bitmap.Clone(bounds, PixelFormat.Format32bppArgb);
        var source = converted ?? bitmap;
        var pixelData = source.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var histogram = new PaletteHistogram(groupSimilar);
        try
        {
            // Stride — шаг строки в памяти, включая выравнивание; он может быть отрицательным.
            // BGRA — порядок байтов в памяти, хотя название формата в .NET содержит ARGB.
            byte[] rowBytes = new byte[checked(bitmap.Width * 4)];
            for (int y = 0; y < bitmap.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Marshal.Copy(IntPtr.Add(pixelData.Scan0, checked(y * pixelData.Stride)), rowBytes, 0, rowBytes.Length);
                histogram.AddBgraRow(rowBytes);
            }
        }
        finally
        {
            // Освобождаем блокировку и при отмене/ошибке: иначе Bitmap нельзя использовать снова.
            source.UnlockBits(pixelData);
        }

        var result = histogram.Complete(maxResults);
        return result with
        {
            ElapsedMilliseconds = timer.ElapsedMilliseconds
        };
    }
}
