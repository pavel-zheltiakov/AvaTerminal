using System;
using System.Text;
using System.Threading.Tasks;

namespace AvaTerminal.Demo;

/// <summary>
/// Something to put on the screen, written straight into the parser.
/// </summary>
/// <param name="Name">The button's label.</param>
/// <param name="Description">What it demonstrates.</param>
/// <param name="Run">Draws it. A few of them take a moment, which is the point of those.</param>
public sealed record ScreenPlay(string Name, string Description, Func<AvaTerminal, Task> Run);

/// <summary>
/// The escape sequences, shown by using them.
/// </summary>
/// <remarks>
/// Every one of these goes through <see cref="AvaTerminal.Feed"/>, which puts bytes on the screen as
/// though a program had printed them and <b>sends nothing</b>. So they work identically whether the
/// terminal is running a shell on a pty, running nothing at all, or attached to a channel — which is
/// why the browser demo can show all of them.
/// </remarks>
public static class ScreenPlays
{
    private const string Esc = "\u001b";
    private const string Bel = "\u0007";

    public static ScreenPlay[] All { get; } =
    [
        new("256 colours",
            "The palette the theme resolves. Change the theme with these on screen and they are "
            + "re-coloured, because the engine stored the index and not the colour.",
            Draw(ColourCube)),

        new("Text attributes",
            "SGR: bold, dim, italic, underline, reverse, strikethrough — and the interactions, which "
            + "are where terminals disagree with each other.",
            Draw(Attributes)),

        new("Box drawing and wide text",
            "Line-drawing characters on the grid, a double-width script, an emoji and a combining "
            + "mark. Four different ways for a cell not to be one character.",
            Draw(WideText)),

        new("Set the window title",
            "OSC 0. Watch the title in the status bar and the entry in the log.",
            SetTitle),

        new("Ring the bell",
            "BEL. The control reports it and makes no sound — whether a bell is audible, a flash or "
            + "nothing at all is the application's decision.",
            Draw(Bel)),

        new("Ask the terminal a question",
            "DSR: the program asks where the cursor is and waits. The answer leaves through Input, "
            + "exactly as a keystroke does — turn on 'log what is typed' to watch it come back.",
            Draw($"Asking where the cursor is...{Esc}[6n\r\n")),

        new("A progress bar",
            "One line redrawn with a carriage return, the way every build tool does it. Nothing "
            + "scrolls and nothing is left behind.",
            Progress),

        new("The alternate screen",
            "What vim and top switch to: a second screen with no scrollback. It comes back to exactly "
            + "what was here, which is the whole point of it.",
            Alternate),
    ];

    private static Func<AvaTerminal, Task> Draw(string text) =>
        terminal =>
        {
            terminal.Feed(Encoding.UTF8.GetBytes(text));
            return Task.CompletedTask;
        };

    private static Task SetTitle(AvaTerminal terminal)
    {
        terminal.Feed(Encoding.UTF8.GetBytes($"{Esc}]0;Set by the program at {DateTime.Now:HH:mm:ss}{Bel}"));
        return Task.CompletedTask;
    }

    private static string ColourCube
    {
        get
        {
            var text = new StringBuilder("\r\n");

            text.Append("  the sixteen a program names by number\r\n  ");
            for (var i = 0; i < 16; i++)
            {
                if (i == 8) text.Append("\r\n  ");
                text.Append($"{Esc}[48;5;{i}m  {Esc}[0m");
            }

            text.Append("\r\n\r\n  the 6x6x6 cube\r\n");
            for (var row = 0; row < 6; row++)
            {
                text.Append("  ");
                for (var column = 0; column < 36; column++)
                    text.Append($"{Esc}[48;5;{16 + (row * 36) + column}m {Esc}[0m");
                text.Append("\r\n");
            }

            text.Append("\r\n  and the grey ramp\r\n  ");
            for (var i = 232; i < 256; i++) text.Append($"{Esc}[48;5;{i}m {Esc}[0m");

            return text.Append("\r\n\r\n").ToString();
        }
    }

    private static string Attributes =>
        "\r\n" +
        $"  {Esc}[1mbold{Esc}[0m   {Esc}[2mdim{Esc}[0m   {Esc}[3mitalic{Esc}[0m   " +
        $"{Esc}[4munderline{Esc}[0m   {Esc}[7mreverse{Esc}[0m   {Esc}[9mstruck out{Esc}[0m\r\n" +
        $"  {Esc}[4:3mcurly underline{Esc}[0m   {Esc}[58;5;196m{Esc}[4:3mand one in red{Esc}[0m\r\n" +
        $"  {Esc}[1;31mbold red{Esc}[0m - bright, because this theme promotes bold 0-7 to 8-15\r\n" +
        $"  {Esc}[2;31mdim red{Esc}[0m - the same colour, moved towards the background\r\n" +
        $"  {Esc}[31;7mred, reversed{Esc}[0m - the pair is swapped after bold and dim, as xterm does\r\n\r\n";

    private static string WideText =>
        "\r\n" +
        "  +---------------+----------------+\r\n" +
        "  | box drawing   | on the grid    |\r\n" +
        "  +---------------+----------------+\r\n" +
        "  | Japanese      | two cells each |\r\n" +
        "  | emoji         | two as well    |\r\n" +
        "  | combining     | one cell       |\r\n" +
        "  +---------------+----------------+\r\n" +
        "\r\n" +
        "  and the same thing drawn with the characters themselves:\r\n\r\n" +
        "  ┌───────────────┬───────────────┐\r\n" +
        "  │ 日本語のテキスト  │ two cells each │\r\n" +
        "  │ \U0001F680 \U0001F30D \U0001F41B      │ two as well    │\r\n" +
        "  │ é à ñ combining │ one cell       │\r\n" +
        "  └───────────────┴───────────────┘\r\n\r\n";

    private static async Task Progress(AvaTerminal terminal)
    {
        terminal.Feed("\r\n"u8);

        for (var percent = 0; percent <= 100; percent += 2)
        {
            var filled = percent / 2;
            var bar = new string('█', filled) + new string('░', 50 - filled);
            terminal.Feed(Encoding.UTF8.GetBytes($"\r  {Esc}[36m{bar}{Esc}[0m {percent,3}%"));
            await Task.Delay(30);
        }

        terminal.Feed(Encoding.UTF8.GetBytes($"\r\n  {Esc}[32mdone{Esc}[0m\r\n\r\n"));
    }

    private static async Task Alternate(AvaTerminal terminal)
    {
        // 1049 is "switch to the alternate screen, and remember where the cursor was".
        terminal.Feed(Encoding.UTF8.GetBytes(
            $"{Esc}[?1049h{Esc}[2J{Esc}[H" +
            $"  {Esc}[1mThis is the alternate screen.{Esc}[0m\r\n\r\n" +
            "  vim, top and every full-screen program run here.\r\n" +
            "  There is no scrollback on it, which is why resizing one is destructive.\r\n\r\n" +
            $"  {Esc}[2mgoing back in a moment...{Esc}[0m"));

        await Task.Delay(2600);

        terminal.Feed(Encoding.UTF8.GetBytes($"{Esc}[?1049l"));
    }
}
