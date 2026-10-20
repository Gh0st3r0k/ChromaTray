using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ChromaTray.Services;

public static class PaletteAnalyzer
{
    public static PaletteAnalysisResult AnalyzeBitmap(Bitmap bmp, bool groupSimilar, int maxResults = 500,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bmp);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        cancellationToken.ThrowIfCancellationRequested();
        var timer = Stopwatch.StartNew();
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

        // Convert imported bitmaps when necessary; screenshots are already 32bpp ARGB.
        using var converted = bmp.PixelFormat == PixelFormat.Format32bppArgb
            ? null : bmp.Clone(rect, PixelFormat.Format32bppArgb);
        var source = converted ?? bmp;
        var data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var histogram = new PaletteHistogram(groupSimilar);
        try
        {
            // Scan0 + y * Stride supports positive and negative row strides.
            byte[] row = new byte[checked(bmp.Width * 4)];
            for (int y = 0; y < bmp.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Marshal.Copy(IntPtr.Add(data.Scan0, checked(y * data.Stride)), row, 0, row.Length);
                histogram.AddBgraRow(row);
            }
        }
        finally
        {
            source.UnlockBits(data);
        }

        var result = histogram.Complete(maxResults);
        return result with { ElapsedMilliseconds = timer.ElapsedMilliseconds };
    }
}
