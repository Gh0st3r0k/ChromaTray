using ChromaTray.Forms;
using ChromaTray.Services;

namespace ChromaTray;

/// <summary>
/// Initializes WinForms, prevents duplicate instances, and opens the main dashboard.
/// </summary>
internal static class Program
{
    [STAThread]
    static void Main()
    {
        long startupStarted = UiPerformance.Start();
        ApplicationConfiguration.Initialize();

        // Multiple instances would compete for global hotkeys and create duplicate tray icons.
        using var mutex = new Mutex(true, "ChromaTray_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "ChromaTray is already running in the system tray.",
                "ChromaTray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dashboard = new MainDashboardForm();
        dashboard.Shown += (_, _) => UiPerformance.RecordWhenIdle(dashboard, "startup-to-idle", startupStarted);
        Application.Run(dashboard);
    }
}
