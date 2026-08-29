using System.Windows;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

public partial class App : Application
{
    /// <summary>
    /// True when launched as `HitboxesLauncher.exe --capture-screenshots &lt;dir&gt;`.
    /// Windows check this to substitute canned data for real network calls
    /// (Mojang version manifest, Modrinth search) so the CI screenshot job
    /// is fast and doesn't depend on external services being reachable.
    /// </summary>
    public static bool ScreenshotMode { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register the shared animatable theme brushes before any window
        // is created so its DynamicResource bindings resolve immediately
        // instead of flashing unstyled.
        ThemeResources.Register(Resources);

        int captureIndex = Array.IndexOf(e.Args, "--capture-screenshots");
        if (captureIndex >= 0 && captureIndex + 1 < e.Args.Length)
        {
            ScreenshotMode = true;
            ScreenshotHarness.Run(e.Args[captureIndex + 1]);
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Closed += (_, _) => Shutdown();
        mainWindow.Show();
    }
}
