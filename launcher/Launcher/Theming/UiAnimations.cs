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
        var transform = new TranslateTransform(0, slideFromY);
        element.RenderTransform = transform;

        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
        var ease = new CubicEase { EasingMode = EasingMode.Out };

        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(slideFromY, 0, duration) { EasingFunction = ease });
    }

    /// <summary>Wires a gentle scale-up "lift" on hover for glass cards (instance tiles).</summary>
    public static void AttachHoverLift(FrameworkElement element, double scale = 1.035)
    {
        if (element.RenderTransform is not ScaleTransform)
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new ScaleTransform(1, 1);
        }

        var duration = new Duration(TimeSpan.FromMilliseconds(160));
        element.MouseEnter += (_, _) => AnimateScale((ScaleTransform)element.RenderTransform, scale, duration);
        element.MouseLeave += (_, _) => AnimateScale((ScaleTransform)element.RenderTransform, 1.0, duration);
    }

    private static void AnimateScale(ScaleTransform transform, double to, Duration duration)
    {
        var ease = new CubicEase { EasingMode = EasingMode.Out };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, duration) { EasingFunction = ease });
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, duration) { EasingFunction = ease });
    }
}
