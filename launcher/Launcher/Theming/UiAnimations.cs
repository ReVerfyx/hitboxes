using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Hitboxes.Launcher.Theming;

/// <summary>Small, reusable animation helpers so windows/cards don't each hand-roll a Storyboard.</summary>
public static class UiAnimations
{
    /// <summary>Fades an element in (and optionally slides it up slightly) once it's loaded — used for window entrances and instance cards appearing.</summary>
    public static void FadeIn(UIElement element, double durationMs = 350, double slideFromY = 12)
    {
        element.Opacity = 0;

        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });

        // Window is backed by a real HWND and doesn't support RenderTransform
        // ("Transform is not valid for Window") — fade it in place instead
        // of also sliding it. Everything else (instance cards, etc.) still
        // gets the slide-up.
        if (element is Window)
        {
            return;
        }

        var transform = new TranslateTransform(0, slideFromY);
        element.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(slideFromY, 0, duration) { EasingFunction = ease });
    }

    /// <summary>Wires a gentle scale-up "lift" on hover for glass cards (instance tiles).</summary>
    public static void AttachHoverLift(FrameworkElement element, double scale = 1.035)
    {
        // Always assign a fresh, local ScaleTransform rather than reusing
        // whatever RenderTransform a Style Setter may already have put there
        // (e.g. GlassCardStyle) — a Setter-provided Freezable can end up
        // frozen by the time this runs, which would throw the instant
        // BeginAnimation touches it. A local value always wins over a Style
        // setter anyway, so this changes nothing visually.
        var transform = new ScaleTransform(1, 1);
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = transform;

        var duration = new Duration(TimeSpan.FromMilliseconds(160));
        element.MouseEnter += (_, _) => AnimateScale(transform, scale, duration);
        element.MouseLeave += (_, _) => AnimateScale(transform, 1.0, duration);
    }

    private static void AnimateScale(ScaleTransform transform, double to, Duration duration)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, duration) { EasingFunction = ease });
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, duration) { EasingFunction = ease });
    }

    /// <summary>Crossfades between two sibling views sharing the same Grid cell — used for the sidebar's Home/Instances switch.</summary>
    public static void CrossFadeSwitch(UIElement showing, UIElement hiding, double durationMs = 220)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
        var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

        if (hiding.Visibility == Visibility.Visible)
        {
            var hideAnim = new DoubleAnimation(hiding.Opacity, 0, duration) { EasingFunction = easeOut };
            hideAnim.Completed += (_, _) => hiding.Visibility = Visibility.Collapsed;
            hiding.BeginAnimation(UIElement.OpacityProperty, hideAnim);
        }

        showing.Visibility = Visibility.Visible;
        showing.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(showing.Opacity, 1, duration) { EasingFunction = easeOut });
    }

    /// <summary>Starts a slow opacity pulse (breathing effect) — used to show a button is mid-action (launching a game instance).</summary>
    public static void StartPulse(UIElement element)
    {
        var pulse = new DoubleAnimation(1.0, 0.55, new Duration(TimeSpan.FromMilliseconds(650)))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        element.BeginAnimation(UIElement.OpacityProperty, pulse);
    }

    /// <summary>Stops a pulse started by <see cref="StartPulse"/> and settles back to fully opaque.</summary>
    public static void StopPulse(UIElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.SetValue(UIElement.OpacityProperty, 1.0);
    }
}
