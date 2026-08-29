using System.Net.Http;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Talks to Fabric's own meta API (meta.fabricmc.net) to resolve a loader
/// version for a given Minecraft version and merge it onto that version's
/// vanilla details — the same two-step "vanilla json + loader json" approach
/// every Fabric-aware launcher uses, just written against our own
/// <see cref="VersionDetail"/> model instead of copying a specific
/// launcher's internal format.
/// </summary>
public sealed class FabricInstallerService
{
    private const string MetaBaseUrl = "https://meta.fabricmc.net/v2/versions/loader";
    private readonly HttpClient _http = new();

    public async Task<string?> GetLatestStableLoaderVersionAsync(string mcVersion)
    {
        using var stream = await _http.GetStreamAsync($"{MetaBaseUrl}/{mcVersion}");
        var entries = await JsonSerializer.DeserializeAsync<List<FabricLoaderEntry>>(stream) ?? new();
        return entries.FirstOrDefault(e => e.Loader.Stable)?.Loader.Version
            ?? entries.FirstOrDefault()?.Loader.Version;
    }

    public async Task<VersionDetail> GetMergedVersionDetailAsync(VersionDetail vanilla, string mcVersion, string loaderVersion)
    {
        string profileUrl = $"{MetaBaseUrl}/{mcVersion}/{loaderVersion}/profile/json";
        using var stream = await _http.GetStreamAsync(profileUrl);
        var profile = await JsonSerializer.DeserializeAsync<FabricProfile>(stream)
            ?? throw new InvalidOperationException("Empty Fabric loader profile.");

        return new VersionDetail
        {
            Id = $"fabric-loader-{loaderVersion}-{mcVersion}",
            MainClass = profile.MainClass,
            AssetsIndexId = vanilla.AssetsIndexId,
            AssetIndex = vanilla.AssetIndex,
            Downloads = vanilla.Downloads,
            Arguments = vanilla.Arguments,
            Libraries = vanilla.Libraries.Concat(profile.Libraries).ToList(),
        };
    }
}
