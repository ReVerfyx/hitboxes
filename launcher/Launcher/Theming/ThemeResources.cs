using System.Windows;
using System.Windows.Media;

namespace Hitboxes.Launcher.Theming;

/// <summary>
/// Shared, animatable brush instances, registered into each Window's own
/// (non-frozen) <c>Resources</c> — deliberately never into
/// <c>Application.Resources</c>, since WPF auto-freezes Freezable
/// resources added there, which would make them impossible to animate.
/// XAML references them the normal way via <c>{DynamicResource ...Brush}</c>;
/// because a WPF brush is a live reference type, animating
/// <see cref="SolidColorBrush.Color"/> (or a gradient stop's Color) here
/// updates every control using it at once — that's what gives the
/// day/night/rain switch and the glass tint change a smooth cross-fade
/// instead of an abrupt swap. Every brush (including the gradient and its
/// stops) is built exactly once as a singleton: constructing a fresh
/// <see cref="GradientStopCollection"/> per window would try to give the
/// same shared <see cref="GradientStop"/> instances multiple simultaneous
/// inheritance contexts, which WPF rejects.
/// </summary>
public static class ThemeResources
{
    // WPF freezes a shared Freezable (brush/gradient stop) once it has
    // actually been used to paint something during a real render pass —
    // after that, even AnimateColor's plain SetValue silently no-ops (see
    // its IsFrozen guard below). Since these brushes get consumed by many
    // elements the instant the first window lays out, ThemeService's
    // "apply the real Day/Night/Rain palette" call in Loaded almost always
    // loses that race and never visibly takes effect. Sidestep it
    // entirely by seeding the *static* initial value with the real
    // current-time palette instead of always Day — that value is what
    // actually survives, so it needs to be correct from the start.
    private static readonly ThemePalette InitialPalette =
        DateTime.Now.Hour is >= 6 and < 20 ? ThemePalettes.Day : ThemePalettes.Night;

    public static readonly GradientStop BackgroundTopStop = new(InitialPalette.BackgroundTop, 0);
    public static readonly GradientStop BackgroundBottomStop = new(InitialPalette.BackgroundBottom, 1);
    public static readonly LinearGradientBrush BackgroundBrush = new(
        new GradientStopCollection { BackgroundTopStop, BackgroundBottomStop },
        new Point(0, 0), new Point(0, 1));
    public static readonly SolidColorBrush PanelBrush = new(InitialPalette.Panel);
    public static readonly SolidColorBrush SurfaceBrush = new(InitialPalette.Surface);
    public static readonly SolidColorBrush TextPrimaryBrush = new(InitialPalette.TextPrimary);
    public static readonly SolidColorBrush TextSecondaryBrush = new(InitialPalette.TextSecondary);
    public static readonly SolidColorBrush AccentBrush = new(InitialPalette.Accent);
    public static readonly SolidColorBrush AccentHoverBrush = new(InitialPalette.AccentHover);
    public static readonly SolidColorBrush BorderBrush = new(InitialPalette.Border);

    /// <summary>User-adjustable accent-color wash overlay (Settings -> "Цвет стекла"). Kept subtle now that
    /// panels are opaque HUD-style rather than frosted glass — it's a tint, not the whole look.</summary>
    public static readonly SolidColorBrush GlassTintBrush = new(Color.FromArgb(0x18, 0x4F, 0xA8, 0xFF));

    public static void Register(ResourceDictionary resources)
    {
        resources["BackgroundBrush"] = BackgroundBrush;
        resources["PanelBrush"] = PanelBrush;
        resources["SurfaceBrush"] = SurfaceBrush;
        resources["TextPrimaryBrush"] = TextPrimaryBrush;
        resources["TextSecondaryBrush"] = TextSecondaryBrush;
        resources["AccentBrush"] = AccentBrush;
        resources["AccentHoverBrush"] = AccentHoverBrush;
        resources["BorderBrush"] = BorderBrush;
        resources["GlassTintBrush"] = GlassTintBrush;
    }
}
