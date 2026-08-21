using System;
using System.Text;
using System.Threading.Tasks;
using AvaTerminal.Pty;
using AvaTerminal.Scene;

namespace AvaTerminal.Demo;

/// <summary>
/// The guided tour: seven short scenarios that between them use every part of the API.
/// </summary>
/// <remarks>
/// Written for somebody who has never embedded a terminal. Each step says what it is doing and why in
/// plain words, does it to the terminal in the middle of the window, and shows the code that did it —
/// which is real code, taken from this file rather than paraphrased.
/// </remarks>
public static class Scenarios
{
    private const string Esc = "\u001b";
    private const string Bel = "\u0007";

    public static Scenario[] All { get; } =
    [
        FirstTerminal, Printing, Typing, Looks, Sizing, Listening, RunningAProgram,
    ];

    // ---- 1 -----------------------------------------------------------------------------------------

    private static Scenario FirstTerminal => new(
        "1 · Your first terminal",
        "What the control is, what it is not, and what you get for one line of code.",
        [
            new Step(
                "One line",
                "That is the whole of the minimum case. The control you are looking at was made exactly "
                + "like this — no command, no configuration, and nothing installed on the machine. What it "
                + "can already do: draw anything a program could print, and turn your keystrokes back into "
                + "the bytes a program would read.",
                """
                Content = new AvaTerminal();
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "Hello. This is a real terminal.\r\n\r\n");
                    await Type(context, "Nothing has been started in it — that is the next few steps.\r\n");
                }),

            new Step(
                "Three things are called \"terminal\"",
                "Knowing which is which makes everything else obvious. A pty is a kernel object; a shell "
                + "is an ordinary program; a terminal emulator parses, draws and encodes. This control is "
                + "the third one. The other two are optional.",
                """
                // a pty       the kernel's - a pipe with terminal manners
                // a shell     an ordinary program, and here optional
                // a terminal  parses bytes into a screen, draws it, encodes keys back  <- this
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, $"{Esc}[1mThree different things, one word{Esc}[0m\r\n\r\n");
                    await Type(context, $"  {Esc}[36ma pty{Esc}[0m        a kernel object. A pipe with terminal manners:\r\n");
                    await Type(context, "               echo, line editing, Ctrl-C to SIGINT, a window size.\r\n");
                    await Type(context, "               It renders nothing and parses nothing.\r\n\r\n");
                    await Type(context, $"  {Esc}[36ma shell{Esc}[0m      an ordinary interactive program. Not a terminal,\r\n");
                    await Type(context, "               and here it is optional.\r\n\r\n");
                    await Type(context, $"  {Esc}[32ma terminal{Esc}[0m   parses the byte stream into a screen, draws that\r\n");
                    await Type(context, "               screen, encodes your keys back into bytes.\r\n");
                    await Type(context, $"               {Esc}[32mThat is this control.{Esc}[0m\r\n");
                }),

