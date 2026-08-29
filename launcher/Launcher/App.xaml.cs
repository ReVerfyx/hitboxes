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

        // Deliberately NOT registered into Application.Resources: WPF
        // auto-freezes Freezable resources (brushes included) added to the
        // Application-level dictionary, which would make them impossible
        // to animate ("sealed or frozen" InvalidOperationException). Each
        // window registers the same shared brush instances into its own
        // (non-frozen) Resources instead — see ThemeResources.Register
        // calls in MainWindow/SettingsWindow/NewInstanceWindow/InstanceSettingsWindow.

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
