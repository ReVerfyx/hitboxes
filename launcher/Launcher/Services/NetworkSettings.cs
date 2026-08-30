using System.Net;
using System.Net.Http;
using Hitboxes.Launcher.Models;

namespace Hitboxes.Launcher.Services;

/// <summary>
/// Live, process-wide network configuration (proxy + mirror fallback),
/// applied from LauncherSettings whenever it's loaded or saved. Exists
/// because Mojang's own endpoints (launchermeta/piston-meta/resources)
/// are unreachable from some networks (reported: a real connection
/// timeout to launchermeta.mojang.com:443). The proxy is the general fix
/// (works for any host); mirror hosts are user-supplied text in Settings
/// → Сеть rather than anything hardcoded here — a mirror this code can't
/// itself verify (no real internet access from where it runs) is worse
/// than no mirror if it's silently wrong.
/// </summary>
public static class NetworkSettings
{
    public static bool ProxyEnabled { get; private set; }
    public static string ProxyAddress { get; private set; } = string.Empty;
    public static string? ProxyUsername { get; private set; }
    public static string? ProxyPassword { get; private set; }
    public static bool MirrorFallbackEnabled { get; private set; } = true;
    private static Dictionary<string, string> _customMirrorHosts = new();

    public static void ApplyFrom(LauncherSettings settings)
    {
        ProxyEnabled = settings.ProxyEnabled;
        ProxyAddress = settings.ProxyAddress;
        ProxyUsername = settings.ProxyUsername;
        ProxyPassword = settings.ProxyPassword;
        MirrorFallbackEnabled = settings.MirrorFallbackEnabled;
        _customMirrorHosts = ParseMirrorOverrides(settings.MirrorOverrides);
    }

    /// <summary>
    /// "host1=mirror1,host2=mirror2" -> a lookup table. No mirror is
    /// verifiable from where this code runs (no real internet access in
    /// this dev/CI environment), so nothing is hardcoded as a default —
    /// this is entirely what the user pastes into Settings → Сеть
    /// themselves after confirming a candidate mirror actually answers
    /// (e.g. by opening its version-manifest URL in a browser).
    /// </summary>
    private static Dictionary<string, string> ParseMirrorOverrides(string raw)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                map[parts[0]] = parts[1];
            }
        }
        return map;
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

    /// <returns>The mirrored URL, or null if this host has no configured mirror.</returns>
    public static string? TryGetMirrorUrl(string url)
    {
        if (!MirrorFallbackEnabled)
        {
            return null;
        }

        var uri = new Uri(url);
        if (!_customMirrorHosts.TryGetValue(uri.Host, out string? mirrorHost))
        {
            return null;
        }

        var builder = new UriBuilder(uri) { Host = mirrorHost };
        return builder.Uri.ToString();
    }
}
