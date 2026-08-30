using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

public partial class SettingsWindow : Window
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    public LauncherSettings Result { get; private set; }

    public SettingsWindow(LauncherSettings current)
    {
        ThemeResources.Register(Resources);
        InitializeComponent();
        Result = current;

        JavaPathBox.Text = current.JavaExecutable;
        JvmArgsBox.Text = current.DefaultJvmArgs;
        var memoryOptionsGb = BuildMemoryOptionsGb();
        MemoryBox.ItemsSource = memoryOptionsGb;
        int selectedGb = Math.Max(4, (int)Math.Ceiling(current.DefaultMemoryMaxMb / 1024.0));
        MemoryBox.SelectedItem = memoryOptionsGb.Contains(selectedGb) ? selectedGb : memoryOptionsGb.FirstOrDefault();

        MusicEnabledBox.IsChecked = current.MainMenuMusicEnabled;
        MusicVolumeSlider.Value = current.MainMenuMusicVolume;
        RainAutoDetectBox.IsChecked = current.RainAutoDetectEnabled;
        WeatherApiKeyBox.Text = current.WeatherApiKey;
        WeatherCityBox.Text = current.WeatherCity;
        GlassHexBox.Text = current.GlassTintColor;
        UpdateGlassPreview();

        Loaded += (_, _) => UiAnimations.FadeIn(this);
    }

    private static List<int> BuildMemoryOptionsGb()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        ulong totalBytes = GlobalMemoryStatusEx(ref status) ? status.TotalPhys : 16UL * 1024 * 1024 * 1024;
        int maxGb = Math.Max(4, (int)(totalBytes / (1024UL * 1024 * 1024)));
        return Enumerable.Range(4, maxGb - 3).ToList();
    }

    private void GlassSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        var border = (System.Windows.Controls.Border)sender;
        if (border.Background is SolidColorBrush brush)
            GlassHexBox.Text = $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    }

    private void GlassHexBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateGlassPreview();

    private void UpdateGlassPreview()
    {
        if (GlassPreview is null) return;
        GlassPreview.Background = new SolidColorBrush(MainWindow.ParseGlassColor(GlassHexBox.Text));
        ThemeService.ApplyGlassTint(MainWindow.ParseGlassColor(GlassHexBox.Text));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        int memoryGb = MemoryBox.SelectedItem is int value ? value : 4;

        Result = new LauncherSettings
        {
            JavaExecutable = string.IsNullOrWhiteSpace(JavaPathBox.Text) ? "javaw" : JavaPathBox.Text.Trim(),
            DefaultMemoryMinMb = 512,
            DefaultMemoryMaxMb = memoryGb * 1024,
            DefaultJvmArgs = JvmArgsBox.Text.Trim(),
            MainMenuMusicEnabled = MusicEnabledBox.IsChecked == true,
            MainMenuMusicVolume = MusicVolumeSlider.Value,
            RainAutoDetectEnabled = RainAutoDetectBox.IsChecked == true,
            WeatherApiKey = string.IsNullOrWhiteSpace(WeatherApiKeyBox.Text) ? null : WeatherApiKeyBox.Text.Trim(),
            WeatherCity = string.IsNullOrWhiteSpace(WeatherCityBox.Text) ? "Moscow" : WeatherCityBox.Text.Trim(),
            GlassTintColor = string.IsNullOrWhiteSpace(GlassHexBox.Text) ? "#8B7CFF" : GlassHexBox.Text.Trim(),
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyGlassTint(MainWindow.ParseGlassColor(Result.GlassTintColor));
        DialogResult = false;
    }
}
