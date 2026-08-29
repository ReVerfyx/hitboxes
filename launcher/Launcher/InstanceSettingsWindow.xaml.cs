using System.IO;
using System.Windows;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

public partial class InstanceSettingsWindow : Window
{
    private readonly Instance _instance;
    private readonly InstanceService _instanceService;
    private readonly ModrinthService _modrinthService = new();

    public InstanceSettingsWindow(Instance instance, InstanceService instanceService, LauncherSettings settings)
    {
        InitializeComponent();
        _instance = instance;
        _instanceService = instanceService;

        NameBox.Text = instance.Name;
        JavaPathBox.Text = instance.JavaExecutableOverride ?? string.Empty;
        MemMinBox.Text = instance.MemoryMinMb?.ToString() ?? string.Empty;
        MemMaxBox.Text = instance.MemoryMaxMb?.ToString() ?? string.Empty;
        JvmArgsBox.Text = instance.ExtraJvmArgs ?? string.Empty;

        ModsTab.IsEnabled = instance.Loader == ModLoader.Fabric;
        if (instance.Loader != ModLoader.Fabric)
        {
            ModsTab.Header = "Моды (только Fabric)";
        }

        RefreshInstalledMods();

        Loaded += (_, _) =>
        {
            GlassWindowHelper.Enable(this, isDialog: true);
            UiAnimations.FadeIn(this);
        };
    }

    private void RefreshInstalledMods()
    {
        string modsDir = _instanceService.GetModsDir(_instance);
        InstalledModsList.ItemsSource = Directory.EnumerateFiles(modsDir, "*.jar")
            .Select(Path.GetFileName)
            .ToList();
    }

    private async void SearchMods_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Поиск...";
        try
        {
            var results = await _modrinthService.SearchModsAsync(ModSearchBox.Text.Trim(), _instance.McVersion);
            SearchResultsList.ItemsSource = results;
            StatusText.Text = $"Найдено: {results.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка поиска: {ex.Message}";
        }
    }

    private async void InstallMod_Click(object sender, RoutedEventArgs e)
    {
        if (SearchResultsList.SelectedItem is not ModrinthProject project)
        {
            StatusText.Text = "Выберите мод в списке.";
            return;
        }

        StatusText.Text = $"Установка {project.Title}...";
        try
        {
            var version = await _modrinthService.GetBestVersionAsync(project.ProjectId, _instance.McVersion);
            if (version is null)
            {
                StatusText.Text = $"{project.Title}: нет версии под {_instance.McVersion} (Fabric).";
                return;
            }

            string modsDir = _instanceService.GetModsDir(_instance);
            await _modrinthService.DownloadModAsync(version, modsDir);

            RefreshInstalledMods();
            StatusText.Text = $"{project.Title} установлен.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка установки: {ex.Message}";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _instance.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? _instance.Name : NameBox.Text.Trim();
        _instance.JavaExecutableOverride = string.IsNullOrWhiteSpace(JavaPathBox.Text) ? null : JavaPathBox.Text.Trim();
        _instance.MemoryMinMb = int.TryParse(MemMinBox.Text, out int min) ? min : null;
        _instance.MemoryMaxMb = int.TryParse(MemMaxBox.Text, out int max) ? max : null;
        _instance.ExtraJvmArgs = string.IsNullOrWhiteSpace(JvmArgsBox.Text) ? null : JvmArgsBox.Text.Trim();

        _instanceService.Save(_instance);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
