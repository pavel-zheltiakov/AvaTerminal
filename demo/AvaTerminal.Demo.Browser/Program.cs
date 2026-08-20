using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;

namespace AvaTerminal.Demo.Browser;

/// <summary>
/// The browser head: the same demo, in a tab.
/// </summary>
/// <remarks>
/// It offers one session — the simulated shell — because a browser tab has no pty and therefore no
/// way to run a program. That is not a cut-down imitation of the control: it is
/// <c>AutoStart="False"</c> with <c>Feed</c> and <c>Input</c>, which is a mode the library documents
/// and supports everywhere. The parser, the renderer and the input encoding on this page are the ones
/// a desktop terminal uses.
/// </remarks>
[SupportedOSPlatform("browser")]
internal static class Program
{
    private static async Task Main(string[] args)
    {
        App.Host = DemoHost.Simulated;

        await BuildAvaloniaApp().StartBrowserAppAsync("out");

        // And then never return.
        //
        // StartBrowserAppAsync completes once the application is up, so a Main that simply awaited it
        // would fall off the end — and falling off the end of Main is how a .NET WebAssembly process
        // asks the runtime to exit. The runtime obliges, and the tab keeps a page that is never drawn
        // again.
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// The application, with Avalonia's own diagnostics turned on.
    /// </summary>
    /// <remarks>
    /// <c>LogToTrace</c> is not decoration here. A browser tab has no debugger attached and no
    /// terminal, so an exception thrown inside layout or render — which Avalonia logs and swallows
    /// rather than letting escape — would leave no trace at all. Routing the log to Trace puts those
    /// messages in the devtools console, which is the only place anyone can read them from.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .LogToTrace(LogEventLevel.Warning);
}
