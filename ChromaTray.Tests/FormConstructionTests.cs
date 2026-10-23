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
    public void Dashboard_ReopeningFromMinimizedRestoresUsableLayout()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            var originalSize = dashboard.ClientSize;
            var formats = dashboard.Controls.Find("ColorFormats", true).Single();
            var rows = formats.Controls.Cast<System.Windows.Forms.Control>().ToArray();
            var reopen = typeof(MainDashboardForm).GetMethod("ShowMainWindow",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            for (int i = 0; i < 3; i++)
            {
                dashboard.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                dashboard.Hide();
                reopen.Invoke(dashboard, null);
                System.Windows.Forms.Application.DoEvents();
                Assert.True(dashboard.Visible);
                Assert.Equal(System.Windows.Forms.FormWindowState.Normal, dashboard.WindowState);
                Assert.Equal(originalSize, dashboard.ClientSize);
                Assert.Equal(rows, formats.Controls.Cast<System.Windows.Forms.Control>().ToArray());
                Assert.All(rows, row => Assert.True(row.Width > 0 && row.Height > 0));
            }
        });
    }

    [Fact]
    public void Dashboard_UnchangedDpiMetricsReuseFormatControlsButChangedFormatsRebuild()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            var settings = new AppSettings();
            using var dashboard = new MainDashboardForm(settings);
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            var formats = dashboard.Controls.Find("ColorFormats", true).Single();
            var rows = formats.Controls.Cast<System.Windows.Forms.Control>().ToArray();
            var metrics = typeof(MainDashboardForm).GetMethod("ApplyDpiMetrics",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            for (int i = 0; i < 3; i++)
                metrics.Invoke(dashboard, null);
            Assert.Equal(rows, formats.Controls.Cast<System.Windows.Forms.Control>().ToArray());
            Assert.All(rows, row => Assert.False(row.IsDisposed));

            settings.ActiveFormats = [ColorFormat.Rgb];
            metrics.Invoke(dashboard, null);
            Assert.All(rows, row => Assert.True(row.IsDisposed));
            Assert.Single(formats.Controls.Cast<System.Windows.Forms.Control>());
            Assert.Single(formats.Controls.Find("ValueRgb", true));
        });
    }

    [Fact]
    public void Dashboard_TaskbarMinimizeDoesNotReconfigureInspectorRows()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            var inspector = (System.Windows.Forms.TableLayoutPanel)dashboard.Controls.Find("InspectorLayout", true).Single();
            var rows = inspector.RowStyles.Cast<System.Windows.Forms.RowStyle>().Select(row => row.Height).ToArray();
            var originalSize = dashboard.ClientSize;
            for (int i = 0; i < 3; i++)
            {
                dashboard.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                System.Windows.Forms.Application.DoEvents();
                Assert.Equal(rows, inspector.RowStyles.Cast<System.Windows.Forms.RowStyle>().Select(row => row.Height).ToArray());
                dashboard.WindowState = System.Windows.Forms.FormWindowState.Normal;
                System.Windows.Forms.Application.DoEvents();
                Assert.Equal(originalSize, dashboard.ClientSize);
                Assert.Equal(rows, inspector.RowStyles.Cast<System.Windows.Forms.RowStyle>().Select(row => row.Height).ToArray());
            }
        });
    }

    [Fact]
    public void Dashboard_UnchangedWorkspaceDoesNotScheduleMoreLayout()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            var workspace = dashboard.Controls.Find("PaletteWorkspace", true).Single();
            var inspector = dashboard.Controls.Find("InspectorLayout", true).Single();
            var fit = typeof(MainDashboardForm).GetMethod("FitWorkspaceColumns",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            int layouts = 0;
            workspace.Layout += (_, _) => layouts++;
            inspector.Layout += (_, _) => layouts++;
            for (int i = 0; i < 5; i++)
                fit.Invoke(dashboard, null);
            Assert.Equal(0, layouts);
        });
    }

    [Fact]
    public void Dashboard_LoadCompletesLayoutBeforeShownAndRestoreKeepsColorValues()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            System.Windows.Forms.Control[]? initialRows = null;
            string[]? initialValues = null;
            dashboard.Shown += (_, _) =>
            {
                var formats = dashboard.Controls.Find("ColorFormats", true).Single();
                initialRows = formats.Controls.Cast<System.Windows.Forms.Control>().ToArray();
                initialValues = DescendantTextBoxes(formats).Select(box => box.Text).ToArray();
                Assert.NotEmpty(initialValues);
                Assert.All(initialValues, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            };
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            dashboard.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            dashboard.WindowState = System.Windows.Forms.FormWindowState.Normal;
            System.Windows.Forms.Application.DoEvents();
            var restored = dashboard.Controls.Find("ColorFormats", true).Single();
            Assert.Equal(initialRows, restored.Controls.Cast<System.Windows.Forms.Control>().ToArray());
            Assert.Equal(initialValues, DescendantTextBoxes(restored).Select(box => box.Text).ToArray());
        });
    }

    private static IEnumerable<System.Windows.Forms.TextBox> DescendantTextBoxes(System.Windows.Forms.Control root)
    {
        foreach (System.Windows.Forms.Control child in root.Controls)
        {
            if (child is System.Windows.Forms.TextBox box)
                yield return box;
            foreach (var descendant in DescendantTextBoxes(child))
                yield return descendant;
        }
    }

    [Fact]
    public void Dashboard_InspectorPaintingDoesNotRepaintItsParentCard()
    {
        OverlayKeyboardTests.OnSta(() =>
        {
            using var dashboard = new MainDashboardForm(new AppSettings());
            dashboard.Show();
            System.Windows.Forms.Application.DoEvents();
            var inspector = dashboard.Controls.Find("InspectorLayout", true).Single();
            int parentPaints = 0;
            inspector.Parent!.Paint += (_, _) => parentPaints++;
            using var bitmap = new System.Drawing.Bitmap(inspector.Width, inspector.Height);
            inspector.DrawToBitmap(bitmap, inspector.ClientRectangle);
            Assert.Equal(0, parentPaints);
            Assert.Equal(255, inspector.BackColor.A);
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
