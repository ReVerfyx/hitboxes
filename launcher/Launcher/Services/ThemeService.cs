using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Hitboxes.Launcher.Theming;

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
/// launcher is left open. Theme changes are cross-faded via
/// <see cref="ThemeResources"/>'s shared brushes rather than swapped
/// instantly.
/// </summary>
public sealed class ThemeService
{
    private static readonly Duration TransitionDuration = new(TimeSpan.FromMilliseconds(700));
    private static readonly IEasingFunction Easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

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
        await EvaluateThemeAsync(animate: false);
        _timer.Start();
    }

    public void SetManualRain(bool enabled)
    {
        _manualRainOverride = enabled;
        _ = EvaluateThemeAsync();
    }

    /// <summary>Recolors the shared glass tint brush; called from Settings when the user picks a new glass color.</summary>
    public static void ApplyGlassTint(Color color)
    {
        AnimateBrush(ThemeResources.GlassTintBrush, color);
    }

    private async Task EvaluateThemeAsync(bool animate = true)
    {
        bool isRaining = _manualRainOverride;

        if (!isRaining && RainAutoDetectEnabled && !string.IsNullOrWhiteSpace(WeatherApiKey))
        {
            isRaining = await WeatherService.IsRainingAsync(WeatherCity, WeatherApiKey);
        }

        AppTheme theme = isRaining ? AppTheme.Rain : IsDaytime() ? AppTheme.Day : AppTheme.Night;
        if (theme == CurrentTheme && animate)
        {
            return;
        }

        CurrentTheme = theme;
        ApplyPalette(theme switch
        {
            AppTheme.Day => ThemePalettes.Day,
            AppTheme.Night => ThemePalettes.Night,
            AppTheme.Rain => ThemePalettes.Rain,
            _ => ThemePalettes.Day
        }, animate);
        ThemeChanged?.Invoke(this, theme);
    }

    private static bool IsDaytime()
    {
        int hour = DateTime.Now.Hour;
        return hour is >= 6 and < 20;
    }

    private static void ApplyPalette(ThemePalette palette, bool animate)
    {
        AnimateColor(ThemeResources.BackgroundTopStop, GradientStop.ColorProperty, palette.BackgroundTop, animate);
        AnimateColor(ThemeResources.BackgroundBottomStop, GradientStop.ColorProperty, palette.BackgroundBottom, animate);
        AnimateBrush(ThemeResources.PanelBrush, palette.Panel, animate);
        AnimateBrush(ThemeResources.TextPrimaryBrush, palette.TextPrimary, animate);
        AnimateBrush(ThemeResources.TextSecondaryBrush, palette.TextSecondary, animate);
        AnimateBrush(ThemeResources.AccentBrush, palette.Accent, animate);
        AnimateBrush(ThemeResources.AccentHoverBrush, palette.AccentHover, animate);
        AnimateBrush(ThemeResources.BorderBrush, palette.Border, animate);
    }

    private static void AnimateBrush(SolidColorBrush brush, Color target, bool animate = true)
        => AnimateColor(brush, SolidColorBrush.ColorProperty, target, animate);

    private static void AnimateColor(Animatable target, System.Windows.DependencyProperty property, Color color, bool animate = true)
    {
        if (!animate)
        {
            target.SetValue(property, color);
            return;
        }

        var animation = new ColorAnimation(color, TransitionDuration) { EasingFunction = Easing };
        target.BeginAnimation(property, animation);
    }
}
