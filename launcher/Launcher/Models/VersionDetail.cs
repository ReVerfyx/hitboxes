using System.Text.Json.Serialization;

namespace Hitboxes.Launcher.Models;

/// <summary>
/// Subset of a version's own JSON (e.g. https://.../1.16.5.json) needed to
/// install and launch it. Only the "arguments"-style format is modeled —
/// every version from 1.16.5 onward uses it.
/// </summary>
public sealed class VersionDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = string.Empty;

    [JsonPropertyName("assets")]
    public string AssetsIndexId { get; set; } = string.Empty;

    [JsonPropertyName("assetIndex")]
    public AssetIndexRef AssetIndex { get; set; } = new();

    [JsonPropertyName("downloads")]
    public DownloadsSection Downloads { get; set; } = new();

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new();

    [JsonPropertyName("arguments")]
    public ArgumentsSection? Arguments { get; set; }
}

public sealed class AssetIndexRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public sealed class DownloadsSection
{
    [JsonPropertyName("client")]
    public DownloadArtifact Client { get; set; } = new();
}

public sealed class DownloadArtifact
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public sealed class Library
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public LibraryDownloads? Downloads { get; set; }

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }
}

public sealed class LibraryDownloads
{
    [JsonPropertyName("artifact")]
    public DownloadArtifactWithPath? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, DownloadArtifactWithPath>? Classifiers { get; set; }
}

public sealed class DownloadArtifactWithPath : DownloadArtifact
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public sealed class Rule
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "allow"; // "allow" | "disallow"

    [JsonPropertyName("os")]
    public OsRule? Os { get; set; }
}

public sealed class OsRule
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } // "windows" | "linux" | "osx"
}

public sealed class ArgumentsSection
{
    [JsonPropertyName("game")]
    public List<System.Text.Json.JsonElement> Game { get; set; } = new();

    [JsonPropertyName("jvm")]
    public List<System.Text.Json.JsonElement> Jvm { get; set; } = new();
}
