using System.Text.Json.Serialization;

namespace Hitboxes.Launcher.Models;

/// <summary>Entry from GET /v2/versions/loader/{mcVersion} on meta.fabricmc.net.</summary>
public sealed class FabricLoaderEntry
{
    [JsonPropertyName("loader")]
    public FabricLoaderVersion Loader { get; set; } = new();
}

public sealed class FabricLoaderVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}

/// <summary>
/// The v2 "profile/json" response — same shape as a vanilla version.json,
/// but with only the loader's own libraries and a KnotClient main class.
/// It normally also carries "inheritsFrom" pointing at the vanilla id it
/// builds on, which we resolve ourselves in <see cref="FabricInstallerService"/>
/// rather than trusting a chain of files.
/// </summary>
public sealed class FabricProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("inheritsFrom")]
    public string InheritsFrom { get; set; } = string.Empty;

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = string.Empty;

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new();
}
