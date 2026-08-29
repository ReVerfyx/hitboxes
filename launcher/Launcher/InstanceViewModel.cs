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

    public InstanceViewModel(Instance instance)
    {
        Instance = instance;
    }
}
