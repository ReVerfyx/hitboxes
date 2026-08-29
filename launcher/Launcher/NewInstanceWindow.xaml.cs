using System.Windows;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;

namespace Hitboxes.Launcher;

public partial class NewInstanceWindow : Window
{
    private readonly MinecraftVersionService _versionService;
    private readonly InstanceService _instanceService;

    public NewInstanceWindow(MinecraftVersionService versionService, InstanceService instanceService)
    {
        InitializeComponent();
        _versionService = versionService;
        _instanceService = instanceService;

        Loaded += async (_, _) =>
        {
            try
            {
                var versions = await _versionService.GetSupportedReleasesAsync();
                VersionComboBox.ItemsSource = versions;
                if (versions.Count > 0)
                {
                    VersionComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Не удалось получить список версий: {ex.Message}";
            }
        };
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (VersionComboBox.SelectedItem is not VersionEntry selected)
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
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
