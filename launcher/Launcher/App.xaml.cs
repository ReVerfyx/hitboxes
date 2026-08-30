using System.IO;
using System.Windows;
using Hitboxes.Launcher.Services;
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
        // calls in MainWindow/NewInstanceWindow/InstanceSettingsWindow/FirstRunWindow.

        int captureIndex = Array.IndexOf(e.Args, "--capture-screenshots");
        if (captureIndex >= 0 && captureIndex + 1 < e.Args.Length)
        {
            ScreenshotMode = true;
            string outputDir = e.Args[captureIndex + 1];

            // The crash moves around between runs (once right after window
            // construction, once during Show() itself) with nothing ever
            // logged — consistent with an unreliable GPU/D3D stack on this
            // CI VM rather than one specific line of our own code. Force
            // WPF's software rasterizer instead of hardware-accelerated
            // rendering, the standard fix for WPF apps on headless/RDP/CI
            // machines with no dependable graphics driver.
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            // The previous CI attempt's harness.log stopped mid-construction
            // of the first window with no exception logged at all — pointing
            // at something that crashes past normal try/catch. Wire up every
            // "last resort" exception hook so whatever it is gets written to
            // disk instead of vanishing, and mark Dispatcher exceptions
            // handled so a single bad window doesn't take the whole process
            // down before later windows get a chance.
            System.IO.Directory.CreateDirectory(outputDir);
            string crashLogPath = System.IO.Path.Combine(outputDir, "crash.log");
            void LogCrash(string source, object? content) =>
                System.IO.File.AppendAllText(crashLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {source}\n{content}\n\n");

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                LogCrash($"AppDomain.UnhandledException (IsTerminating={args.IsTerminating})", args.ExceptionObject);
            DispatcherUnhandledException += (_, args) =>
            {
                LogCrash("Application.DispatcherUnhandledException", args.Exception);
                args.Handled = true;
            };

            ScreenshotHarness.Run(outputDir);
            Shutdown();
            return;
        }

        string rootDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HitboxesLauncher");

        DevLog.Initialize(rootDir);
        DevLog.Log($"Launcher started. Version {typeof(App).Assembly.GetName().Version}.");

        // Without these, a real user's crash just vanishes (or shows the
        // default WPF "unhandled exception" dialog with no way to copy the
        // detail) — the screenshot-mode-only crash.log above doesn't cover
        // a normal run at all. Log the full exception (not just .Message,
        // which is what StatusText shows elsewhere) so Settings →
        // Разработчик has something a user can actually copy and send.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            DevLog.Log($"FATAL (AppDomain.UnhandledException, IsTerminating={args.IsTerminating}):\n{args.ExceptionObject}");
        DispatcherUnhandledException += (_, args) =>
        {
            DevLog.Log($"UNHANDLED (Dispatcher):\n{args.Exception}");
            MessageBox.Show(
                $"Необработанная ошибка:\n{args.Exception.Message}\n\nПолный текст сохранён в Настройки → Разработчик.",
                "ReVerfyx Client Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            DevLog.Log($"UNOBSERVED TASK EXCEPTION:\n{args.Exception}");
            args.SetObserved();
        };

        bool isFirstRun = !File.Exists(Path.Combine(rootDir, "settings.json"));
        if (isFirstRun)
        {
            var firstRun = new FirstRunWindow(rootDir);
            if (firstRun.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Closed += (_, _) => Shutdown();
        mainWindow.Show();
    }
}
