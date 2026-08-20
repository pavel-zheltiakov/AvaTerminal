using System;
using System.Collections.Generic;

namespace AvaTerminal.Demo;

/// <summary>
/// One thing the demo can put in the terminal.
/// </summary>
/// <param name="Name">What the picker shows.</param>
/// <param name="Description">One sentence saying what it demonstrates.</param>
/// <param name="Start">
/// Starts it, and returns whatever has to be let go of when the user picks something else — null when
/// the control owns everything itself, which is the case for anything running on a pty.
/// </param>
public sealed record DemoSession(string Name, string Description, Func<AvaTerminal, IDisposable?> Start)
{
    public override string ToString() => Name;
}

/// <summary>
/// What this head of the demo can offer.
/// </summary>
/// <remarks>
/// The seam between the two heads, and the honest way to express the one real difference between
/// them: a desktop can start a process on a pty and a browser tab cannot. Everything else in the demo
/// — the parser, the renderer, the input encoding, the whole panel of properties on the right — is
/// identical, because it is the same control either way.
/// </remarks>
public sealed class DemoHost
{
    public required string Platform { get; init; }

    /// <summary>Shown under the session picker. Say what this head can and cannot do.</summary>
    public required string Note { get; init; }

    public required IReadOnlyList<DemoSession> Sessions { get; init; }

    /// <summary>
    /// The shell that runs in this process: no pty, no child, nothing installed.
    /// </summary>
    /// <remarks>
    /// This is <c>AutoStart="False"</c> plus <c>Feed</c> and <c>Input</c> — the mode the guide calls
    /// "a terminal with no process". It is what the browser demo runs, and it is offered on the
    /// desktop too so the two can be compared side by side.
    /// </remarks>
    public static DemoSession SimulatedShell { get; } = new(
        "Simulated shell (no process)",
        "Nothing is spawned and no pty is opened. The control raises Input for what you type and this "
        + "demo answers with Feed — the same six lines you would write over an SSH channel.",
        MockShell.Attach);

    /// <summary>A head with nothing but the simulated shell. What a browser tab gets.</summary>
    public static DemoHost Simulated { get; } = new()
    {
        Platform = "browser",
        Note = "There is no pty in a browser tab, so no real program can run here. Everything else on "
             + "this page is the library doing its actual work: the escape sequences below are parsed "
             + "by the real engine, and the screen you are looking at is the real renderer.",
        Sessions = [SimulatedShell],
    };
}
