using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaTerminal.Demo;

/// <summary>
/// The demo, on whichever kind of host it finds itself.
/// </summary>
/// <remarks>
/// A desktop lifetime wants a window to put the view in; a browser tab has no windows at all and
/// hands over a single view instead. Both get the same <see cref="MainView"/> — what differs is the
/// <see cref="DemoHost"/> each head passes in, which is the list of things that can be started. On a
/// desktop that includes real programs on a real pty; in a browser there is no pty, so it does not.
/// </remarks>
public partial class App : Application
{
    /// <summary>Set by the head before <c>Start</c>, because it is the head that knows.</summary>
    public static DemoHost Host { get; set; } = DemoHost.Simulated;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new Window
                {
                    Title = "AvaTerminal — a terminal emulator control for Avalonia",
                    Width = 1280,
                    Height = 820,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new MainView(Host),
                };
                break;

            case ISingleViewApplicationLifetime single:
                single.MainView = new MainView(Host);
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
