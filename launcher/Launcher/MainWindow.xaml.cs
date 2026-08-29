using System.IO;
using System.Windows;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;

namespace Hitboxes.Launcher;

public partial class MainWindow : Window
{
    private readonly ThemeService _themeService = new();
    private readonly MinecraftVersionService _versionService = new();
    private readonly string _rootDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HitboxesLauncher");

    public MainWindow()
    {
        InitializeComponent();

        _themeService.ThemeChanged += (_, theme) => Dispatcher.Invoke(() => ThemeLabel.Text = theme switch
        {
            AppTheme.Day => "  ·  день",
            AppTheme.Night => "  ·  ночь",
            AppTheme.Rain => "  ·  дождь",
            _ => string.Empty
        });

        Loaded += async (_, _) =>
        {
            await _themeService.StartAsync();
            await LoadVersionsAsync();
        };
    }

    private async Task LoadVersionsAsync()
    {
        StatusText.Text = "Загрузка списка версий...";
        try
        {
            var versions = await _versionService.GetSupportedReleasesAsync();
            VersionComboBox.ItemsSource = versions;
            if (versions.Count > 0)
            {
                VersionComboBox.SelectedIndex = 0;
            }
            StatusText.Text = $"Доступно версий: {versions.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Не удалось получить список версий: {ex.Message}";
        }
    }

    private void ManualRainCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _themeService.SetManualRain(ManualRainCheckBox.IsChecked == true);
    }

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (VersionComboBox.SelectedItem is not VersionEntry selected)
        {
            StatusText.Text = "Выберите версию.";
            return;
        }

        string username = UsernameBox.Text.Trim();
        if (!AuthService.IsValidUsername(username))
        {
            StatusText.Text = "Никнейм: 3–16 символов, латиница/цифры/подчёркивание.";
            return;
        }

        LaunchButton.IsEnabled = false;
        var progress = new Progress<string>(msg => StatusText.Text = msg);

        try
        {
            var detail = await _versionService.GetVersionDetailAsync(selected.Url);

            var installer = new GameInstaller(_rootDir);
            var installed = await installer.EnsureInstalledAsync(detail, progress);

            var profile = new Profile { Username = username };
            var launcher = new GameLauncher(_rootDir);
            launcher.Launch(installed, profile);

            StatusText.Text = $"Запущено: {detail.Id} как {username}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка запуска: {ex.Message}";
        }
        finally
        {
            LaunchButton.IsEnabled = true;
        }
    }
}
