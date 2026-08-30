using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;

namespace Hitboxes.Launcher.Theming;

/// <summary>
/// CI-only headless capture mode: builds each window with sample data
/// (no live network calls — see <see cref="App.ScreenshotMode"/>), lets it
/// lay out and finish its entrance animation, then renders it to a PNG via
/// <see cref="RenderTargetBitmap"/>. This is how "build it and show me a
/// screenshot" actually gets satisfied — there's no Windows/GUI environment
/// available to run the app in interactively outside of the real Windows
/// runner GitHub Actions provides, so this mode runs there and the PNGs
/// come back as a build artifact.
///
/// Writes a harness.log alongside the PNGs regardless of outcome — the
/// first run of this produced a clean exit (code 0) but zero PNG files,
/// with no exception anywhere, so the log exists specifically to pin down
/// what actually happened step by step on the next attempt.
/// </summary>
public static class ScreenshotHarness
{
    public static readonly List<VersionEntry> SampleVersions = new()
    {
        new VersionEntry { Id = "1.20.4", Type = "release", Url = "", ReleaseTime = DateTimeOffset.UtcNow },
        new VersionEntry { Id = "1.16.5", Type = "release", Url = "", ReleaseTime = DateTimeOffset.UtcNow.AddYears(-4) },
    };

    private static StreamWriter? _log;

    public static void Run(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        _log = new StreamWriter(Path.Combine(outputDir, "harness.log")) { AutoFlush = true };

        try
        {
            RunCore(outputDir);
            Log("Run() completed normally.");
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex);
            throw;
        }
        finally
        {
            _log?.Dispose();
        }
    }

    /// <summary>Public so other code (e.g. MainWindow's Loaded handler) can add step-by-step
    /// breadcrumbs to the same harness.log while <see cref="App.ScreenshotMode"/> is active.</summary>
    public static void Log(string message)
    {
        _log?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    private static void RunCore(string outputDir)
    {
        Log($"outputDir = {outputDir}, exists = {Directory.Exists(outputDir)}");

        string rootDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HitboxesLauncher");
        var instanceService = new InstanceService(rootDir);
        var settings = new LauncherSettings();

        var vanillaInstance = new Instance { Name = "Дом", McVersion = "1.20.4", Loader = ModLoader.Vanilla };
        var fabricInstance = new Instance { Name = "С модами", McVersion = "1.16.5", Loader = ModLoader.Fabric };
        instanceService.Save(vanillaInstance);
        instanceService.Save(fabricInstance);
        Log("Sample instances saved.");

        string modsDir = instanceService.GetModsDir(fabricInstance);
        File.WriteAllText(Path.Combine(modsDir, "sodium-fabric-0.4.10+1.16.5.jar"), string.Empty);
        File.WriteAllText(Path.Combine(modsDir, "lithium-fabric-mc1.16.5-0.6.4.jar"), string.Empty);

        TryCaptureWindow("FirstRunWindow", () => new FirstRunWindow(rootDir), Path.Combine(outputDir, "00-first-run.png"));

        TryCaptureWindow("MainWindow", () => new MainWindow(), Path.Combine(outputDir, "01-main.png"));

        TryCaptureWindow("MainWindow-Settings", () => new MainWindow { ScreenshotInitialView = "Settings" },
            Path.Combine(outputDir, "02-settings.png"));

        TryCaptureWindow("MainWindow-Instances", () => new MainWindow { ScreenshotInitialView = "Instances" },
            Path.Combine(outputDir, "02b-instances.png"));

        TryCaptureWindow("NewInstanceWindow", () => new NewInstanceWindow(new MinecraftVersionService(), instanceService),
            Path.Combine(outputDir, "03-new-instance.png"));

        TryCaptureWindow("InstanceSettingsWindow", () =>
        {
            var window = new InstanceSettingsWindow(fabricInstance, instanceService, settings);
            window.Loaded += (_, _) =>
            {
                window.ModSearchBox.Text = "sodium";
                window.SearchResultsList.ItemsSource = new List<ModrinthProject>
                {
                    new() { Title = "Sodium", Description = "A modern rendering engine for Minecraft", Downloads = 42_000_000 },
                    new() { Title = "Lithium", Description = "No-compromises game logic/server optimization mod", Downloads = 18_000_000 },
                    new() { Title = "Iris Shaders", Description = "A modern shaders mod for Minecraft", Downloads = 15_000_000 },
                };
            };
            return window;
        }, Path.Combine(outputDir, "04-instance-settings.png"));

        Log($"Files now in outputDir: {string.Join(", ", Directory.GetFiles(outputDir))}");
    }

    /// <summary>Isolates one window's capture so a fatal failure on it doesn't stop the rest from being attempted.</summary>
    private static void TryCaptureWindow(string label, Func<Window> factory, string outputPath)
    {
        try
        {
            CaptureWindow(label, factory, outputPath);
        }
        catch (Exception ex)
        {
            Log($"{label}: TOP-LEVEL FAILURE: {ex}");
        }
    }

    private static void CaptureWindow(string label, Func<Window> factory, string outputPath)
    {
        Log($"{label}: constructing...");
        var window = factory();
        Log($"{label}: constructed. Showing...");

        window.Show();
        Log($"{label}: Show() returned. IsVisible={window.IsVisible}, ActualWidth={window.ActualWidth}, ActualHeight={window.ActualHeight}");

        // Let Loaded fire, entrance animations settle, and any
        // fast-path-completed async continuations resume. Pumped in short
        // slices with a log line between each so a hang/crash inside the
        // pump itself is visible (vs. one that only shows up afterward).
        for (int i = 0; i < 6; i++)
        {
            PumpFor(TimeSpan.FromMilliseconds(200));
            Log($"{label}: pump slice {i + 1}/6 done.");
        }

        Log($"{label}: calling UpdateLayout()...");
        window.UpdateLayout();
        Log($"{label}: after pump+UpdateLayout. ActualWidth={window.ActualWidth}, ActualHeight={window.ActualHeight}");

        try
        {
            Log($"{label}: calling Capture()...");
            Capture(window, outputPath);
            Log($"{label}: captured to {outputPath}. Exists={File.Exists(outputPath)}, Size={(File.Exists(outputPath) ? new FileInfo(outputPath).Length : -1)}");
        }
        catch (Exception ex)
        {
            Log($"{label}: CAPTURE FAILED: {ex}");
        }

        Log($"{label}: calling Close()...");
        window.Close();
        Log($"{label}: closed.");

        // Let the Closed handlers (e.g. main menu music teardown) run.
        PumpFor(TimeSpan.FromMilliseconds(200));
    }

    private static void Capture(Window window, string outputPath)
    {
        int width = Math.Max(1, (int)window.ActualWidth);
        int height = Math.Max(1, (int)window.ActualHeight);

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        stream.Flush();
    }

    /// <summary>Runs a nested Dispatcher message loop for a fixed duration — the manual equivalent of Application.Run() while we're not inside one.</summary>
    private static void PumpFor(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = duration,
        };
        timer.Tick += (_, _) =>
        {
            frame.Continue = false;
            timer.Stop();
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
}
