using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AvaTerminal.Demo;

/// <summary>
/// A shell that runs in this process: no pty, no child process, nothing installed.
/// </summary>
/// <remarks>
/// <b>This is what the browser demo runs</b>, and it is not a trick to hide a missing feature — it is
/// the mode the library documents as "a terminal with no process". <see cref="AvaTerminal.AutoStart"/>
/// is off, so nothing is spawned; the control raises <see cref="AvaTerminal.Input"/> with the bytes the
/// user's keys encode to, and this class answers with <see cref="AvaTerminal.Feed"/>. Replace the two
/// handlers with an SSH channel and the same terminal is an SSH client.
/// <para>
/// What it is <i>not</i> is the library's own <c>BuiltinShell</c>. That one searches <c>PATH</c> and
/// runs the program it finds on a pty, which needs an operating system underneath it. This one has a
/// dictionary of made-up files and answers about a dozen commands.
/// </para>
/// </remarks>
public sealed class MockShell : IDisposable
{
    private const string Esc = "\u001b";

    /// <summary>A directory that exists only here, so that <c>ls</c> has something honest to print.</summary>
    private static readonly Dictionary<string, string> Files = new(StringComparer.Ordinal)
    {
        ["README.md"] = "# A directory that does not exist\n\nEvery file here is a string in "
                      + "MockShell.cs. The terminal drawing them is real.\n",
        ["Program.cs"] = "Content = new AvaTerminal();\n",
        ["notes.txt"] = "A terminal is three things people call by one name:\n"
                      + "  a pty      - a kernel object; renders nothing, parses nothing\n"
                      + "  a shell    - an ordinary program; optional\n"
                      + "  a terminal - parses, draws, and encodes keys back. That is this.\n",
    };

    private readonly AvaTerminal _terminal;
    private readonly StringBuilder _line = new();
    private readonly List<string> _history = [];

    /// <summary>Stateful, because the halves of one keystroke can arrive in separate calls.</summary>
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();

    private int _browsing;
    private int _pending;   // how much of an ESC [ A sequence has arrived
    private string _directory = "~/project";

    private MockShell(AvaTerminal terminal)
    {
        _terminal = terminal;
        _terminal.Input += OnInput;

        Write($"{Esc}[2J{Esc}[H");
        Write($"{Esc}[1mA terminal with no process at all.{Esc}[0m\r\n\r\n");
        Write("Nothing is running: AutoStart is off, so no program was started and no pty was\r\n");
        Write("opened. What you type is raised as Input and answered with Feed - the same six\r\n");
        Write("lines you would write to put this terminal on an SSH channel.\r\n\r\n");
        Write($"Type {Esc}[36mhelp{Esc}[0m. The files are made up; the terminal is not.\r\n\r\n");
        Prompt();
    }

    /// <summary>Attaches one to a terminal. Dispose to let go of it.</summary>
    public static IDisposable Attach(AvaTerminal terminal)
    {
        terminal.Stop();
        return new MockShell(terminal);
    }

    public void Dispose() => _terminal.Input -= OnInput;

    // ---- the keyboard ------------------------------------------------------------------------------

    private void OnInput(ReadOnlyMemory<byte> data)
    {
        foreach (var b in data.Span) Key(b);
    }

    private void Key(byte b)
    {
        // An arrow is three bytes - ESC [ A - so two of them are remembered rather than printed.
        if (_pending == 1) { _pending = b == (byte)'[' ? 2 : 0; return; }
        if (_pending == 2)
        {
            _pending = 0;
            if (b == (byte)'A') Recall(-1);
            else if (b == (byte)'B') Recall(1);
            return;
        }

        switch (b)
        {
            case 0x1b: _pending = 1; return;

            case 0x0d or 0x0a:
                Write("\r\n");
                Run(_line.ToString().Trim());
                _line.Clear();
                return;

            case 0x7f or 0x08:
                if (_line.Length == 0) return;
                _line.Length--;
                Write("\b \b");
                return;

            case 0x03:
                Write($"{Esc}[2m^C{Esc}[0m\r\n");
                _line.Clear();
                Prompt();
                return;

            case >= 0x20:
                Typed(b);
                return;
        }
    }

    /// <summary>A byte of text: added to the line and echoed, once it completes a character.</summary>
    private void Typed(byte b)
    {
        Span<char> decoded = stackalloc char[4];
        var count = _decoder.GetChars([b], decoded, flush: false);
        if (count == 0) return;

        var text = new string(decoded[..count]);
        _line.Append(text);
        Write(text);
    }

