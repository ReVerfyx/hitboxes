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
    // "Liquid Glass" palettes: Day is a genuinely light, milky-white/silver
    // surface (dark ink on frosted glass); Night is graphite/near-black
    // (light ink on dark frosted glass). The ReVerfyx brand accent — a
    // violet-blue — stays IDENTICAL across every theme so it reads as a
    // fixed brand color rather than something that drifts with the clock;
    // only the neutrals (background/panel/surface/text/border) change.
    private static readonly Color BrandAccent = Color.FromRgb(0x7B, 0x68, 0xF5);
    private static readonly Color BrandAccentHover = Color.FromRgb(0x93, 0x84, 0xFF);

    public static readonly ThemePalette Day = new(
        BackgroundTop: Color.FromRgb(0xF7, 0xF6, 0xFB),
        BackgroundBottom: Color.FromRgb(0xE7, 0xE5, 0xF1),
        Panel: Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF),
        Surface: Color.FromArgb(0x1C, 0x25, 0x22, 0x40),
        TextPrimary: Color.FromRgb(0x1D, 0x1B, 0x24),
        TextSecondary: Color.FromRgb(0x6E, 0x6C, 0x7C),
        Accent: BrandAccent,
        AccentHover: BrandAccentHover,
        Border: Color.FromArgb(0x2C, 0x20, 0x1E, 0x35));

    public static readonly ThemePalette Night = new(
        BackgroundTop: Color.FromRgb(0x17, 0x16, 0x1E),
        BackgroundBottom: Color.FromRgb(0x0A, 0x0A, 0x10),
        Panel: Color.FromArgb(0xB8, 0x18, 0x17, 0x20),
        Surface: Color.FromArgb(0x99, 0x2A, 0x2A, 0x38),
        TextPrimary: Color.FromRgb(0xF2, 0xF0, 0xFA),
        TextSecondary: Color.FromRgb(0x9E, 0x9B, 0xB0),
        Accent: BrandAccent,
        AccentHover: BrandAccentHover,
        Border: Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF));

    public static readonly ThemePalette Rain = new(
        BackgroundTop: Color.FromRgb(0x23, 0x2A, 0x38),
        BackgroundBottom: Color.FromRgb(0x12, 0x16, 0x1D),
        Panel: Color.FromArgb(0xB8, 0x18, 0x1C, 0x24),
        Surface: Color.FromArgb(0x99, 0x28, 0x30, 0x3A),
        TextPrimary: Color.FromRgb(0xE7, 0xED, 0xF2),
        TextSecondary: Color.FromRgb(0x95, 0xA2, 0xAC),
        Accent: BrandAccent,
        AccentHover: BrandAccentHover,
        Border: Color.FromArgb(0x34, 0xFF, 0xFF, 0xFF));
}
