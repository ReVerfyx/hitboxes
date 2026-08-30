using System.Windows;
using System.Windows.Controls;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

public partial class NewInstanceWindow : Window
{
    private readonly MinecraftVersionService _versionService;
    private readonly InstanceService _instanceService;

    private List<VersionEntry> _allVersions = new();

    public Instance? CreatedInstance { get; private set; }

    public NewInstanceWindow(MinecraftVersionService versionService, InstanceService instanceService)
    {
        ThemeResources.Register(Resources);
        InitializeComponent();
        _versionService = versionService;
        _instanceService = instanceService;

        Loaded += async (_, _) =>
        {
            UiAnimations.FadeIn(this);

            if (App.ScreenshotMode)
            {
                SetVersions(ScreenshotHarness.SampleVersions);
                return;
            }

            try
            {
                SetVersions(await _versionService.GetSupportedReleasesAsync());
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Не удалось получить список версий: {ex.Message}";
            }
        };
    }

    private void SetVersions(List<VersionEntry> versions)
    {
        _allVersions = versions;
        VersionListBox.ItemsSource = _allVersions;
        if (_allVersions.Count > 0)
        {
            VersionListBox.SelectedIndex = 0;
        }
    }

    private void VersionSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = VersionSearchBox.Text.Trim();
        var filtered = string.IsNullOrEmpty(query)
            ? _allVersions
            : _allVersions.Where(v => v.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        VersionListBox.ItemsSource = filtered;
        if (filtered.Count > 0)
        {
            VersionListBox.SelectedIndex = 0;
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (VersionListBox.SelectedItem is not VersionEntry selected)
        {
            ErrorText.Text = "Выберите версию.";
            return;
        }

        string name = string.IsNullOrWhiteSpace(NameBox.Text) ? selected.Id : NameBox.Text.Trim();

        var instance = new Instance
        {
            Name = name,
            McVersion = selected.Id,
            Loader = FabricRadio.IsChecked == true ? ModLoader.Fabric : ModLoader.Vanilla,
        };

        _instanceService.Save(instance);
        CreatedInstance = instance;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
