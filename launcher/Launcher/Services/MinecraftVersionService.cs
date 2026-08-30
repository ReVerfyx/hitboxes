using System.Net.Http;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Reads the official Mojang version manifest — through a user-configured
/// proxy and/or a same-content mirror (see NetworkSettings) when the direct
/// connection fails, since Mojang's endpoints aren't reachable from every
/// network. Either path serves the exact same official data; no modified
/// or third-party version source is ever consulted.
/// </summary>
public sealed class MinecraftVersionService
{
    private const string ManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";
    private static readonly Version MinimumSupported = new(1, 16, 5);

    private HttpClient _http = NetworkSettings.CreateHttpClient();

    /// <summary>Called after Settings saves a new proxy configuration so this
    /// already-constructed, long-lived service picks it up immediately
    /// instead of only on the next app start.</summary>
    public void RefreshHttpClient() => _http = NetworkSettings.CreateHttpClient();

    public async Task<List<VersionEntry>> GetSupportedReleasesAsync()
    {
        var manifest = await GetJsonWithMirrorFallbackAsync<VersionManifest>(ManifestUrl)
            ?? throw new InvalidOperationException("Empty version manifest.");

        return manifest.Versions
            .Where(v => v.Type == "release" && MeetsMinimumVersion(v.Id))
            .OrderByDescending(v => v.ReleaseTime)
            .ToList();
    }

    public async Task<VersionDetail> GetVersionDetailAsync(string versionJsonUrl)
    {
        return await GetJsonWithMirrorFallbackAsync<VersionDetail>(versionJsonUrl)
            ?? throw new InvalidOperationException("Empty version detail.");
    }

    /// <summary>Tries the real URL first; only on a network-level failure
    /// (not an HTTP error status) does it retry against the mirror — so a
    /// working direct connection never takes the mirror path at all.</summary>
    private async Task<T?> GetJsonWithMirrorFallbackAsync<T>(string url)
    {
        try
        {
            using var stream = await _http.GetStreamAsync(url);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            string? mirrorUrl = NetworkSettings.TryGetMirrorUrl(url);
            if (mirrorUrl is null)
            {
                throw;
            }

            DevLog.Log($"{url} unreachable ({ex.Message}) — retrying via mirror {mirrorUrl}");
            using var mirrorStream = await _http.GetStreamAsync(mirrorUrl);
            return await JsonSerializer.DeserializeAsync<T>(mirrorStream);
        }
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
