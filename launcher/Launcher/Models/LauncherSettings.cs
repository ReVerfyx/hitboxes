namespace Hitboxes.Launcher.Models;

public sealed class LauncherSettings
{
    public List<Account> Accounts { get; set; } = new();
    public string? CurrentAccountId { get; set; }

    /// <summary>Not persisted (no setter) — computed from Accounts/CurrentAccountId on every read.</summary>
    public Account? CurrentAccount =>
        Accounts.FirstOrDefault(a => a.Id == CurrentAccountId) ?? Accounts.FirstOrDefault();

    public string JavaExecutable { get; set; } = "javaw";

    // Prism-style UI exposes one memory value. Xms stays conservative while
    // Xmx follows this selected value.
    public int DefaultMemoryMinMb { get; set; } = 512;
    public int DefaultMemoryMaxMb { get; set; } = 4096;
    public string DefaultJvmArgs { get; set; } = string.Empty;

    public bool MainMenuMusicEnabled { get; set; } = true;
    public double MainMenuMusicVolume { get; set; } = 0.5;
    public bool RainAutoDetectEnabled { get; set; } = false;
    public string? WeatherApiKey { get; set; }
    public string WeatherCity { get; set; } = "Moscow";
    public string GlassTintColor { get; set; } = "#8B7CFF";

    // Some networks can't reach Mojang's endpoints directly (reported: a
    // real connection timeout to launchermeta.mojang.com) — a proxy fixes
    // that for every request this app makes; the mirror fallback below is
    // a narrower fix scoped to just the Mojang hosts this app calls.
    public bool ProxyEnabled { get; set; } = false;
    public string ProxyAddress { get; set; } = string.Empty;
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
    public bool MirrorFallbackEnabled { get; set; } = true;
}
