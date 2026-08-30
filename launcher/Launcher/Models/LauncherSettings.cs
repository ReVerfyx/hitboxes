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
}
