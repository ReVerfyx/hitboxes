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
/// </summary>
public static class ScreenshotHarness
{
    public static readonly List<VersionEntry> SampleVersions = new()
    {
        new VersionEntry { Id = "1.20.4", Type = "release", Url = "", ReleaseTime = DateTimeOffset.UtcNow },
        new VersionEntry { Id = "1.16.5", Type = "release", Url = "", ReleaseTime = DateTimeOffset.UtcNow.AddYears(-4) },
    };

    public static void Run(string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        string rootDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HitboxesLauncher");
        var instanceService = new InstanceService(rootDir);
        var settings = new LauncherSettings();

        var vanillaInstance = new Instance { Name = "Дом", McVersion = "1.20.4", Loader = ModLoader.Vanilla };
        var fabricInstance = new Instance { Name = "С модами", McVersion = "1.16.5", Loader = ModLoader.Fabric };
        instanceService.Save(vanillaInstance);
        instanceService.Save(fabricInstance);

        string modsDir = instanceService.GetModsDir(fabricInstance);
        File.WriteAllText(Path.Combine(modsDir, "sodium-fabric-0.4.10+1.16.5.jar"), string.Empty);
        File.WriteAllText(Path.Combine(modsDir, "lithium-fabric-mc1.16.5-0.6.4.jar"), string.Empty);

        CaptureWindow(() =>
        {
            var window = new MainWindow();
            return window;
        }, Path.Combine(outputDir, "01-main.png"));

        CaptureWindow(() => new SettingsWindow(settings) { Owner = null },
            Path.Combine(outputDir, "02-settings.png"));

        CaptureWindow(() => new NewInstanceWindow(new MinecraftVersionService(), instanceService),
            Path.Combine(outputDir, "03-new-instance.png"));

        CaptureWindow(() =>
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
    }

    private static void CaptureWindow(Func<Window> factory, string outputPath)
    {
        var window = factory();
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.Top = -10000;
        window.ShowInTaskbar = false;
        window.Show();

        // Let Loaded fire, entrance animations settle, and any
        // fast-path-completed async continuations resume.
        PumpFor(TimeSpan.FromMilliseconds(900));

        window.UpdateLayout();
        Capture(window, outputPath);
        window.Close();

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
