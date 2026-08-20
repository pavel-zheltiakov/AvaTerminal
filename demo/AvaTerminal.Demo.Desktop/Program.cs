using System;
using System.Collections.Generic;
using Avalonia;

namespace AvaTerminal.Demo.Desktop;

/// <summary>
/// The desktop head: the same demo, with a pty underneath it.
/// </summary>
/// <remarks>
/// The only thing this head adds is the list of sessions — real programs on a real pty, which a
/// browser tab cannot have. Everything else, including every panel on the left, is the shared demo.
/// </remarks>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.Host = DesktopHost();

        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .StartWithClassicDesktopLifetime(args);
    }

    private static DemoHost DesktopHost()
    {
        var sessions = new List<DemoSession>
        {
            new("Built-in shell",
                "The shell that comes with the control: cd, pwd, exit and help are builtins, and "
                + "everything else is a file found on PATH and run on a pty of its own. No zsh, no "
                + "bash, nothing installed.",
                terminal => { terminal.StartBuiltinShell(); return null; }),

            new("Your login shell",
                "$SHELL, or /bin/sh — the choice every other terminal emulator makes, with your "
                + "aliases, your prompt and your completion.",
                terminal => { terminal.StartSystemShell(); return null; }),
        };

        // Offered only if they are actually installed, which is what CommandResolver is for: the
        // PATH search on its own, without running anything.
        foreach (var (command, arguments, what) in new[]
        {
            ("top", Array.Empty<string>(),
                "A full-screen program. It switches to the alternate screen, redraws on every "
                + "SIGWINCH, and wants every keystroke — press q to leave."),
            ("git", new[] { "log", "--oneline", "--graph", "--all", "--color=always" },
                "A program that decides to use colour because it is on a terminal. Run from this "
                + "demo's own directory."),
        })
        {
            if (!CommandResolver.TryResolve(command, null, out var path, out _)) continue;

            sessions.Add(new DemoSession($"{command} {string.Join(' ', arguments)}".Trim(), what,
                terminal => { terminal.Start(path, arguments); return null; }));
        }

        sessions.Add(DemoHost.SimulatedShell);

        return new DemoHost
        {
            Platform = "desktop",
            Note = "This head can start real programs, because it has a pty. The last session in the "
                 + "list is the one the browser demo runs — no process at all — so the two can be "
                 + "compared side by side.",
            Sessions = sessions,
        };
    }
}
