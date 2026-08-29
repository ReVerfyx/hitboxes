using System.Windows;
using System.Windows.Threading;

namespace Hitboxes.Launcher.Services;

public enum AppTheme
{
    Day,
    Night,
    Rain
}

/// <summary>
/// Picks Day/Night automatically from the system clock and lets the user
/// force a Rain overlay (manually, or automatically if they supply an
/// OpenWeatherMap API key in settings — see <see cref="RainAutoDetectEnabled"/>).
/// Re-evaluates every minute so the theme drifts with real time while the
/// launcher is left open.
/// </summary>
public sealed class ThemeService
{
    private readonly DispatcherTimer _timer;
    private bool _manualRainOverride;

    public bool RainAutoDetectEnabled { get; set; } = false;
    public string? WeatherApiKey { get; set; }
    public string WeatherCity { get; set; } = "Moscow";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Day;

    public event EventHandler<AppTheme>? ThemeChanged;

    public ThemeService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (_, _) => await EvaluateThemeAsync();
    }

    public async Task StartAsync()
    {
        await EvaluateThemeAsync();
        _timer.Start();
    }

    public void SetManualRain(bool enabled)
    {
        _manualRainOverride = enabled;
        _ = EvaluateThemeAsync();
    }

    private async Task EvaluateThemeAsync()
    {
        bool isRaining = _manualRainOverride;

        if (!isRaining && RainAutoDetectEnabled && !string.IsNullOrWhiteSpace(WeatherApiKey))
        {
            isRaining = await WeatherService.IsRainingAsync(WeatherCity, WeatherApiKey);
        }

        AppTheme theme = isRaining ? AppTheme.Rain : IsDaytime() ? AppTheme.Day : AppTheme.Night;
        if (theme == CurrentTheme)
        {
            return;
        }

        CurrentTheme = theme;
        ApplyThemeDictionary(theme);
        ThemeChanged?.Invoke(this, theme);
    }

    private static bool IsDaytime()
    {
        int hour = DateTime.Now.Hour;
        return hour is >= 6 and < 20;
    }

    private static void ApplyThemeDictionary(AppTheme theme)
    {
        string fileName = theme switch
        {
            AppTheme.Day => "Themes/Day.xaml",
            AppTheme.Night => "Themes/Night.xaml",
            AppTheme.Rain => "Themes/Rain.xaml",
            _ => "Themes/Day.xaml"
        };

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(fileName, UriKind.Relative)
        };

        var appResources = Application.Current.Resources.MergedDictionaries;

        for (int i = appResources.Count - 1; i >= 0; i--)
        {
            var source = appResources[i].Source?.OriginalString ?? string.Empty;
            if (source.StartsWith("Themes/"))
            {
                appResources.RemoveAt(i);
            }
        }

        appResources.Add(dictionary);
    }
}
