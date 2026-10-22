using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ChromaTray.Services;

/// <summary>
/// Reads bitmap rows and forwards their pixels to the platform-independent palette histogram.
/// </summary>
public static class PaletteAnalyzer
{
    /// <summary>
    /// Scans a bitmap and returns its most frequent RGB colors.
    /// </summary>
    /// <param name="bitmap">The source bitmap. The caller retains ownership.</param>
    /// <param name="groupSimilar">Whether to combine shades into step-eight RGB buckets represented by original colors.</param>
    /// <param name="maxResults">Maximum returned entries. Must be positive; defaults to 500.</param>
    /// <param name="cancellationToken">Cancels processing between bitmap rows.</param>
    /// <returns>Pixel totals, the pre-limit number of colors or groups, and a frequency-sorted palette.</returns>
    /// <remarks>This method is synchronous. The dashboard runs it on a background task. Percentages use the full region even when only the top entries are returned.</remarks>
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
