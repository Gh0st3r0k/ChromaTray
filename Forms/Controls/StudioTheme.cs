using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChromaTray.Forms.Controls;

/// <summary>
/// Shared colors, fonts, and geometry helpers. Layout dimensions use a 96-DPI baseline.
/// </summary>
internal static class StudioTheme
{
    private const int ImmersiveDarkModeAttribute = 20;
    private const int WindowCornerPreferenceAttribute = 33;

    // Base interface palette: #161D6F / #0B2F9F / #98DED9 / #C7FFD8.
    public static readonly Color Canvas = Color.FromArgb(22, 29, 111);
    public static readonly Color Surface = Color.FromArgb(25, 40, 121);
    public static readonly Color Raised = Color.FromArgb(11, 47, 159);
    public static readonly Color Border = Color.FromArgb(65, 91, 168);
    public static readonly Color Text = Color.FromArgb(240, 255, 249);
    public static readonly Color Muted = Color.FromArgb(165, 196, 222);
    public static readonly Color Accent = Color.FromArgb(152, 222, 217);
    public static readonly Color Mint = Color.FromArgb(199, 255, 216);
    public static readonly Color Danger = Color.FromArgb(255, 184, 184);
    // Shared fonts live for the application's lifetime; individual controls must not dispose them.
    public static readonly Font Body = new("Segoe UI", 10f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font Strong = new("Segoe UI Semibold", 10f);
    public static readonly Font Heading = new("Segoe UI Semibold", 16f);
    public static readonly Font Title = new("Segoe UI Semibold", 24f);
    public static readonly Font Mono = new("Consolas", 10f);
    public static readonly Font MonoLarge = new("Consolas", 24f, FontStyle.Bold);

    // A child control may still report its previous scale during a DPI change.
    // Use the window's current DPI consistently for both painting and layout.
    public static int Dpi(Control control) => control.FindForm()?.DeviceDpi ?? control.DeviceDpi;

    public static GraphicsPath Round(RectangleF rect, float radius = 12)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        if (diameter <= 0)
        {
            return path;
        }
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Label Label(string text, Font? font = null, Color? color = null) => new()
    {
        Text = text,
        Font = font ?? Body,
        ForeColor = color ?? Text,
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent,
        AutoEllipsis = true,
        Margin = Padding.Empty
    };

    // Height >= 0 specifies a fixed logical height; -1 gives the row the remaining space.
    public static DpiStackPanel Stack(params (Control Control, int Height)[] rows)
    {
        var stack = new DpiStackPanel(rows.Select(row => row.Height).ToArray())
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = rows.Length,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < rows.Length; i++)
        {
            var (control, height) = rows[i];
            stack.RowStyles.Add(height < 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, height));
            control.Dock = DockStyle.Fill;
            stack.Controls.Add(control, 0, i);
        }
        return stack;
    }

    public static void ApplyWindowChrome(Form form)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        int dark = 1;
        _ = DwmSetWindowAttribute(form.Handle, ImmersiveDarkModeAttribute, ref dark, sizeof(int));
        int corners = 2;
        _ = DwmSetWindowAttribute(form.Handle, WindowCornerPreferenceAttribute, ref corners, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
