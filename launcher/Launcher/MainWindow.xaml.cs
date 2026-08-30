using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;
using Hitboxes.Launcher.Theming;

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
    private InstanceViewModel? _selectedInstance;

    /// <summary>Screenshot-harness-only hook: which tab to land on once Loaded finishes its own setup. Null = Home (the default).</summary>
    internal string? ScreenshotInitialView { get; set; }

    public MainWindow()
    {
        ThemeResources.Register(Resources);
        InitializeComponent();

        _settingsService = new SettingsService(_rootDir);
        _instanceService = new InstanceService(_rootDir);
        _musicService = new MainMenuMusicService(_rootDir);
        _settings = _settingsService.Load();

        _themeService.RainAutoDetectEnabled = _settings.RainAutoDetectEnabled;
        _themeService.WeatherApiKey = _settings.WeatherApiKey;
        _themeService.WeatherCity = _settings.WeatherCity;

        ThemeService.ApplyGlassTint(ParseGlassColor(_settings.GlassTintColor));

        Loaded += async (_, _) =>
        {
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: entered.");

            UiAnimations.FadeIn(this);
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: FadeIn done.");

            await _themeService.StartAsync();
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: ThemeService.StartAsync done.");

            RefreshInstances();
            SetActiveNav(HomeNavButton);
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: RefreshInstances done.");

            PlayMenuMusicSafely();
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: music Play() done.");

            if (ScreenshotInitialView == "Settings")
            {
                SettingsButton_Click(this, new RoutedEventArgs());
            }
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: handler exiting.");
        };
        Closed += (_, _) => _musicService.Dispose();
    }

    /// <summary>
    /// NAudio's WaveOutEvent touches a real Windows audio device — the same
    /// class of native, not-managed-catchable risk the earlier DWM P/Invoke
    /// call turned out to be on the CI image (see GlassWindowHelper's
    /// removal). Inaudible in a screenshot anyway, so skip it outright
    /// there; for real usage, still guard with try/catch so a missing/
    /// broken audio device can't take the rest of a click handler down
    /// with it (e.g. Settings silently failing to refresh the RAM tile
    /// because Play() threw before UpdateHomeHero() ran).
    /// </summary>
    private void PlayMenuMusicSafely()
    {
        if (App.ScreenshotMode) return;

        try
        {
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
        catch (Exception ex)
        {
            StatusText.Text = $"Музыка недоступна: {ex.Message}";
        }
    }

    internal static Color ParseGlassColor(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return Color.FromArgb(0x33, color.R, color.G, color.B);
        }
        catch
        {
            return Color.FromArgb(0x33, 0x4F, 0xA8, 0xFF);
        }
    }

    private void InstanceCard_Loaded(object sender, RoutedEventArgs e)
    {
        var card = (FrameworkElement)sender;
        UiAnimations.AttachHoverLift(card);
        UiAnimations.FadeIn(card, durationMs: 250, slideFromY: 8);
    }

    private void RefreshInstances()
    {
        var instances = _instanceService.LoadAll()
            .Select(i => new InstanceViewModel(i))
            .OrderByDescending(vm => vm.Instance.LastPlayedAt ?? DateTimeOffset.MinValue)
            .ToList();

        if (_selectedInstance is null || instances.All(vm => vm.Instance.Id != _selectedInstance.Instance.Id))
        {
            _selectedInstance = instances.FirstOrDefault();
        }
        foreach (var vm in instances)
        {
            vm.IsSelected = vm.Instance.Id == _selectedInstance?.Instance.Id;
        }
        InstancesList.ItemsSource = instances;

        UpdateHomeHero();
    }

    private void UpdateHomeHero()
    {
        bool hasSelection = _selectedInstance is not null;
        PlaySelectedButton.IsEnabled = hasSelection;
        EditSelectedButton.IsEnabled = hasSelection;

        if (_selectedInstance is null)
        {
            SelectedName.Text = "Нет сборок";
            SelectedSubtitle.Text = "Нажмите «Создать сборку», чтобы начать";
            LoaderLabel.Text = "—";
        }
        else
        {
            SelectedName.Text = _selectedInstance.Name;
            SelectedSubtitle.Text = $"Minecraft {_selectedInstance.Subtitle}";
            LoaderLabel.Text = _selectedInstance.Instance.Loader == ModLoader.Fabric ? "Fabric" : "Vanilla";
        }

        RamLabel.Text = $"{Math.Max(1, _settings.DefaultMemoryMaxMb / 1024)} ГБ";
        AccountLabel.Text = _settings.Username;
        AccountNameText.Text = _settings.Username;
        AvatarInitial.Text = _settings.Username[..1].ToUpperInvariant();
    }

    private void ShowView(UIElement showing)
    {
        UIElement[] all = { HomeView, InstancesView, SettingsView };
        UIElement? hiding = all.FirstOrDefault(v => !ReferenceEquals(v, showing) && v.Visibility == Visibility.Visible);
        if (hiding is not null)
        {
            UiAnimations.CrossFadeSwitch(showing, hiding);
        }
        else
        {
            showing.Visibility = Visibility.Visible;
            showing.SetValue(UIElement.OpacityProperty, 1.0);
        }
    }

    private void SetActiveNav(Button active)
    {
        // Tag is compared against the XAML Trigger's Value="True" as a plain
        // string (Tag's DP type is object, so WPF has no other type hint to
        // convert against) — use string literals here too, not a C# bool,
        // or the trigger silently never matches.
        HomeNavButton.Tag = "False";
        InstancesNavButton.Tag = "False";
        SettingsNavButton.Tag = "False";
        active.Tag = "True";
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Главная";
        PageSubtitle.Text = "Выбранная сборка";
        UpdateHomeHero();
        SetActiveNav(HomeNavButton);
        ShowView(HomeView);
    }

    private void InstancesButton_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Сборки";
        PageSubtitle.Text = "Управление установленными сборками";
        SetActiveNav(InstancesNavButton);
        ShowView(InstancesView);
    }

    private void AccountChip_Click(object sender, RoutedEventArgs e) => SettingsButton_Click(sender, e);

    private void NewInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewInstanceWindow(_versionService, _instanceService) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            if (dialog.CreatedInstance is { } created)
            {
                _selectedInstance = new InstanceViewModel(created);
            }
            RefreshInstances();
            HomeButton_Click(sender, e);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Настройки";
        PageSubtitle.Text = "Параметры лаунчера";
        SetActiveNav(SettingsNavButton);
        LoadSettingsView();
        ShowView(SettingsView);
    }

    private void LoadSettingsView()
    {
        SettingsUsernameBox.Text = _settings.Username;
        SettingsJavaPathBox.Text = _settings.JavaExecutable;
        SettingsJvmArgsBox.Text = _settings.DefaultJvmArgs;

        var memoryOptionsGb = SystemMemory.BuildMemoryOptionsGb();
        SettingsMemoryBox.ItemsSource = memoryOptionsGb;
        int selectedGb = Math.Max(4, (int)Math.Ceiling(_settings.DefaultMemoryMaxMb / 1024.0));
        SettingsMemoryBox.SelectedItem = memoryOptionsGb.Contains(selectedGb) ? selectedGb : memoryOptionsGb.FirstOrDefault();

        SettingsMusicEnabledBox.IsChecked = _settings.MainMenuMusicEnabled;
        SettingsMusicVolumeSlider.Value = _settings.MainMenuMusicVolume;
        SettingsRainAutoDetectBox.IsChecked = _settings.RainAutoDetectEnabled;
        SettingsWeatherApiKeyBox.Text = _settings.WeatherApiKey;
        SettingsWeatherCityBox.Text = _settings.WeatherCity;
        SettingsGlassHexBox.Text = _settings.GlassTintColor;
        UpdateSettingsGlassPreview();

        SettingsStatusText.Text = string.Empty;
    }

    private void GlassSwatch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var border = (Border)sender;
        if (border.Background is SolidColorBrush brush)
        {
            SettingsGlassHexBox.Text = $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
        }
    }

    private void SettingsGlassHexBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateSettingsGlassPreview();

    private void UpdateSettingsGlassPreview()
    {
        if (SettingsGlassPreview is null) return;
        SettingsGlassPreview.Background = new SolidColorBrush(ParseGlassColor(SettingsGlassHexBox.Text));
        ThemeService.ApplyGlassTint(ParseGlassColor(SettingsGlassHexBox.Text));
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        int memoryGb = SettingsMemoryBox.SelectedItem is int value ? value : 4;

        _settings.Username = string.IsNullOrWhiteSpace(SettingsUsernameBox.Text) ? "Player" : SettingsUsernameBox.Text.Trim();
        _settings.JavaExecutable = string.IsNullOrWhiteSpace(SettingsJavaPathBox.Text) ? "javaw" : SettingsJavaPathBox.Text.Trim();
        _settings.DefaultMemoryMinMb = 512;
        _settings.DefaultMemoryMaxMb = memoryGb * 1024;
        _settings.DefaultJvmArgs = SettingsJvmArgsBox.Text.Trim();
        _settings.MainMenuMusicEnabled = SettingsMusicEnabledBox.IsChecked == true;
        _settings.MainMenuMusicVolume = SettingsMusicVolumeSlider.Value;
        _settings.RainAutoDetectEnabled = SettingsRainAutoDetectBox.IsChecked == true;
        _settings.WeatherApiKey = string.IsNullOrWhiteSpace(SettingsWeatherApiKeyBox.Text) ? null : SettingsWeatherApiKeyBox.Text.Trim();
        _settings.WeatherCity = string.IsNullOrWhiteSpace(SettingsWeatherCityBox.Text) ? "Moscow" : SettingsWeatherCityBox.Text.Trim();
        _settings.GlassTintColor = string.IsNullOrWhiteSpace(SettingsGlassHexBox.Text) ? "#8B7CFF" : SettingsGlassHexBox.Text.Trim();

        // Applying the visible RAM/account tiles first means a save is
        // always reflected on screen even if something below (theme/music)
        // throws — this used to be the last thing this method did, behind
        // a music Play() call that can genuinely fail on a broken/missing
        // audio device.
        _settingsService.Save(_settings);
        UpdateHomeHero();

        _themeService.RainAutoDetectEnabled = _settings.RainAutoDetectEnabled;
        _themeService.WeatherApiKey = _settings.WeatherApiKey;
        _themeService.WeatherCity = _settings.WeatherCity;
        ThemeService.ApplyGlassTint(ParseGlassColor(_settings.GlassTintColor));

        PlayMenuMusicSafely();

        SettingsStatusText.Text = "Сохранено.";
    }

    private void EditInstance_Click(object sender, RoutedEventArgs e)
    {
        var vm = (InstanceViewModel)((FrameworkElement)sender).Tag;
        EditInstance(vm);
    }

    private void EditSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedInstance is not null)
        {
            EditInstance(_selectedInstance);
        }
    }

    private void EditInstance(InstanceViewModel vm)
    {
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
            if (_selectedInstance?.Instance.Id == vm.Instance.Id)
            {
                _selectedInstance = null;
            }
            RefreshInstances();
        }
    }

    private void PlayInstance_Click(object sender, RoutedEventArgs e)
    {
        var vm = (InstanceViewModel)((FrameworkElement)sender).Tag;
        _ = LaunchInstanceAsync(vm, (Button)sender);
    }

    private void PlaySelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedInstance is not null)
        {
            _ = LaunchInstanceAsync(_selectedInstance, (Button)sender);
        }
    }

    private async Task LaunchInstanceAsync(InstanceViewModel vm, Button triggerButton)
    {
        string username = _settings.Username;
        if (!AuthService.IsValidUsername(username))
        {
            StatusText.Text = "Никнейм: 3–16 символов, латиница/цифры/подчёркивание. Задайте его в Настройках.";
            return;
        }

        triggerButton.IsEnabled = false;
        UiAnimations.StartPulse(triggerButton);
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
            if (instance.Loader == ModLoader.Fabric)
            {
                string modsDir = _instanceService.GetModsDir(instance);
                BundledModService.EnsureReVerfyxClientInstalled(modsDir);
            }
            launcher.Launch(installed, profile, instance, _settings, gameDir);

            instance.LastPlayedAt = DateTimeOffset.UtcNow;
            _instanceService.Save(instance);

            StatusText.Text = $"Запущено: {instance.Name} ({launchDetail.Id}) как {username}.";
            RefreshInstances();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка запуска: {ex.Message}";
        }
        finally
        {
            UiAnimations.StopPulse(triggerButton);
            triggerButton.IsEnabled = true;
        }
    }
}
