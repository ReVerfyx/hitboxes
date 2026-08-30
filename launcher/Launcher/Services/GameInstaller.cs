using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Downloads exactly what the official version JSON points at: the client
/// jar, the Windows-applicable libraries (native and pure-Java), and the
/// asset objects listed in that version's asset index. Everything is
/// fetched from Mojang's CDN URLs embedded in the manifest — through a
/// user-configured proxy and/or a same-content mirror (see
/// NetworkSettings) when the direct connection fails.
/// </summary>
public sealed class GameInstaller
{
    private readonly HttpClient _http = NetworkSettings.CreateHttpClient();
    private readonly string _rootDir;

    public GameInstaller(string rootDir)
    {
        _rootDir = rootDir;
    }

    public string VersionsDir => Path.Combine(_rootDir, "versions");
    public string LibrariesDir => Path.Combine(_rootDir, "libraries");
    public string AssetsDir => Path.Combine(_rootDir, "assets");

    public async Task<InstalledVersion> EnsureInstalledAsync(VersionDetail detail, IProgress<string>? progress = null)
    {
        string versionDir = Path.Combine(VersionsDir, detail.Id);
        Directory.CreateDirectory(versionDir);

        string clientJarPath = Path.Combine(versionDir, $"{detail.Id}.jar");
        progress?.Report($"Клиент {detail.Id}...");
        await DownloadIfMissingAsync(detail.Downloads.Client.Url, clientJarPath, detail.Downloads.Client.Sha1);

        string nativesDir = Path.Combine(versionDir, "natives");
        Directory.CreateDirectory(nativesDir);

        var classpathEntries = new List<string> { clientJarPath };

        foreach (var library in detail.Libraries)
        {
            if (!IsAllowedOnWindows(library))
            {
                continue;
            }

            if (library.Downloads?.Artifact is { } artifact && !string.IsNullOrEmpty(artifact.Path))
            {
                string libPath = Path.Combine(LibrariesDir, artifact.Path.Replace('/', Path.DirectorySeparatorChar));
                progress?.Report($"Библиотека {library.Name}...");
                await DownloadIfMissingAsync(artifact.Url, libPath, artifact.Sha1);
                classpathEntries.Add(libPath);
            }
            else if (!string.IsNullOrEmpty(library.Url) && !string.IsNullOrEmpty(library.Name))
            {
                // Fabric-style entry: "name" is a Maven coordinate, "url" is
                // the repo base — derive the standard Maven layout path.
                string relativePath = MavenCoordinateToPath(library.Name);
                string libPath = Path.Combine(LibrariesDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string url = library.Url.TrimEnd('/') + "/" + relativePath;
                progress?.Report($"Библиотека {library.Name}...");
                await DownloadIfMissingAsync(url, libPath, expectedSha1: null);
                classpathEntries.Add(libPath);
            }

            if (library.Natives is { } natives && natives.TryGetValue("windows", out string? classifierKey)
                && library.Downloads?.Classifiers is { } classifiers
                && classifiers.TryGetValue(classifierKey, out var nativeArtifact))
            {
                string nativeJarPath = Path.Combine(LibrariesDir, nativeArtifact.Path.Replace('/', Path.DirectorySeparatorChar));
                progress?.Report($"Нативная библиотека {library.Name}...");
                await DownloadIfMissingAsync(nativeArtifact.Url, nativeJarPath, nativeArtifact.Sha1);
                ExtractNatives(nativeJarPath, nativesDir);
            }
        }

        progress?.Report("Ассеты...");
        await DownloadAssetsAsync(detail.AssetIndex, progress);

        return new InstalledVersion(detail, clientJarPath, nativesDir, classpathEntries);
    }

    private async Task DownloadAssetsAsync(AssetIndexRef assetIndexRef, IProgress<string>? progress)
    {
        if (string.IsNullOrEmpty(assetIndexRef.Url))
        {
            return;
        }

        string indexPath = Path.Combine(AssetsDir, "indexes", $"{assetIndexRef.Id}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
        await DownloadIfMissingAsync(assetIndexRef.Url, indexPath, expectedSha1: null);

        using var stream = File.OpenRead(indexPath);
        using var doc = await JsonDocument.ParseAsync(stream);
        var objects = doc.RootElement.GetProperty("objects");

        string objectsDir = Path.Combine(AssetsDir, "objects");
        int total = objects.EnumerateObject().Count();
        int done = 0;

        foreach (var entry in objects.EnumerateObject())
        {
            string hash = entry.Value.GetProperty("hash").GetString()!;
            string prefix = hash[..2];
            string objectPath = Path.Combine(objectsDir, prefix, hash);
            string url = $"https://resources.download.minecraft.net/{prefix}/{hash}";

            await DownloadIfMissingAsync(url, objectPath, expectedSha1: hash);

            done++;
            if (done % 50 == 0)
            {
                progress?.Report($"Ассеты {done}/{total}...");
            }
        }
    }

    private async Task DownloadIfMissingAsync(string url, string destinationPath, string? expectedSha1)
    {
        if (File.Exists(destinationPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            string? mirrorUrl = NetworkSettings.TryGetMirrorUrl(url);
            if (mirrorUrl is null)
            {
                throw;
            }

            DevLog.Log($"{url} unreachable ({ex.Message}) — retrying via mirror {mirrorUrl}");
            response = await _http.GetAsync(mirrorUrl);
            response.EnsureSuccessStatusCode();
        }

        using (response)
        {
            string tempPath = destinationPath + ".tmp";
            await using (var fileStream = File.Create(tempPath))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            File.Move(tempPath, destinationPath, overwrite: true);
        }
    }

    private static bool IsAllowedOnWindows(Library library)
    {
        if (library.Rules is null || library.Rules.Count == 0)
        {
            return true;
        }

        bool allowed = false;
        foreach (var rule in library.Rules)
        {
            bool matchesOs = rule.Os?.Name is null or "windows";
            if (!matchesOs)
            {
                continue;
            }
            allowed = rule.Action == "allow";
        }
        return allowed;
    }

    /// <summary>"group.id:artifact:version[:classifier]" -&gt; standard Maven repo-relative path.</summary>
    private static string MavenCoordinateToPath(string coordinate)
    {
        string[] parts = coordinate.Split(':');
        string group = parts[0].Replace('.', '/');
        string artifact = parts[1];
        string version = parts[2];
        string classifierSuffix = parts.Length > 3 ? $"-{parts[3]}" : string.Empty;
        return $"{group}/{artifact}/{version}/{artifact}-{version}{classifierSuffix}.jar";
    }

    private static void ExtractNatives(string nativeJarPath, string destinationDir)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(nativeJarPath);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.StartsWith("META-INF/") || entry.Name.Length == 0)
            {
                continue;
            }
            string outPath = Path.Combine(destinationDir, entry.Name);
            if (!File.Exists(outPath))
            {
                entry.ExtractToFile(outPath, overwrite: false);
            }
        }
    }
}

public sealed record InstalledVersion(
    VersionDetail Detail,
    string ClientJarPath,
    string NativesDir,
    List<string> ClasspathEntries);
