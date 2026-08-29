using System.Windows;
using System.Windows.Media;

namespace Hitboxes.Launcher.Theming;

/// <summary>
/// Shared, animatable brush instances registered once into
/// <c>Application.Current.Resources</c>. XAML references them the normal
/// way via <c>{DynamicResource ...Brush}</c>; because a WPF brush is a
/// live reference type, animating <see cref="SolidColorBrush.Color"/> (or
/// a gradient stop's Color) here updates every control using it at once —
/// that's what gives the day/night/rain switch and the glass tint change
/// a smooth cross-fade instead of an abrupt swap.
/// </summary>
public static class ThemeResources
{
    public static readonly GradientStop BackgroundTopStop = new(ThemePalettes.Day.BackgroundTop, 0);
    public static readonly GradientStop BackgroundBottomStop = new(ThemePalettes.Day.BackgroundBottom, 1);
    public static readonly SolidColorBrush PanelBrush = new(ThemePalettes.Day.Panel);
    public static readonly SolidColorBrush TextPrimaryBrush = new(ThemePalettes.Day.TextPrimary);
    public static readonly SolidColorBrush TextSecondaryBrush = new(ThemePalettes.Day.TextSecondary);
    public static readonly SolidColorBrush AccentBrush = new(ThemePalettes.Day.Accent);
    public static readonly SolidColorBrush AccentHoverBrush = new(ThemePalettes.Day.AccentHover);
    public static readonly SolidColorBrush BorderBrush = new(ThemePalettes.Day.Border);

    /// <summary>User-adjustable glass tint overlay (Settings -> glass color).</summary>
    public static readonly SolidColorBrush GlassTintBrush = new(Color.FromArgb(0x33, 0x4F, 0xA8, 0xFF));

    public static void Register(ResourceDictionary resources)
    {
        resources["BackgroundBrush"] = new LinearGradientBrush(
            new GradientStopCollection { BackgroundTopStop, BackgroundBottomStop },
            new Point(0, 0), new Point(0, 1));
        resources["PanelBrush"] = PanelBrush;
        resources["TextPrimaryBrush"] = TextPrimaryBrush;
        resources["TextSecondaryBrush"] = TextSecondaryBrush;
        resources["AccentBrush"] = AccentBrush;
        resources["AccentHoverBrush"] = AccentHoverBrush;
        resources["BorderBrush"] = BorderBrush;
        resources["GlassTintBrush"] = GlassTintBrush;
    }
}
