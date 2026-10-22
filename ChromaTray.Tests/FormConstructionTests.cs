using System.Runtime.ExceptionServices;
using ChromaTray.Forms;
using ChromaTray.Models;
using Xunit;

namespace ChromaTray.Tests;

[Collection("Desktop UI")]
public class FormConstructionTests
{
    [Fact]
    public void Dashboard_ReopeningReusesFormatControlsAndPreservesMaximizedState()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            var formats = dashboard.Controls.Find("ColorFormats", true).Single();
            var rows = formats.Controls.Cast<System.Windows.Forms.Control>().ToArray();
            var reopen = typeof(MainDashboardForm).GetMethod("ShowMainWindow",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            dashboard.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            for (int i = 0; i < 3; i++)
            {
                dashboard.Hide();
                reopen.Invoke(dashboard, null);
                System.Windows.Forms.Application.DoEvents();
                Assert.Equal(System.Windows.Forms.FormWindowState.Maximized, dashboard.WindowState);
                Assert.Equal(rows, formats.Controls.Cast<System.Windows.Forms.Control>().ToArray());
                Assert.All(rows, row => Assert.False(row.IsDisposed));
            }
        });
    }

    [Fact]
    public void DashboardAndSettings_CanCreateWindowsHandlesAndDispose()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.ThrowException, threadScope: true);
                using var dashboard = new MainDashboardForm();
                using var settings = new SettingsForm(new AppSettings());
                Assert.NotEqual(IntPtr.Zero, dashboard.Handle);
                Assert.NotEqual(IntPtr.Zero, settings.Handle);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "Creating the desktop forms timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
