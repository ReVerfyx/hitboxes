using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;

namespace Hitboxes.Launcher;

public partial class MainWindow : Window
{
    private readonly string _rootDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HitboxesLauncher");

    private readonly ThemeService _themeService = new();
    private readonly MinecraftVersionService _versionService = new();
    private readonly FabricInstallerService _fabricService = new();
    private readonly SettingsService _settingsService;
    private readonly InstanceService _instanceService;
    private readonly MainMenuMusicService _musicService;

    private LauncherSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = new SettingsService(_rootDir);
        _instanceService = new InstanceService(_rootDir);
        _musicService = new MainMenuMusicService(_rootDir);
        _settings = _settingsService.Load();

        _themeService.RainAutoDetectEnabled = _settings.RainAutoDetectEnabled;
        _themeService.WeatherApiKey = _settings.WeatherApiKey;
        _themeService.WeatherCity = _settings.WeatherCity;
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
            RefreshInstances();

            _musicService.Volume = (float)_settings.MainMenuMusicVolume;
            if (_settings.MainMenuMusicEnabled)
            {
                _musicService.Play();
            }
        };
        Closed += (_, _) => _musicService.Dispose();
    }

    private void RefreshInstances()
    {
        var instances = _instanceService.LoadAll().Select(i => new InstanceViewModel(i)).ToList();
        InstancesList.ItemsSource = instances;
    }

    private void NewInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewInstanceWindow(_versionService, _instanceService) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            RefreshInstances();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Result;
            _settingsService.Save(_settings);

            _themeService.RainAutoDetectEnabled = _settings.RainAutoDetectEnabled;
            _themeService.WeatherApiKey = _settings.WeatherApiKey;
            _themeService.WeatherCity = _settings.WeatherCity;

            _musicService.Volume = (float)_settings.MainMenuMusicVolume;
            if (_settings.MainMenuMusicEnabled && !_musicService.IsPlaying)
            {
                _musicService.Play();
            }
            else if (!_settings.MainMenuMusicEnabled)
            {
                _musicService.Stop();
            }
        }
    }

    private void EditInstance_Click(object sender, RoutedEventArgs e)
    {
        var vm = (InstanceViewModel)((FrameworkElement)sender).Tag;
        var dialog = new InstanceSettingsWindow(vm.Instance, _instanceService, _settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            RefreshInstances();
        }
    }

    private void DeleteInstance_Click(object sender, RoutedEventArgs e)
    {
        var vm = (InstanceViewModel)((FrameworkElement)sender).Tag;
        var result = MessageBox.Show($"Удалить сборку «{vm.Name}»? Файлы мира будут удалены безвозвратно.",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _instanceService.Delete(vm.Instance);
            RefreshInstances();
        }
    }

    private async void PlayInstance_Click(object sender, RoutedEventArgs e)
    {
        var vm = (InstanceViewModel)((FrameworkElement)sender).Tag;
        string username = UsernameBox.Text.Trim();
        if (!AuthService.IsValidUsername(username))
        {
            StatusText.Text = "Никнейм: 3–16 символов, латиница/цифры/подчёркивание.";
            return;
        }

        ((Button)sender).IsEnabled = false;
        var progress = new Progress<string>(msg => StatusText.Text = msg);

        try
        {
            var instance = vm.Instance;
            var vanillaEntry = (await _versionService.GetSupportedReleasesAsync())
                .FirstOrDefault(v => v.Id == instance.McVersion)
                ?? throw new InvalidOperationException($"Версия {instance.McVersion} недоступна в манифесте Mojang.");

            var vanillaDetail = await _versionService.GetVersionDetailAsync(vanillaEntry.Url);

            VersionDetail launchDetail = vanillaDetail;
            if (instance.Loader == ModLoader.Fabric)
            {
                string loaderVersion = instance.FabricLoaderVersion
                    ?? await _fabricService.GetLatestStableLoaderVersionAsync(instance.McVersion)
                    ?? throw new InvalidOperationException("Не удалось получить версию Fabric Loader.");
                launchDetail = await _fabricService.GetMergedVersionDetailAsync(vanillaDetail, instance.McVersion, loaderVersion);
            }

            var installer = new GameInstaller(_rootDir);
            var installed = await installer.EnsureInstalledAsync(launchDetail, progress);

            var profile = new Profile { Username = username };
            var launcher = new GameLauncher(_rootDir);
            string gameDir = _instanceService.GetGameDir(instance);
            _instanceService.GetModsDir(instance); // ensure it exists before Fabric Loader scans it
            launcher.Launch(installed, profile, instance, _settings, gameDir);

            instance.LastPlayedAt = DateTimeOffset.UtcNow;
            _instanceService.Save(instance);

            StatusText.Text = $"Запущено: {instance.Name} ({launchDetail.Id}) как {username}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка запуска: {ex.Message}";
        }
        finally
        {
            ((Button)sender).IsEnabled = true;
        }
    }
}
