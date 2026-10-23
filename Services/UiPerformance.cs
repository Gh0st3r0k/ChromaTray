using System.Diagnostics;
using System.Globalization;

namespace ChromaTray.Services;

// Opt-in local timings; never log captured colors, settings, or screen contents.
internal static class UiPerformance
{
    private static readonly string? LogPath = Environment.GetEnvironmentVariable("CHROMATRAY_PERF_LOG");

    internal static long Start() => string.IsNullOrWhiteSpace(LogPath) ? 0 : Stopwatch.GetTimestamp();

    internal static void Record(string operation, long started)
    {
        if (started == 0 || string.IsNullOrWhiteSpace(LogPath))
            return;
        double milliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        try
        {
            File.AppendAllText(LogPath, string.Create(CultureInfo.InvariantCulture,
                $"{DateTimeOffset.Now:O} pid={Environment.ProcessId} {operation}={milliseconds:F1}ms\n"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Diagnostics must never prevent opening the application.
        }
    }

    internal static void RecordWhenIdle(Form owner, string operation, long started)
    {
        if (started == 0)
            return;
        EventHandler? handler = null;
        EventHandler? closed = null;
        void Detach()
        {
            Application.Idle -= handler;
            owner.Disposed -= closed;
        }
        handler = (_, _) =>
        {
            Detach();
            if (!owner.IsDisposed)
                Record(operation, started);
        };
        closed = (_, _) => Detach();
        owner.Disposed += closed;
        Application.Idle += handler;
    }
}
