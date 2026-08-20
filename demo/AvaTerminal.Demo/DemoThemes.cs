using System;
using AvaTerminal.Scene;

namespace AvaTerminal.Demo;

/// <summary>
/// Three palettes, to show that <see cref="Theme"/> is data rather than a setting.
/// </summary>
/// <remarks>
/// Changing one re-colours a screen that was drawn minutes ago. The engine stores what the program
/// <i>said</i> — "palette index 4" — and never what it should look like, so the theme is consulted at
/// the moment of drawing and not at the moment of parsing.
/// </remarks>
public static class DemoThemes
{
    public static Theme Dark => Theme.Dark;

    /// <summary>Light on white, with the same sixteen colours darkened enough to read on it.</summary>
    public static Theme Light { get; } = new(
        foreground:  Rgba.FromHex(0x24292F),
        background:  Rgba.FromHex(0xFFFFFF),
        cursor:      Rgba.FromHex(0x0969DA),
        cursorText:  Rgba.FromHex(0xFFFFFF),
        selection:   Rgba.FromHex(0xB6D7FF),
        ansi16:
        [
            Rgba.FromHex(0x24292F), Rgba.FromHex(0xCF222E), Rgba.FromHex(0x116329), Rgba.FromHex(0x7D4E00),
            Rgba.FromHex(0x0969DA), Rgba.FromHex(0x8250DF), Rgba.FromHex(0x1B7C83), Rgba.FromHex(0x6E7781),
            Rgba.FromHex(0x57606A), Rgba.FromHex(0xA40E26), Rgba.FromHex(0x1A7F37), Rgba.FromHex(0x9A6700),
            Rgba.FromHex(0x218BFF), Rgba.FromHex(0xA475F9), Rgba.FromHex(0x3192AA), Rgba.FromHex(0x8C959F),
        ],
        boldIsBright: false);

    /// <summary>Solarized Dark, because it is the palette people check a terminal against.</summary>
    public static Theme Solarized { get; } = new(
        foreground:  Rgba.FromHex(0x839496),
        background:  Rgba.FromHex(0x002B36),
        cursor:      Rgba.FromHex(0x93A1A1),
        cursorText:  Rgba.FromHex(0x002B36),
        selection:   Rgba.FromHex(0x073642),
        ansi16:
        [
            Rgba.FromHex(0x073642), Rgba.FromHex(0xDC322F), Rgba.FromHex(0x859900), Rgba.FromHex(0xB58900),
            Rgba.FromHex(0x268BD2), Rgba.FromHex(0xD33682), Rgba.FromHex(0x2AA198), Rgba.FromHex(0xEEE8D5),
            Rgba.FromHex(0x002B36), Rgba.FromHex(0xCB4B16), Rgba.FromHex(0x586E75), Rgba.FromHex(0x657B83),
            Rgba.FromHex(0x839496), Rgba.FromHex(0x6C71C4), Rgba.FromHex(0x93A1A1), Rgba.FromHex(0xFDF6E3),
        ]);

    public static (string Name, Func<Theme> Get)[] All { get; } =
    [
        ("Dark (the default)", () => Dark),
        ("Light", () => Light),
        ("Solarized Dark", () => Solarized),
    ];
}
