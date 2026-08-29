using System.Windows;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher;

public partial class SettingsWindow : Window
{
    public LauncherSettings Result { get; private set; }

    public SettingsWindow(LauncherSettings current)
    {
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
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
