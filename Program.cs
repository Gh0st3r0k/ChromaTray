using ChromaTray.Forms;

namespace ChromaTray;

/// <summary>
/// Точка входа: настраивает WinForms, запрещает второй экземпляр и запускает главное окно.
/// </summary>
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Иначе два экземпляра конфликтовали бы за глобальные хоткеи и значки трея.
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
