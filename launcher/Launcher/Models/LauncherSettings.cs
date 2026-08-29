namespace Hitboxes.Launcher.Models;

/// <summary>
/// Global defaults, applied to any instance that doesn't override them —
/// this is the launcher's own "Settings" page (Prism calls it much the
/// same thing).
/// </summary>
public sealed class LauncherSettings
{
    public string JavaExecutable { get; set; } = "javaw";
    public int DefaultMemoryMinMb { get; set; } = 512;
    public int DefaultMemoryMaxMb { get; set; } = 2048;
    public string DefaultJvmArgs { get; set; } = string.Empty;

    public bool MainMenuMusicEnabled { get; set; } = true;
    public double MainMenuMusicVolume { get; set; } = 0.5;

    public bool RainAutoDetectEnabled { get; set; } = false;
    public string? WeatherApiKey { get; set; }
    public string WeatherCity { get; set; } = "Moscow";

    /// <summary>Glass overlay tint, as "#RRGGBB". Alpha is applied separately so the glass stays translucent.</summary>
    public string GlassTintColor { get; set; } = "#4FA8FF";
}