            new Step(
                "Nothing is running, and that is fine",
                "AutoStart is off in this demo, so no process was spawned and no pty was opened. A "
                + "terminal with nothing behind it is still a terminal: you can draw to it and read from "
                + "it. That is the mode the rest of this tour uses, and it is the one you want when the "
                + "far end is an SSH channel rather than a local program.",
                """
                var terminal = new AvaTerminal { AutoStart = false };

                terminal.IsRunning;   // false
                terminal.Pid;         // null
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, $"IsRunning   {Answer(context.Terminal.IsRunning)}\r\n");
                    await Type(context, $"Pid         {context.Terminal.Pid?.ToString() ?? Dim("null")}\r\n");
                    await Type(context, $"Columns     {context.Terminal.Columns}\r\n");
                    await Type(context, $"Rows        {context.Terminal.Rows}\r\n\r\n");
                    await Type(context, "Everything above was written by the demo with Feed.\r\n");
                }),
        ]);

    // ---- 2 -----------------------------------------------------------------------------------------

    private static Scenario Printing => new(
        "2 · Printing to it",
        "Feed puts bytes on the screen as though a program had printed them. Colour, attributes, moving "
        + "the cursor and redrawing in place are all just bytes.",
        [
            new Step(
                "Feed some text",
                "Feed takes the bytes a program would have written. It sends nothing anywhere — it is the "
                + "way in to the screen, not the way out of the keyboard. Note the CR LF: a terminal moves "
                + "down on LF and back to column one on CR, so a line ending in LF alone leaves the cursor "
                + "where it was.",
                """
                terminal.Feed("Hello.\r\n"u8);
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "Hello.\r\n");
                    await Type(context, "A second line, because the first one ended in CR LF.\r\n");
                    await Type(context, "This one ends in LF only —\nso the next starts where it left off.\r\n");
                }),

            new Step(
                "Colour",
                "An escape sequence is an instruction hidden in the stream. ESC [ 31 m means \"draw in "
                + "colour 1\", ESC [ 0 m means \"back to normal\". The engine stores the palette index and "
                + "not the colour, which is why changing the theme re-colours text drawn minutes ago.",
                """
                terminal.Feed("\u001b[31mred\u001b[0m and \u001b[32mgreen\u001b[0m\r\n"u8);

                // 256 colours: ESC [ 38;5;<n> m for the foreground, 48 for the background
                terminal.Feed("\u001b[38;5;208morange\u001b[0m\r\n"u8);
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, $"{Esc}[31mred{Esc}[0m  {Esc}[32mgreen{Esc}[0m  {Esc}[33myellow{Esc}[0m  {Esc}[34mblue{Esc}[0m  {Esc}[35mmagenta{Esc}[0m  {Esc}[36mcyan{Esc}[0m\r\n\r\n");
                    await Type(context, "and the 6x6x6 cube of the 256-colour palette:\r\n\r\n");

                    for (var row = 0; row < 6; row++)
                    {
                        var line = new StringBuilder("  ");
                        for (var column = 0; column < 36; column++)
                            line.Append($"{Esc}[48;5;{16 + (row * 36) + column}m {Esc}[0m");
                        await Type(context, line.Append("\r\n").ToString());
                    }

                    await Type(context, "\r\nLeave this on screen and change the theme in the Sandbox.\r\n");
                }),

            new Step(
                "Bold, underline, and the rest",
                "The same ESC [ … m instruction carries the attributes, and they combine. The order they "
                + "are resolved in matters: bold may brighten a colour, dim then moves it towards the "
                + "background, and reverse swaps the finished pair — the order xterm uses, and the reason "
                + "programs look here the way they look there.",
                """
                terminal.Feed("\u001b[1mbold\u001b[0m \u001b[4munderline\u001b[0m \u001b[7mreverse\u001b[0m"u8);
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, $"  {Esc}[1mbold{Esc}[0m   {Esc}[2mdim{Esc}[0m   {Esc}[3mitalic{Esc}[0m   {Esc}[4munderline{Esc}[0m   {Esc}[7mreverse{Esc}[0m   {Esc}[9mstruck out{Esc}[0m\r\n\r\n");
                    await Type(context, $"  {Esc}[1;31mbold red{Esc}[0m      bright, because bold promotes colours 0-7 to 8-15\r\n");
                    await Type(context, $"  {Esc}[2;31mdim red{Esc}[0m       the same colour, moved towards the background\r\n");
                    await Type(context, $"  {Esc}[31;7mred reversed{Esc}[0m  the pair swapped, after bold and dim were applied\r\n");
                }),

            new Step(
                "Moving the cursor, and erasing",
                "A terminal is a grid you can write anywhere on. ESC [ row ; column H puts the cursor "
                + "somewhere, ESC [ 2 J erases the screen, ESC [ 0 J erases from here down. This is how "
                + "every full-screen program draws — there is no other mechanism.",
                """
                terminal.Feed("\u001b[2J"u8);        // erase the screen
                terminal.Feed("\u001b[5;20H"u8);     // row 5, column 20
                terminal.Feed("here"u8);
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "watch:");
                    await Task.Delay(500);

                    for (var i = 0; i < 8; i++)
                    {
                        Write(context, $"{Esc}[{3 + i};{6 + (i * 5)}H{Esc}[3{(i % 6) + 1}mrow {3 + i}{Esc}[0m");
                        await Task.Delay(160);
                    }

                    await Task.Delay(500);
                    Write(context, $"{Esc}[13;1Hand everything below this line is erased:{Esc}[0J");
                }),

            new Step(
                "Redrawing one line in place",
                "A carriage return with no line feed moves the cursor back to column one without moving "
                + "down, so what is written next covers what was there. Every build tool's progress bar is "
                + "this and nothing more — and it is why a terminal is worth using for build output, where "
                + "a text box would show you two hundred lines of bar.",
                """
                for (var percent = 0; percent <= 100; percent += 2)
                {
                    var bar = new string('#', percent / 2).PadRight(50, '.');
                    terminal.Feed(Encoding.UTF8.GetBytes($"\r  {bar} {percent,3}%"));
                    await Task.Delay(30);
                }
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "\r\n");

                    for (var percent = 0; percent <= 100; percent += 2)
                    {
                        var bar = new string('#', percent / 2).PadRight(50, '.');
                        Write(context, $"\r  {Esc}[36m{bar}{Esc}[0m {percent,3}%");
                        await Task.Delay(28);
                    }

                    await Type(context, $"\r\n  {Esc}[32mdone{Esc}[0m — and only one line was ever used.\r\n");
                }),
        ]);

    // ---- 3 -----------------------------------------------------------------------------------------

    private static Scenario Typing => new(
        "3 · Typing into it",
        "Input is the other half. Handle it yourself and the control is a terminal over anything — an "
        + "SSH channel, a container, or an interpreter in your own process.",
        [
            new Step(
                "See what a keystroke is",
                "The control raises Input with the bytes your keys encode to. A letter is one byte, Return "
                + "is CR, an arrow key is three bytes beginning with ESC. Click the terminal and type: "
                + "nothing will appear, because nothing is echoing it yet. Watch the log at the bottom.",
                """
                terminal.Input += bytes =>
                {
                    // what the user typed, already encoded the way a program expects to read it
                };
                """,
                context =>
                {
                    Clear(context);
                    Write(context, "Click here and type. Nothing echoes it yet — look at the log below.\r\n\r\n");

                    context.WhileHereOnInput(bytes => context.Log($"Input: {Readable(bytes.Span)}"));
                    context.Terminal.Focus();
                    return Task.CompletedTask;
                }),

            new Step(
                "Echo it back",
                "A terminal does not echo what you type — the far end does, and you are the far end here. "
                + "Feed the bytes straight back and the characters appear.",
                """
                terminal.Input += bytes => terminal.Feed(bytes.Span);
                """,
                context =>
                {
                    Clear(context);
                    Write(context, "Now it echoes. Type away.\r\n\r\n> ");

                    context.WhileHereOnInput(bytes => context.Terminal.Feed(bytes.Span));
                    context.Terminal.Focus();
                    return Task.CompletedTask;
                }),

            new Step(
                "Read a line properly",
                "Echoing raw bytes is not enough. Return has to start a new line, Backspace has to erase a "
                + "character, and anything above ASCII arrives as two to four bytes of UTF-8 that must be "
                + "decoded together — which is why the decoder is kept between calls. Type a line and "
                + "press Return.",
                """
                var line = new StringBuilder();
                var decoder = Encoding.UTF8.GetDecoder();   // stateful: a keystroke can be split

                terminal.Input += bytes =>
                {
                    foreach (var b in bytes.Span)
                        switch (b)
                        {
                            case 0x0d:                          // Return
                                terminal.Feed($"\r\nyou said: {line}\r\n> "u8);
                                line.Clear();
                                break;

                            case 0x7f when line.Length > 0:     // Backspace
                                line.Length--;
                                terminal.Feed("\b \b"u8);       // back, erase, back
                                break;

                            case >= 0x20:
                                Span<char> one = stackalloc char[4];
                                var n = decoder.GetChars([b], one, flush: false);
                                if (n == 0) break;              // half a character; wait for the rest
                                line.Append(one[..n]);
                                terminal.Feed(bytes.Span);      // echo
                                break;
                        }
                };
                """,
                context =>
                {
                    Clear(context);
                    Write(context, "A line editor in twenty lines. Type, use Backspace, press Return.\r\n\r\n> ");

                    var line = new StringBuilder();
                    var decoder = Encoding.UTF8.GetDecoder();

                    context.WhileHereOnInput(bytes =>
                    {
                        foreach (var b in bytes.Span)
                        {
                            if (b is 0x0d or 0x0a)
                            {
                                Write(context, $"\r\n{Esc}[32myou said:{Esc}[0m {line}\r\n\r\n> ");
                                line.Clear();
                            }
                            else if (b is 0x7f or 0x08)
                            {
                                if (line.Length == 0) continue;
                                line.Length--;
                                Write(context, "\b \b");
                            }
                            else if (b >= 0x20)
                            {
                                var decoded = new char[4];
                                var count = decoder.GetChars([b], decoded, flush: false);
                                if (count == 0) continue;

                                var text = new string(decoded, 0, count);
                                line.Append(text);
                                Write(context, text);
                            }
                        }
                    });

                    context.Terminal.Focus();
                    return Task.CompletedTask;
                }),

            new Step(
                "That was a terminal over a channel",
                "Replace the two handlers and the same control is an SSH client, a Docker exec window, a "
                + "serial console, or a recording being replayed. Six lines, none of which know what is on "
                + "the other end — and it is exactly what the browser build of this demo is doing.",
                """
                var terminal = new AvaTerminal { AutoStart = false };

                terminal.Input   += bytes => channel.Send(bytes);    // what the user typed
                channel.Received += bytes => terminal.Feed(bytes);   // what the far end printed
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, $"{Esc}[1mSix lines, and it is an SSH client.{Esc}[0m\r\n\r\n");
                    await Type(context, "  terminal.Input   -> channel.Send      what you typed\r\n");
                    await Type(context, "  channel.Received -> terminal.Feed     what came back\r\n\r\n");
                    await Type(context, "Answers to questions the far end asks — where is the cursor, what\r\n");
                    await Type(context, "are you — leave through Input too, so forwarding Input forwards a\r\n");
                    await Type(context, "complete terminal. Scenario 6 shows one of those answers.\r\n");
                }),
        ]);

    // ---- 4 -----------------------------------------------------------------------------------------

    private static Scenario Looks => new(
        "4 · Making it yours",
        "The face, its size, and the colours — three properties, and what happens when the one you asked "
        + "for is not installed.",
        [
            new Step(
                "The face",
                "TerminalFont.Resolve takes families in preference order and returns the first that is "
                + "really fixed-pitch. That check matters: ask macOS for a family it does not have and it "
                + "hands back a proportional one, which would give you a terminal whose columns drift "
                + "apart. A browser tab has no system fonts at all, which is why this demo embeds one.",
                """
                terminal.Font = TerminalFont.Resolve("JetBrains Mono", "Menlo", "Consolas");

                terminal.Font.RequestedFamily;   // what you asked for
                terminal.Font.FamilyName;        // what actually answered
                """,
                async context =>
                {
                    Clear(context);
                    var font = context.Terminal.Font;
                    await Type(context, $"asked for   {font.RequestedFamily}\r\n");
                    await Type(context, $"drawing in  {Esc}[32m{font.FamilyName}{Esc}[0m\r\n\r\n");
                    await Type(context, "The Sandbox tab offers families this machine may well not have,\r\n");
                    await Type(context, "so you can watch the fallback happen and see what answered.\r\n");
                }),

            new Step(
                "How big the text is",
                "TerminalFontSize is in device-independent pixels. Changing it re-measures the cell and, "
                + "because AutoSize is on, re-fits the console — so a bigger face means fewer columns in "
                + "the same rectangle, exactly as it would anywhere else.",
                """
                terminal.TerminalFontSize = 16;
                """,
                async context =>
                {
                    Clear(context);
                    var original = context.Terminal.TerminalFontSize;

                    foreach (var size in new double[] { 11, 13, 15, 17, 15, 13 })
                    {
                        context.Terminal.TerminalFontSize = size;
                        Write(context, $"\r{Esc}[K  {size:0} dip — {context.Terminal.Columns} columns fit");
                        await Task.Delay(650);
                    }

                    context.Terminal.TerminalFontSize = original;
                    await Type(context, $"\r{Esc}[K  back to {original:0} dip — {context.Terminal.Columns} columns\r\n");
                }),

            new Step(
                "The colours",
                "A Theme is the terminal's colour model: sixteen ANSI colours, what \"default\" means, and "
                + "how bold, dim and reverse change the answer. The other 240 palette entries are computed "
                + "from the standard cube and grey ramp. Watch the swatches already on screen change with "
                + "it — nothing repaints them, because the engine kept the index rather than the colour.",
                """
                terminal.TerminalTheme = Theme.Dark;   // the default: xterm's own sixteen

                terminal.TerminalTheme = new Theme(
                    foreground: Rgba.FromHex(0x24292F),
                    background: Rgba.FromHex(0xFFFFFF),
                    cursor:     Rgba.FromHex(0x0969DA),
                    cursorText: Rgba.FromHex(0xFFFFFF),
                    selection:  Rgba.FromHex(0xB6D7FF),
                    ansi16:     sixteenColours,
                    boldIsBright: false);
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "Some colour to look at while the theme changes underneath it:\r\n\r\n  ");
                    for (var i = 0; i < 8; i++) Write(context, $"{Esc}[3{i}m### {Esc}[0m");
                    Write(context, "\r\n  ");
                    for (var i = 0; i < 8; i++) Write(context, $"{Esc}[4{i}m   {Esc}[0m ");
                    await Type(context, "\r\n\r\n");

                    foreach (var (name, get) in DemoThemes.All)
                    {
                        context.Terminal.TerminalTheme = get();
                        Write(context, $"\r{Esc}[K  now: {name}");
                        await Task.Delay(1300);
                    }

                    context.Terminal.TerminalTheme = DemoThemes.Dark;
                    await Type(context, $"\r{Esc}[K  back to the default.\r\n");
                }),
        ]);

    // ---- 6 -----------------------------------------------------------------------------------------

    private static Scenario Listening => new(
        "6 · What it tells you",
        "A terminal reports things: a title, a bell, an exit code — and answers to questions the program "
        + "asks it.",
        [
            new Step(
                "The title",
                "Programs say what they are doing by setting the window title: a shell prompt puts the "
                + "directory there, ssh puts the host, vim puts the file. An application with tabs puts it "
                + "on the tab. It arrives as OSC 0 — an escape sequence ending in BEL.",
                """
                terminal.TitleChanged += title => tab.Header = title;

                terminal.Title;   // whatever it last was
                """,
                async context =>
                {
                    Clear(context);
                    foreach (var title in new[] { "~/project", "vim README.md", "build-host: make" })
                    {
                        Write(context, $"{Esc}]0;{title}{Bel}");
                        await Type(context, $"set the title to {Esc}[36m{title}{Esc}[0m\r\n");
                        await Task.Delay(1000);
                    }

                    await Type(context, "\r\nLook at the right-hand end of the status bar, and at the log.\r\n");
                }),

            new Step(
                "The bell",
                "One byte, 0x07. The control reports it and makes no sound — whether a bell should be "
                + "audible, a flash, a dock badge or nothing at all is a decision about your application, "
                + "and a library that made it for you would be a library that beeps in a build log.",
                """
                terminal.Bell += () => tab.NeedsAttention = true;
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "Ringing it three times — silently.\r\n\r\n");

                    for (var i = 0; i < 3; i++)
                    {
                        Write(context, Bel);
                        await Type(context, $"  bell {i + 1}\r\n");
                        await Task.Delay(700);
                    }

                    await Type(context, "\r\nThree entries in the log, and no noise.\r\n");
                }),

            new Step(
                "It answers questions too",
                "A terminal is asked things as well as told them. ESC [ 6 n means \"where is the cursor\", "
                + "and the program that sent it waits for the reply. The control answers by the same route "
                + "a keystroke leaves by — so an application forwarding Input to a channel is forwarding a "
                + "complete terminal, answers included. Without this, a program that asked would wait for "
                + "ever.",
                """
                // the program writes this into the stream, then waits
                terminal.Feed("\u001b[6n"u8);

                // ...and the answer comes out of Input, as ESC [ <row> ; <col> R
                terminal.Input += bytes => channel.Send(bytes);
                """,
                async context =>
                {
                    Clear(context);
                    context.WhileHereOnInput(bytes => context.Log($"the terminal answered: {Readable(bytes.Span)}"));

                    await Type(context, "The cursor is on this line, at about column 44.");
                    await Task.Delay(700);
                    Write(context, $"{Esc}[6n");
                    await Task.Delay(400);
                    await Type(context, "\r\n\r\nThe answer is in the log below: the row and column it reported.\r\n");
                }),

            new Step(
                "Reading the screen",
                "Screen is the parsed state, not a log of bytes. A program that redraws a progress bar ten "
                + "thousand times leaves one row, not ten thousand — so this is the right thing to search, "
                + "and the wrong thing to use as a transcript. If you want a transcript, record what you "
                + "fed it.",
                """
                terminal.Screen.GetScreenText();        // everything visible
                terminal.Screen.CursorX, CursorY;       // where the cursor is
                terminal.Screen.IsAlternateScreen;      // is a full-screen program up
                terminal.Screen.Scrollback;             // what has gone off the top
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "one\r\ntwo\r\nthree\r\n");

                    var screen = context.Terminal.Screen;
                    var lines = screen.GetScreenText().TrimEnd().Split('\n').Length;

                    await Type(context, $"\r\nGetScreenText() returned {Esc}[32m{lines}{Esc}[0m lines.\r\n");
                    await Type(context, $"The cursor is at column {screen.CursorX}, row {screen.CursorY}.\r\n");
                }),
        ]);

    // ---- 5 -----------------------------------------------------------------------------------------

    private static Scenario Sizing => new(
        "5 · How big is it",
        "The console size is a number the program is told. Whether the window decides it or you do is one "
        + "property, and getting it wrong is visible immediately.",
        [
            new Step(
                "The window decides",
                "AutoSize is on by default, so the console is however many whole cells fit in the control. "
                + "Drag the window edge and watch the numbers move. Whole cells only: a rectangle that fits "
                + "40.9 columns is a console of 40, because a program told it had 41 would draw into pixels "
                + "that are not there.",
                """
                <ava:AvaTerminal />        <!-- AutoSize is on by default -->

                terminal.Columns;          // read it: what the program was told
                terminal.Rows;
                """,
                async context =>
                {
                    Clear(context);
                    context.Terminal.AutoSize = true;
                    await Type(context, $"The console is {Esc}[32m{context.Terminal.Columns}x{context.Terminal.Rows}{Esc}[0m right now.\r\n\r\n");
                    await Type(context, "Drag the window edge and watch the status bar at the bottom.\r\n");
                    await Type(context, "On a real pty every one of those changes raises SIGWINCH in the\r\n");
                    await Type(context, "program, which is how vim knows to redraw itself.\r\n");
                }),

            new Step(
                "Or you decide",
                "Writing to Columns or Rows pins the size and turns AutoSize off — the property is both "
                + "the question and the answer. The control then draws that console inside whatever space "
                + "it has. This is what you want for a fixed-format report, or for a panel that moves a lot "
                + "and would otherwise resize the program every time.",
                """
                <ava:AvaTerminal AutoSize="False" Columns="80" Rows="24" />
                """,
                async context =>
                {
                    Clear(context);
                    context.Terminal.Columns = 80;
                    context.Terminal.Rows = 24;

                    await Type(context, $"Pinned at {Esc}[32m80x24{Esc}[0m. AutoSize is now {Answer(context.Terminal.AutoSize)}.\r\n\r\n");
                    await Type(context, "Drag the window: the console stays 80x24 and the drawing is\r\n");
                    await Type(context, "letterboxed inside it. Turn AutoSize back on in the Sandbox.\r\n");
                }),

            new Step(
                "Why resizing can destroy something",
                "A full-screen program runs on the alternate screen — a second buffer with no scrollback. "
                + "Shrinking it throws rows away and there is nothing to restore them from. That is the one "
                + "place where a layout that moves about is genuinely dangerous, and the reason AutoSize is "
                + "worth turning off for a panel that collapses.",
                """
                terminal.Screen.IsAlternateScreen;   // true while vim or top is up

                // ESC [ ? 1049 h   switch to it, remembering the cursor
                // ESC [ ? 1049 l   switch back
                """,
                async context =>
                {
                    Clear(context);
                    await Type(context, "This text is on the normal screen, with scrollback behind it.\r\n");
                    await Task.Delay(1000);

                    Write(context, $"{Esc}[?1049h{Esc}[2J{Esc}[H");
                    await Type(context, $"  {Esc}[1mThe alternate screen.{Esc}[0m\r\n\r\n");
                    await Type(context, "  vim, top and every full-screen program live here.\r\n");
                    await Type(context, "  No scrollback: what goes off the edge is gone.\r\n\r\n");
                    await Type(context, $"  IsAlternateScreen is {Answer(context.Terminal.Screen.IsAlternateScreen)}\r\n");
                    await Task.Delay(2800);

                    Write(context, $"{Esc}[?1049l");
                    await Type(context, "…and back, to exactly what was here. That is the point of it.\r\n");
                }),
        ]);

    // ---- 7 -----------------------------------------------------------------------------------------

    private static Scenario RunningAProgram => new(
        "7 · Running a program",
        "The part that needs an operating system: a pty, a process, and a shell if you want one.",
        [
            new Step(
                "A shell, with nothing installed",
                "The control brings its own. cd, pwd, exit and help are builtins; everything else is a "
                + "file found on PATH and run on a pty of its own — so ls, mkdir, git, vim and top all "
                + "work with no zsh, bash or cmd.exe involved. That is what makes it a control you can "
                + "drop on a form rather than one that works only where somebody configured a shell.",
                """
                terminal.StartBuiltinShell();

                // or the user's own, with their aliases and prompt
                terminal.StartSystemShell();
                """,
                context => Attempt(context, terminal => terminal.StartBuiltinShell(),
                    "type ls, mkdir, pwd, vim or top")),

            new Step(
                "One program, no shell",
                "Most applications embedding a terminal do not want a shell at all — they want one "
                + "program. Command is an absolute path: there is no PATH search and no shell parsing "
                + "here, because a control that split its own command line would be a second, worse shell. "
                + "CommandResolver is that search on its own, for when you have a name.",
                """
                terminal.Start("/usr/bin/top");

                // when you have a name rather than a path
                if (CommandResolver.TryResolve("git", null, out var path, out var why))
                    terminal.Start(path, ["log", "--oneline"]);
                else
                    ShowError(why);
                """,
                context => Attempt(context, terminal =>
                {
                    if (!CommandResolver.TryResolve("top", null, out var path, out var why))
                        throw new InvalidOperationException(why);

                    terminal.Start(path);
                }, "press q to leave top")),

            new Step(
                "Stopping it",
                "Signal reaches the program's process group, and Int is what Ctrl-C does. Stop ends it "
                + "now. Either way Exited arrives with the code — after the program's last output and "
                + "after the control has torn down, so IsRunning is already false in your handler. The "
                + "last thing a program prints is regularly the thing the user needs.",
                """
                terminal.Signal(PtySignal.Int);    // Ctrl-C
                terminal.Stop();                   // end it now

                terminal.Exited += code => status.Text = $"exited with {code}";
                """,
                async context =>
                {
                    if (!context.Terminal.IsRunning)
                    {
                        Clear(context);
                        await Type(context, "Nothing is running to stop — go back a step and start something.\r\n");
                        return;
                    }

                    context.Log("Signal(PtySignal.Int)");
                    context.Terminal.Signal(PtySignal.Int);
                    await Task.Delay(800);

                    if (!context.Terminal.IsRunning) return;

                    context.Log("it ignored the interrupt; Stop()");
                    context.Terminal.Stop();
                }),
        ],
        NeedsPty: true);

    // ---- the small print ---------------------------------------------------------------------------

    /// <summary>Does something that needs a pty, or explains why it cannot here.</summary>
    private static async Task Attempt(StepContext context, Action<AvaTerminal> start, string hint)
    {
        Clear(context);

        try
        {
            start(context.Terminal);
            await Task.Delay(400);
            context.Log($"started — {hint}");
        }
        catch (Exception e)
        {
            await Type(context, $"{Esc}[33mThis step needs a pty, and there is not one here.{Esc}[0m\r\n\r\n");
            await Type(context, $"  {e.Message}\r\n\r\n");
            await Type(context, "A browser tab has no operating system underneath it, so no program\r\n");
            await Type(context, "can run. The code on the right is what you would write, and it works\r\n");
            await Type(context, "unchanged on a desktop — clone the demo and try it there.\r\n");
        }
    }

    private static void Clear(StepContext context) =>
        Write(context, $"{Esc}[0m{Esc}[2J{Esc}[H");

    private static void Write(StepContext context, string text) =>
        context.Terminal.Feed(Encoding.UTF8.GetBytes(text));

    /// <summary>Writes, then pauses — a wall of text that appears all at once does not get read.</summary>
    private static async Task Type(StepContext context, string text)
    {
        Write(context, text);
        await Task.Delay(90);
    }

    private static string Answer(bool value) =>
        value ? $"{Esc}[32mtrue{Esc}[0m" : $"{Esc}[31mfalse{Esc}[0m";

    private static string Dim(string text) => $"{Esc}[2m{text}{Esc}[0m";

    /// <summary>Bytes as something a person can read: escapes named, the rest as they were typed.</summary>
    public static string Readable(ReadOnlySpan<byte> bytes)
    {
        var text = new StringBuilder();

        foreach (var b in bytes)
            text.Append(b switch
            {
                0x1b => "ESC ",
                0x0d => "CR",
                0x0a => "LF",
                0x09 => "TAB",
                0x7f => "DEL",
                < 0x20 => $"^{(char)(b + 64)}",
                _ => ((char)b).ToString(),
            });

        return text.ToString();
    }
}
