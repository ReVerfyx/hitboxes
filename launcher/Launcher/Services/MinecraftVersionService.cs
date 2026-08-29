using System.Net.Http;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Reads the official Mojang version manifest only — no third-party or
/// modified version sources are ever consulted.
/// </summary>
public sealed class MinecraftVersionService
{
    private const string ManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";
    private static readonly Version MinimumSupported = new(1, 16, 5);

    private readonly HttpClient _http = new();

    public async Task<List<VersionEntry>> GetSupportedReleasesAsync()
    {
        using var stream = await _http.GetStreamAsync(ManifestUrl);
        var manifest = await JsonSerializer.DeserializeAsync<VersionManifest>(stream)
            ?? throw new InvalidOperationException("Empty version manifest.");

        return manifest.Versions
            .Where(v => v.Type == "release" && MeetsMinimumVersion(v.Id))
            .OrderByDescending(v => v.ReleaseTime)
            .ToList();
    }

    public async Task<VersionDetail> GetVersionDetailAsync(string versionJsonUrl)
    {
        using var stream = await _http.GetStreamAsync(versionJsonUrl);
        return await JsonSerializer.DeserializeAsync<VersionDetail>(stream)
            ?? throw new InvalidOperationException("Empty version detail.");
    }

    private static bool MeetsMinimumVersion(string id)
    {
        // Release ids are "major.minor[.patch]"; anything that doesn't
        // parse that way (e.g. odd historical ids) is excluded rather
        // than guessed at.
        var parts = id.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }
        if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor))
        {
            return false;
        }
        int patch = parts.Length >= 3 && int.TryParse(parts[2], out int p) ? p : 0;

        var parsed = new Version(major, minor, patch);
        return parsed >= MinimumSupported;
    }
}
