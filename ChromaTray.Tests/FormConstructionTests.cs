using System.Runtime.ExceptionServices;
using ChromaTray.Forms;
using ChromaTray.Models;
using Xunit;

namespace ChromaTray.Tests;

[Collection("Desktop UI")]
public class FormConstructionTests
{
    [Fact]
    public void DashboardAndSettings_CanCreateWindowsHandlesAndDispose()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dashboard = new MainDashboardForm();
                using var settings = new SettingsForm(new AppSettings());
                Assert.NotEqual(IntPtr.Zero, dashboard.Handle);
                Assert.NotEqual(IntPtr.Zero, settings.Handle);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "Creating the desktop forms timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
