namespace Hitboxes.Launcher.Models;

public enum ModLoader
{
    Vanilla,
    Fabric
}

/// <summary>
/// One "instance" — a Prism/MultiMC-style isolated install: its own game
/// directory, mods folder and launch overrides, independent of other
/// instances. The on-disk shape (a single flat instance.json per folder)
/// is our own — deliberately not a copy of Prism's mmc-pack.json/instance.cfg
/// pair, just the same underlying idea of "one instance = one folder".
/// </summary>
public sealed class Instance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Instance";
    public string McVersion { get; set; } = string.Empty;
    public ModLoader Loader { get; set; } = ModLoader.Vanilla;
    public string? FabricLoaderVersion { get; set; }
    public string IconKey { get; set; } = "grass";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>Per-instance overrides; null means "use launcher defaults".</summary>
    public int? MemoryMinMb { get; set; }
    public int? MemoryMaxMb { get; set; }
    public string? ExtraJvmArgs { get; set; }
    public string? JavaExecutableOverride { get; set; }
}
