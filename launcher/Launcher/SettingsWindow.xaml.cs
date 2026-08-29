using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

public partial class SettingsWindow : Window
{
    public LauncherSettings Result { get; private set; }

    public SettingsWindow(LauncherSettings current)
    {
        ThemeResources.Register(Resources);
        InitializeComponent();
        Result = current;

        JavaPathBox.Text = current.JavaExecutable;
        MemMinBox.Text = current.DefaultMemoryMinMb.ToString();
        MemMaxBox.Text = current.DefaultMemoryMaxMb.ToString();
        JvmArgsBox.Text = current.DefaultJvmArgs;

        MusicEnabledBox.IsChecked = current.MainMenuMusicEnabled;
        MusicVolumeSlider.Value = current.MainMenuMusicVolume;

        RainAutoDetectBox.IsChecked = current.RainAutoDetectEnabled;
        WeatherApiKeyBox.Text = current.WeatherApiKey;
        WeatherCityBox.Text = current.WeatherCity;

        GlassHexBox.Text = current.GlassTintColor;
        UpdateGlassPreview();

        Loaded += (_, _) => GlassWindowHelper.Enable(this, isDialog: true);
    }

    private void GlassSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        var border = (System.Windows.Controls.Border)sender;
        if (border.Background is SolidColorBrush brush)
        {
            GlassHexBox.Text = $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
        }
    }

    private void GlassHexBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateGlassPreview();
    }

    private void UpdateGlassPreview()
    {
        if (GlassPreview is null)
        {
            return;
        }
        GlassPreview.Background = new SolidColorBrush(MainWindow.ParseGlassColor(GlassHexBox.Text));
        // Live preview: recolor the shared glass tint immediately so the
        // effect is visible behind this dialog too, without waiting for Save.
        ThemeService.ApplyGlassTint(MainWindow.ParseGlassColor(GlassHexBox.Text));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        int memMin = int.TryParse(MemMinBox.Text, out int min) ? min : Result.DefaultMemoryMinMb;
        int memMax = int.TryParse(MemMaxBox.Text, out int max) ? max : Result.DefaultMemoryMaxMb;

        Result = new LauncherSettings
        {
            JavaExecutable = string.IsNullOrWhiteSpace(JavaPathBox.Text) ? "javaw" : JavaPathBox.Text.Trim(),
            DefaultMemoryMinMb = memMin,
            DefaultMemoryMaxMb = memMax,
            DefaultJvmArgs = JvmArgsBox.Text.Trim(),
            MainMenuMusicEnabled = MusicEnabledBox.IsChecked == true,
            MainMenuMusicVolume = MusicVolumeSlider.Value,
            RainAutoDetectEnabled = RainAutoDetectBox.IsChecked == true,
            WeatherApiKey = string.IsNullOrWhiteSpace(WeatherApiKeyBox.Text) ? null : WeatherApiKeyBox.Text.Trim(),
            WeatherCity = string.IsNullOrWhiteSpace(WeatherCityBox.Text) ? "Moscow" : WeatherCityBox.Text.Trim(),
            GlassTintColor = string.IsNullOrWhiteSpace(GlassHexBox.Text) ? "#4FA8FF" : GlassHexBox.Text.Trim(),
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Revert the live preview if the user cancels without saving.
        ThemeService.ApplyGlassTint(MainWindow.ParseGlassColor(Result.GlassTintColor));
        DialogResult = false;
    }
}
