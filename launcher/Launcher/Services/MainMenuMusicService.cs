using System.IO;
using System.Text.Json;
using NAudio.Vorbis;
using NAudio.Wave;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Background music for the launcher's main menu — the original C418
/// soundtrack. No track is bundled with the launcher itself: this service
/// only ever plays .ogg files that already exist under the shared
/// <c>assets/objects</c> store because a version was installed through
/// <see cref="GameInstaller"/>, which fetches them straight from Mojang's
/// own asset CDN as part of that official, licensed download. If nothing
/// has been installed yet there is simply nothing to play.
/// </summary>
public sealed class MainMenuMusicService : IDisposable
{
    private readonly string _assetsDir;
    private readonly Random _random = new();
    private readonly List<string> _menuTrackHashes = new();
    private readonly List<string> _allTrackHashes = new();

    private WaveOutEvent? _output;
    private VorbisWaveReader? _reader;
    private bool _scanned;

    public bool IsPlaying { get; private set; }
    public float Volume { get; set; } = 0.5f;

    public MainMenuMusicService(string rootDir)
    {
        _assetsDir = Path.Combine(rootDir, "assets");
    }

    public void Play()
    {
        if (IsPlaying)
        {
            return;
        }

        EnsureScanned();
        PlayNextTrack();
    }

    public void Stop()
    {
        IsPlaying = false;
        _output?.Stop();
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }

    private void EnsureScanned()
    {
        if (_scanned)
        {
            return;
        }
        _scanned = true;

        string indexesDir = Path.Combine(_assetsDir, "indexes");
        if (!Directory.Exists(indexesDir))
        {
            return;
        }

        foreach (var indexFile in Directory.EnumerateFiles(indexesDir, "*.json"))
        {
            try
            {
                ScanIndexFile(indexFile);
            }
            catch (JsonException)
            {
                // Skip a malformed index rather than failing music entirely.
            }
        }
    }

    private void ScanIndexFile(string indexFile)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(indexFile));
        if (!doc.RootElement.TryGetProperty("objects", out var objects))
        {
            return;
        }

        foreach (var entry in objects.EnumerateObject())
        {
            if (!entry.Name.Contains("sounds/music/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string hash = entry.Value.GetProperty("hash").GetString()!;
            _allTrackHashes.Add(hash);
            if (entry.Name.Contains("menu", StringComparison.OrdinalIgnoreCase))
            {
                _menuTrackHashes.Add(hash);
            }
        }
    }

    private void PlayNextTrack()
    {
        var pool = _menuTrackHashes.Count > 0 ? _menuTrackHashes : _allTrackHashes;
        if (pool.Count == 0)
        {
            IsPlaying = false;
            return; // nothing installed yet — silently do nothing
        }

        string hash = pool[_random.Next(pool.Count)];
        string path = Path.Combine(_assetsDir, "objects", hash[..2], hash);
        if (!File.Exists(path))
        {
            IsPlaying = false;
            return;
        }

        _reader = new VorbisWaveReader(path);
        _output = new WaveOutEvent { Volume = Volume };
        _output.Init(_reader);
        _output.PlaybackStopped += (_, _) =>
        {
            if (IsPlaying)
            {
                PlayNextTrack(); // loop the "playlist" like the vanilla menu does
            }
        };
        _output.Play();
        IsPlaying = true;
    }

    public void Dispose() => Stop();
}
