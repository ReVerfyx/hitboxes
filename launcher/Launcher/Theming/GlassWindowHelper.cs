using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Hitboxes.Launcher.Theming;

/// <summary>
/// Turns on the real OS-level frosted-glass backdrop (Mica/Acrylic, the
/// same material Windows 11's own Settings app uses) behind a window via
/// the public DWM attribute API. This only does anything on Windows 11
/// (build 22621+); on older Windows it silently no-ops and the window
/// just keeps its plain background — there is no reliable frosted-glass
/// API before that, and we don't fake it with a screenshot-blur hack.
/// </summary>
public static class GlassWindowHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_ROUND = 2;
    private const int DWMSBT_MAINWINDOW = 2; // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic-like, used for dialogs

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>
    /// Call once the window's Win32 handle exists (Loaded or
    /// SourceInitialized). <paramref name="isDialog"/> picks the
    /// Acrylic-flavoured backdrop used for transient/owned windows instead
    /// of the main-window Mica material.
    /// </summary>
    public static void Enable(Window window, bool isDialog = false)
    {
        // Window.Background stays whatever the theme's (semi-transparent)
        // gradient brush is — see ThemePalette. WPF composites it against
        // the DWM backdrop set below per-pixel, so the theme tint and the
        // blur show through together instead of one replacing the other.
        IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int corner = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

        int backdrop = isDialog ? DWMSBT_TRANSIENTWINDOW : DWMSBT_MAINWINDOW;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
    }

    public static void SetDarkTitleBar(Window window, bool dark)
    {
        IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (hwnd == IntPtr.Zero)
        {
            return;
        }
        int value = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }
}
