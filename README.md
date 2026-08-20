# AvaTerminal

**A terminal emulator control for [Avalonia](https://avaloniaui.net/).** Drop it on a form and it
works — no shell installed, no configuration, no wiring.

A VT/xterm parser, a cell renderer, keyboard and mouse encoding, a pty, and a shell of its own that
finds programs on `PATH` and runs them. One assembly, one dependency.

**[Try it in your browser →](https://pavel-zheltiakov.github.io/AvaTerminal/demo/)** ·
**[Documentation, guide and API reference →](https://pavel-zheltiakov.github.io/AvaTerminal/)**

```
dotnet add package AvaTerminal --prerelease
```

```xml
<ava:AvaTerminal />
```

That is the whole of the minimum case.

---

## What is in this repository

This is the **public** repository: the documentation site and the releases. The library's own source
is not here — the package on [nuget.org](https://www.nuget.org/packages/AvaTerminal) is what you
install, and [the releases](https://github.com/pavel-zheltiakov/AvaTerminal/releases) are where each
`.nupkg` is attached.

| | |
|---|---|
| `docs/` | The site, served by GitHub Pages. Home, the demo, the documentation, the guide and the releases page. |
| `docs/demo/` | The demo compiled to WebAssembly — what the Demo link opens. |
| `demo/` | The demo's source: the shared view, the desktop head and the browser head, restoring `AvaTerminal` from nuget.org. |
| `LICENSE.md` | Freeware, commercial use included. |
| `THIRD-PARTY-NOTICES.md` | What the library builds on — a shorter list than you would expect. |

## Running the demo

Nothing to install: **[open it in your browser](https://pavel-zheltiakov.github.io/AvaTerminal/demo/)**.

Or run it on your machine, where it has a pty and can therefore run real programs:

```
git clone https://github.com/pavel-zheltiakov/AvaTerminal.git
cd AvaTerminal/demo
dotnet run --project AvaTerminal.Demo.Desktop
```

Every panel in it is a property or an event from the documentation: the font, the theme, the console
size, what to run, what to send, what to feed, and a log of everything the control raises. The
browser build offers one session — a shell written in the demo, answering `Input` with `Feed`, with no
process and no pty — because that is what a tab can do, and it is a mode the library supports
everywhere.

## Twelve lines

```csharp
var terminal = new AvaTerminal();          // runs its own shell: cd, pwd, exit, help + PATH
terminal.Exited += code => Close();
Content = terminal;

// or point it at a program
terminal.Start("/usr/bin/ssh", ["build-host"]);

// or drive it yourself, with no process and no pty at all
var hosted = new AvaTerminal { AutoStart = false };
hosted.Input += bytes => channel.Send(bytes);
channel.Received += bytes => hosted.Feed(bytes);
```

[The guide](https://pavel-zheltiakov.github.io/AvaTerminal/guide.html) walks through every one of
those and assumes you have never written a terminal.

## What it deliberately is not

Chrome, tabs, splitting, search, or a session manager. It is a terminal in a rectangle; everything
around it belongs to the application it is dropped into.

Its built-in shell is not `zsh`: there are no pipes, no redirection, no globbing and no variables.
Each of those is a language feature, and a half-implementation that silently ignored `> out.txt`
would be worse than an honest absence. An application that wants the user's own shell asks for it —
`terminal.StartSystemShell()`.

## Requirements

Avalonia 12.1, .NET 8.0 or later, macOS or Linux. The pty layer is `posix_spawn` and `openpty`;
Windows needs a ConPTY implementation of `IPtyHost`, which is one class and no change above it.

## Feedback

[GitHub Issues](https://github.com/pavel-zheltiakov/AvaTerminal/issues) — a bug, a missing escape
sequence, a question. In the open, where the answer helps the next person.

---

**This is a preview.** The parser, the renderer and the shell work; the API may still move before
12.1.0 final.

Copyright © 2026 Pavel Zheltiakov. Freeware; see `LICENSE.md` for the exact terms.
