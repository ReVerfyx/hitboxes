using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher;

/// <summary>Thin binding wrapper so the instance grid can show a formatted subtitle.</summary>
public sealed class InstanceViewModel
{
    public Instance Instance { get; }
    public string Name => Instance.Name;
    public string Subtitle => Instance.Loader == ModLoader.Fabric
        ? $"{Instance.McVersion} · Fabric"
        : Instance.McVersion;

    /// <summary>Set by MainWindow.RefreshInstances so the grid card can show a selected-glow border. No change notification needed — the ItemsSource list is rebuilt on every refresh.</summary>
    public bool IsSelected { get; set; }

    public InstanceViewModel(Instance instance)
    {
        Instance = instance;
    }
}