    private void Recall(int by)
    {
        if (_history.Count == 0) return;

        var wanted = Math.Clamp(_browsing + by, 0, _history.Count);
        if (wanted == _browsing) return;
        _browsing = wanted;

        // Erase the whole line rather than the characters: the one replacing it may be shorter.
        Write($"\r{Esc}[2K");
        Prompt(withNewline: false);

        _line.Clear();
        if (wanted < _history.Count) _line.Append(_history[wanted]);
        Write(_line.ToString());
    }

    // ---- the commands ------------------------------------------------------------------------------

    private void Run(string line)
    {
        if (line.Length > 0) _history.Add(line);
        _browsing = _history.Count;

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var argument = words.Length > 1 ? string.Join(' ', words[1..]) : "";

        switch (words.FirstOrDefault())
        {
            case null: break;

            case "help": Help(); break;
            case "about": About(); break;
            case "ls" or "dir": List(); break;
            case "cat" or "type": Cat(argument); break;
            case "pwd": Write($"{_directory}\r\n"); break;
            case "cd": ChangeDirectory(argument); break;
            case "echo": Write($"{argument}\r\n"); break;
            case "whoami": Write("a visitor\r\n"); break;
            case "date": Write($"{DateTime.Now:dddd, d MMMM yyyy HH:mm:ss}\r\n"); break;
            case "clear": Write($"{Esc}[2J{Esc}[H"); break;

            case "exit" or "quit":
                Write("There is nothing to exit: no process was ever started.\r\n");
                break;

            default:
                Write($"{Esc}[31m{words[0]}: not one of the dozen commands this demo answers{Esc}[0m\r\n");
                Write($"Try {Esc}[36mhelp{Esc}[0m.\r\n");
                break;
        }

        Prompt();
    }

    private void Help()
    {
        Write($"{Esc}[1mCommands{Esc}[0m\r\n");
        foreach (var (name, what) in new[]
        {
            ("ls", "list the made-up files"),
            ("cat <file>", "print one"),
            ("pwd / cd <dir>", "where you are, and move"),
            ("echo <text>", "print it back"),
            ("whoami / date", "the usual two"),
            ("clear", "erase the screen - ESC [ 2 J"),
            ("about", "what is actually running here"),
        })
            Write($"  {Esc}[36m{name,-16}{Esc}[0m{what}\r\n");

        Write($"\r\nUp and down recall earlier lines. Ctrl-C abandons the one you are typing.\r\n");
        Write($"The buttons on the left draw straight onto this screen without sending anything.\r\n");
    }

    private void About()
    {
        Write($"\r\n{Esc}[1mWhat is running{Esc}[0m\r\n\r\n");
        Write("  This shell        a class in the demo, about 200 lines\r\n");
        Write("  The pty           none. Nothing was spawned.\r\n");
        Write($"  The parser        {Esc}[32mAvaTerminal{Esc}[0m - real, and the same one a pty would feed\r\n");
        Write($"  The renderer      {Esc}[32mAvaTerminal{Esc}[0m - real; every cell you can see came from it\r\n");
        Write($"  The keyboard      {Esc}[32mAvaTerminal{Esc}[0m - real; this shell only reads the bytes\r\n\r\n");
        Write("  On a desktop the same control runs /bin/ls, vim and top on a real pty.\r\n");
        Write("  A browser tab has no pty, so this demo is the library's documented\r\n");
        Write("  \"terminal with no process\" mode instead.\r\n\r\n");
    }

    private void List()
    {
        Write($"{Esc}[34msrc{Esc}[0m\r\n");
        foreach (var name in Files.Keys) Write($"{name}\r\n");
    }

    private void Cat(string name)
    {
        if (name.Length == 0) { Write("cat: which file?\r\n"); return; }

        if (!Files.TryGetValue(name, out var content))
        {
            Write($"{Esc}[31mcat: {name}: no such file{Esc}[0m\r\n");
            return;
        }

        Write(content.Replace("\n", "\r\n"));
    }

    private void ChangeDirectory(string wanted)
    {
        _directory = wanted switch
        {
            "" or "~" => "~/project",
            ".." => "~",
            _ when wanted.StartsWith('/') => wanted,
            _ => $"{_directory}/{wanted}",
        };
    }

    // ---- drawing -----------------------------------------------------------------------------------

    private void Prompt(bool withNewline = false)
    {
        if (withNewline) Write("\r\n");
        Write($"{Esc}[36m{_directory}{Esc}[0m {Esc}[32m${Esc}[0m ");
    }

    private void Write(string text) => _terminal.Feed(Encoding.UTF8.GetBytes(text));
}
