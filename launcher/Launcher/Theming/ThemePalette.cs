using System.Windows.Media;

namespace Hitboxes.Launcher.Theming;

/// <summary>Color set for one time-of-day/weather theme, animated onto the shared brushes in <see cref="ThemeResources"/>.</summary>
public readonly record struct ThemePalette(
    Color BackgroundTop,
    Color BackgroundBottom,
    Color Panel,
    Color TextPrimary,
    Color TextSecondary,
    Color Accent,
    Color AccentHover,
    Color Border);

public static class ThemePalettes
{
    // A punchy, game-menu look (chunky HUD panels, vivid sky, one consistent
    // green "brand" accent across all three times of day) instead of the
    // earlier frosted-glass treatment.
    public static readonly ThemePalette Day = new(
        BackgroundTop: Color.FromRgb(0x5E, 0xBE, 0xFF),
        BackgroundBottom: Color.FromRgb(0xC7, 0xEC, 0xFF),
        Panel: Color.FromArgb(0xE8, 0x14, 0x19, 0x22),
        TextPrimary: Color.FromRgb(0xF5, 0xF7, 0xFA),
        TextSecondary: Color.FromRgb(0xAC, 0xB7, 0xC6),
        Accent: Color.FromRgb(0x4C, 0xAF, 0x34),
        AccentHover: Color.FromRgb(0x63, 0xC9, 0x3F),
        Border: Color.FromRgb(0x0B, 0x0D, 0x12));

    public static readonly ThemePalette Night = new(
        BackgroundTop: Color.FromRgb(0x0B, 0x10, 0x2A),
        BackgroundBottom: Color.FromRgb(0x24, 0x2A, 0x4C),
        Panel: Color.FromArgb(0xE8, 0x12, 0x14, 0x1F),
        TextPrimary: Color.FromRgb(0xEA, 0xF0, 0xFF),
        TextSecondary: Color.FromRgb(0x9A, 0xA6, 0xD6),
        Accent: Color.FromRgb(0x4C, 0xAF, 0x34),
        AccentHover: Color.FromRgb(0x63, 0xC9, 0x3F),
        Border: Color.FromRgb(0x05, 0x06, 0x0C));

    public static readonly ThemePalette Rain = new(
        BackgroundTop: Color.FromRgb(0x49, 0x59, 0x66),
        BackgroundBottom: Color.FromRgb(0x84, 0x95, 0xA1),
        Panel: Color.FromArgb(0xE8, 0x16, 0x1A, 0x21),
        TextPrimary: Color.FromRgb(0xE7, 0xEE, 0xF2),
        TextSecondary: Color.FromRgb(0xAF, 0xC0, 0xC9),
        Accent: Color.FromRgb(0x4C, 0xAF, 0x34),
        AccentHover: Color.FromRgb(0x63, 0xC9, 0x3F),
        Border: Color.FromRgb(0x08, 0x09, 0x0D));
}
