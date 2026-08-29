using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Mod search/download against Modrinth's public API — no API key needed.
/// Scoped to Fabric mods matching a specific instance's Minecraft version,
/// the same "browse, pick a version-compatible file, drop it in mods/"
/// flow Prism Launcher's mod page offers, built against Modrinth's own
/// documented v2 API rather than any particular launcher's internal code.
/// </summary>
public sealed class ModrinthService
{
    private const string ApiBase = "https://api.modrinth.com/v2";
    private readonly HttpClient _http;

    public ModrinthService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReVerfyxClientLauncher", "0.1.0"));
    }

    public async Task<List<ModrinthProject>> SearchModsAsync(string query, string mcVersion, string loader = "fabric")
    {
        var facets = JsonSerializer.Serialize(new[]
        {
            new[] { $"project_type:mod" },
            new[] { $"versions:{mcVersion}" },
            new[] { $"categories:{loader}" },
        });

        string url = $"{ApiBase}/search?query={Uri.EscapeDataString(query)}&facets={Uri.EscapeDataString(facets)}&limit=20";
        using var stream = await _http.GetStreamAsync(url);
        var result = await JsonSerializer.DeserializeAsync<ModrinthSearchResult>(stream);
        return result?.Hits ?? new List<ModrinthProject>();
    }

    public async Task<ModrinthVersion?> GetBestVersionAsync(string projectId, string mcVersion, string loader = "fabric")
    {
        string url = $"{ApiBase}/project/{projectId}/version?game_versions=[\"{mcVersion}\"]&loaders=[\"{loader}\"]";
        using var stream = await _http.GetStreamAsync(url);
        var versions = await JsonSerializer.DeserializeAsync<List<ModrinthVersion>>(stream) ?? new();
        return versions.FirstOrDefault();
    }

    public async Task<string> DownloadModAsync(ModrinthVersion version, string modsDir)
    {
        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.First();
        string destPath = Path.Combine(modsDir, file.Filename);

        if (!File.Exists(destPath))
        {
            using var response = await _http.GetAsync(file.Url);
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(destPath);
            await response.Content.CopyToAsync(fileStream);
        }

        return destPath;
    }
}
