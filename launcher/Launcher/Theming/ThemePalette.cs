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
    public static readonly ThemePalette Day = new(
        BackgroundTop: Color.FromArgb(0xCC, 0x9F, 0xD8, 0xF2),
        BackgroundBottom: Color.FromArgb(0xCC, 0xE9, 0xF6, 0xFC),
        Panel: Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF),
        TextPrimary: Color.FromRgb(0x14, 0x22, 0x2B),
        TextSecondary: Color.FromRgb(0x46, 0x5C, 0x68),
        Accent: Color.FromRgb(0x2E, 0x9C, 0xCA),
        AccentHover: Color.FromRgb(0x22, 0x7D, 0xA3),
        Border: Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));

    public static readonly ThemePalette Night = new(
        BackgroundTop: Color.FromArgb(0xCC, 0x0B, 0x10, 0x26),
        BackgroundBottom: Color.FromArgb(0xCC, 0x22, 0x27, 0x45),
        Panel: Color.FromArgb(0x90, 0x2A, 0x2F, 0x52),
        TextPrimary: Color.FromRgb(0xEA, 0xF0, 0xFF),
        TextSecondary: Color.FromRgb(0x9A, 0xA6, 0xD6),
        Accent: Color.FromRgb(0x7C, 0x6F, 0xE0),
        AccentHover: Color.FromRgb(0x9C, 0x90, 0xFF),
        Border: Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));

    public static readonly ThemePalette Rain = new(
        BackgroundTop: Color.FromArgb(0xCC, 0x46, 0x57, 0x63),
        BackgroundBottom: Color.FromArgb(0xCC, 0x74, 0x86, 0x93),
        Panel: Color.FromArgb(0x90, 0x3F, 0x4C, 0x56),
        TextPrimary: Color.FromRgb(0xE7, 0xEE, 0xF2),
        TextSecondary: Color.FromRgb(0xAF, 0xC0, 0xC9),
        Accent: Color.FromRgb(0x4F, 0xA3, 0xC4),
        AccentHover: Color.FromRgb(0x6F, 0xC0, 0xE0),
        Border: Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
}
