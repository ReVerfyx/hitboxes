using System.Windows.Media;

namespace Hitboxes.Launcher.Theming;

/// <summary>Color set for one time-of-day/weather theme, animated onto the shared brushes in <see cref="ThemeResources"/>.</summary>
public readonly record struct ThemePalette(
    Color BackgroundTop,
    Color BackgroundBottom,
    Color Panel,
    Color Surface,
    Color TextPrimary,
    Color TextSecondary,
    Color Accent,
    Color AccentHover,
    Color Border);

public static class ThemePalettes
{
    // Deep-graphite dark mode / quality silver-white light mode, both used
    // sparingly-translucent ("moderate glass" — a bordered, softly-shadowed
    // card, not a flat gray box) rather than heavy frosted panels. The
    // ReVerfyx brand accent (violet-blue, with a deeper indigo for gradient
    // ends / pressed states) stays IDENTICAL across every theme so it reads
    // as a fixed brand color rather than something that drifts with the clock.
    private static readonly Color BrandAccent = Color.FromRgb(0x8B, 0x7C, 0xFF);
    private static readonly Color BrandAccentDeep = Color.FromRgb(0x66, 0x58, 0xE8);

    public static readonly ThemePalette Day = new(
        BackgroundTop: Color.FromRgb(0xF8, 0xF8, 0xFB),
        BackgroundBottom: Color.FromRgb(0xEC, 0xEE, 0xF4),
        Panel: Color.FromArgb(0xF0, 0xFF, 0xFF, 0xFF),
        Surface: Color.FromArgb(0x12, 0x10, 0x10, 0x22),
        TextPrimary: Color.FromRgb(0x1B, 0x1B, 0x21),
        TextSecondary: Color.FromRgb(0x6B, 0x6B, 0x76),
        Accent: BrandAccent,
        AccentHover: BrandAccentDeep,
        Border: Color.FromArgb(0x16, 0x00, 0x00, 0x00));

    public static readonly ThemePalette Night = new(
        BackgroundTop: Color.FromRgb(0x0D, 0x0E, 0x13),
        BackgroundBottom: Color.FromRgb(0x08, 0x09, 0x0D),
        Panel: Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF),
        Surface: Color.FromArgb(0x66, 0x00, 0x00, 0x00),
        TextPrimary: Color.FromRgb(0xF5, 0xF5, 0xF7),
        TextSecondary: Color.FromRgb(0x8B, 0x8E, 0x98),
        Accent: BrandAccent,
        AccentHover: BrandAccentDeep,
        Border: Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));

    public static readonly ThemePalette Rain = new(
        BackgroundTop: Color.FromRgb(0x12, 0x16, 0x1F),
        BackgroundBottom: Color.FromRgb(0x0A, 0x0D, 0x13),
        Panel: Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        Surface: Color.FromArgb(0x66, 0x00, 0x00, 0x00),
        TextPrimary: Color.FromRgb(0xE9, 0xED, 0xF3),
        TextSecondary: Color.FromRgb(0x93, 0xA0, 0xAC),
        Accent: BrandAccent,
        AccentHover: BrandAccentDeep,
        Border: Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
}
