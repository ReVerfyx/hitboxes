using System.Net;
using System.Net.Http;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Live, process-wide network configuration (proxy + mirror fallback),
/// applied from LauncherSettings whenever it's loaded or saved. Exists
/// because Mojang's own endpoints (launchermeta/piston-meta/resources)
/// are unreachable from some networks (reported: a real connection
/// timeout to launchermeta.mojang.com:443) — a proxy or a mirror fixes
/// that without needing the user to touch anything outside this app.
/// </summary>
public static class NetworkSettings
{
    public static bool ProxyEnabled { get; private set; }
    public static string ProxyAddress { get; private set; } = string.Empty;
    public static string? ProxyUsername { get; private set; }
    public static string? ProxyPassword { get; private set; }
    public static bool MirrorFallbackEnabled { get; private set; } = true;

    public static void ApplyFrom(LauncherSettings settings)
    {
        ProxyEnabled = settings.ProxyEnabled;
        ProxyAddress = settings.ProxyAddress;
        ProxyUsername = settings.ProxyUsername;
        ProxyPassword = settings.ProxyPassword;
        MirrorFallbackEnabled = settings.MirrorFallbackEnabled;
    }

    /// <summary>
    /// One new client per call (cheap: it's a thin handle over a shared
    /// SocketsHttpHandler-backed connection pool) so every caller — long-
    /// lived services and the fresh GameInstaller/GameLauncher created per
    /// launch alike — always picks up whatever proxy is currently
    /// configured, instead of one baked in at construction time.
    /// </summary>
    public static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();

        if (ProxyEnabled && !string.IsNullOrWhiteSpace(ProxyAddress))
        {
            string address = ProxyAddress.Contains("://", StringComparison.Ordinal)
                ? ProxyAddress
                : $"http://{ProxyAddress}";
            try
            {
                var proxy = new WebProxy(new Uri(address));
                if (!string.IsNullOrEmpty(ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
                }
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }
            catch (UriFormatException)
            {
                // Malformed address (e.g. still being typed in Settings) —
                // fall back to no proxy rather than throwing out of a
                // client-creation call every single request would hit.
            }
        }

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>
    /// Known-good 1:1 hostname swaps for a public Mojang mirror
    /// (fastmcmirror.org) — same path/query, just a different host, so a
    /// URL built for the official host works unchanged against the mirror.
    /// Only the hosts this app actually calls directly are listed; unlisted
    /// hosts (e.g. whatever a version JSON's own library URLs point at)
    /// aren't guessed at.
    /// </summary>
    private static readonly Dictionary<string, string> MirrorHosts = new()
    {
        ["launchermeta.mojang.com"] = "launchermeta.fastmcmirror.org",
        ["piston-meta.mojang.com"] = "piston-meta.fastmcmirror.org",
        ["resources.download.minecraft.net"] = "resources.fastmcmirror.org",
    };

    /// <returns>The mirrored URL, or null if this host has no known mirror.</returns>
    public static string? TryGetMirrorUrl(string url)
    {
        if (!MirrorFallbackEnabled)
        {
            return null;
        }

        var uri = new Uri(url);
        if (!MirrorHosts.TryGetValue(uri.Host, out string? mirrorHost))
        {
            return null;
        }

        var builder = new UriBuilder(uri) { Host = mirrorHost };
        return builder.Uri.ToString();
    }
}
