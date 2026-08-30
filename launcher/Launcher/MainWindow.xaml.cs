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

            GlassWindowHelper.Enable(this);
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: GlassWindowHelper.Enable done.");

            UiAnimations.FadeIn(this);
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: FadeIn done.");

            await _themeService.StartAsync();
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: ThemeService.StartAsync done.");

            RefreshInstances();
            SetActiveNav(HomeNavButton);
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: RefreshInstances done.");

            // Music is inaudible in a screenshot and NAudio touches a real
            // Windows audio device (WaveOutEvent) to play anything — the
            // same class of native, not-managed-catchable risk the DWM
            // P/Invoke call turned out to be on this CI image. Skip it
            // outright here rather than relying on "no assets means Play()
            // no-ops anyway".
            if (!App.ScreenshotMode)
            {
                _musicService.Volume = (float)_settings.MainMenuMusicVolume;
                if (_settings.MainMenuMusicEnabled)
                {
                    _musicService.Play();
                }
            }
            if (App.ScreenshotMode) ScreenshotHarness.Log("MainWindow.Loaded: music Play() done, handler exiting.");
        };
        Closed += (_, _) => _musicService.Dispose();
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
        InstancesList.ItemsSource = instances;

        if (_selectedInstance is null || instances.All(vm => vm.Instance.Id != _selectedInstance.Instance.Id))
        {
            _selectedInstance = instances.FirstOrDefault();
        }
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
        AccountLabel.Text = string.IsNullOrWhiteSpace(UsernameBox.Text) ? "Player" : UsernameBox.Text.Trim();
    }

    private void ShowView(UIElement showing)
    {
        UIElement hiding = ReferenceEquals(showing, HomeView) ? InstancesView : HomeView;
        UiAnimations.CrossFadeSwitch(showing, hiding);
    }

    private void SetActiveNav(Button active)
    {
        HomeNavButton.Style = (Style)FindResource("GlassSecondaryButtonStyle");
        InstancesNavButton.Style = (Style)FindResource("GlassSecondaryButtonStyle");
        active.Style = (Style)FindResource("GlassButtonStyle");
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

    private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Fires as soon as InitializeComponent() sets UsernameBox.Text="Player"
        // during XAML parsing — at that point AccountLabel (declared later in
        // the visual tree) hasn't been assigned to its named field yet.
        if (AccountLabel is null) return;
        AccountLabel.Text = string.IsNullOrWhiteSpace(UsernameBox.Text) ? "Player" : UsernameBox.Text.Trim();
    }

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
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Result;
            _settingsService.Save(_settings);

            _themeService.RainAutoDetectEnabled = _settings.RainAutoDetectEnabled;
            _themeService.WeatherApiKey = _settings.WeatherApiKey;
            _themeService.WeatherCity = _settings.WeatherCity;
            ThemeService.ApplyGlassTint(ParseGlassColor(_settings.GlassTintColor));

            _musicService.Volume = (float)_settings.MainMenuMusicVolume;
            if (_settings.MainMenuMusicEnabled && !_musicService.IsPlaying)
            {
                _musicService.Play();
            }
            else if (!_settings.MainMenuMusicEnabled)
            {
                _musicService.Stop();
            }

            UpdateHomeHero();
        }
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
        string username = UsernameBox.Text.Trim();
        if (!AuthService.IsValidUsername(username))
        {
            StatusText.Text = "Никнейм: 3–16 символов, латиница/цифры/подчёркивание.";
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
