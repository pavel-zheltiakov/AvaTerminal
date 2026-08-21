using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvaTerminal.Demo;

/// <summary>
/// What a step can do to the terminal, and what it must clean up after itself.
/// </summary>
/// <remarks>
/// A step that subscribes to <see cref="AvaTerminal.Input"/> — several of them do, because that is
/// how a terminal with no process is driven — has to let go of it when the reader moves on, or two
/// scenarios end up answering the same keystroke.
/// </remarks>
public sealed class StepContext(AvaTerminal terminal, Action<string> log)
{
    private readonly List<IDisposable> _owned = [];

    public AvaTerminal Terminal { get; } = terminal;

    /// <summary>Writes a line to the demo's event log.</summary>
    public void Log(string line) => log(line);

    /// <summary>Hands over something to be disposed when the reader leaves this scenario.</summary>
    public T Own<T>(T resource) where T : IDisposable
    {
        _owned.Add(resource);
        return resource;
    }

    /// <summary>Subscribes to what the user types, for as long as this scenario is on screen.</summary>
    public void WhileHereOnInput(Action<ReadOnlyMemory<byte>> handler)
    {
        Terminal.Input += handler;
        Own(new Unsubscribe(() => Terminal.Input -= handler));
    }

    public void Release()
    {
        foreach (var resource in _owned) resource.Dispose();
        _owned.Clear();
    }

    private sealed class Unsubscribe(Action release) : IDisposable
    {
        public void Dispose() => release();
    }
}

/// <summary>
/// One thing to do, with the code that does it.
/// </summary>
/// <param name="Title">The heading above the explanation.</param>
/// <param name="Explanation">Two or three sentences. Assume the reader has never written a terminal.</param>
/// <param name="Code">What this step is, as an application would write it.</param>
/// <param name="Run">Does it.</param>
public sealed record Step(string Title, string Explanation, string Code, Func<StepContext, Task> Run);

/// <summary>A short guided sequence, walked one step at a time.</summary>
/// <param name="Name">The tab's label.</param>
/// <param name="Summary">One sentence: what the reader will know afterwards.</param>
/// <param name="Steps">In order.</param>
/// <param name="NeedsPty">
/// True for a scenario that has to start a process. The browser head shows it and explains it, but
/// the steps say so rather than pretending — there is no pty in a tab.
/// </param>
public sealed record Scenario(string Name, string Summary, IReadOnlyList<Step> Steps, bool NeedsPty = false)
{
    public override string ToString() => Name;
}
