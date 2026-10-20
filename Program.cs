using ChromaTray.Forms;

namespace ChromaTray;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Prevent multiple instances from running simultaneously
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

        Application.Run(new MainDashboardForm());
    }
}
