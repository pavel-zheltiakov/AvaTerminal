using System;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaTerminal.Pty;
using AvaTerminal.Scene;

namespace AvaTerminal.Demo;

/// <summary>
/// The demo: a terminal, and every knob the library exposes beside it.
/// </summary>
/// <remarks>
/// Written as ordinary code-behind with named controls rather than as a view model, because it is a
/// sample — the interesting line is the one that touches <see cref="AvaTerminal"/>, and a binding
/// layer between the two would hide exactly the thing somebody came here to read.
/// </remarks>
public partial class MainView : UserControl
{
    private readonly DemoHost _host;

    /// <summary>Whatever the current session left behind — null when the control owns everything.</summary>
    private IDisposable? _attached;

    /// <summary>
    /// True while the panels are being brought into line with the control.
    /// </summary>
    /// <remarks>
    /// The size panel is two-way: it writes <see cref="AvaTerminal.Columns"/>, and it also shows what
    /// the console became when the window was dragged. Without this guard the second would trigger
    /// the first, and merely resizing the window would pin the size and turn AutoSize off.
    /// </remarks>
    private bool _syncing;

    public MainView() : this(DemoHost.Simulated) { }

    public MainView(DemoHost host)
    {
        _host = host;

        // Generated from MainView.axaml: it loads the XAML and hands every x:Name a field.
        InitializeComponent();

        Build();
    }

    private void Build()
    {
        PlatformTag.Text = _host.Platform;
        HostNote.Text = _host.Note;

        BuildSessionPanel();
        BuildAppearancePanel();
        BuildSizePanel();
        BuildSendPanel();
        BuildPlaysPanel();
        BuildEventsPanel();

        WatchTheTerminal();

        Loaded += (_, _) =>
        {
            StartSelectedSession();
            Terminal.Focus();
        };
    }

    // ---- session -----------------------------------------------------------------------------------

    private void BuildSessionPanel()
    {
        Sessions.ItemsSource = _host.Sessions;
        Sessions.SelectedIndex = 0;
        Sessions.SelectionChanged += (_, _) => StartSelectedSession();

        Restart.Click += (_, _) => StartSelectedSession();
        StopIt.Click += (_, _) =>
        {
            Detach();
            Terminal.Stop();
            Note("stopped");
        };

        // The same call whether a program, a shell or nothing is running: at a prompt an interrupt
        // means "abandon the line", and there is no process to send it to.
        Interrupt.Click += (_, _) =>
        {
            Terminal.Signal(PtySignal.Int);
            Terminal.Focus();
        };
    }

    private void StartSelectedSession()
    {
        if (Sessions.SelectedItem is not DemoSession session) return;

        Detach();
        SessionNote.Text = session.Description;

        try
        {
            _attached = session.Start(Terminal);
            Note($"started: {session.Name}");
        }
        catch (Exception e)
        {
            // A program that is not installed, or a pty that cannot be opened. Worth showing on the
            // screen rather than only in the log: the terminal is where a user is looking.
            Terminal.Feed(Encoding.UTF8.GetBytes($"\r\n{e.Message}\r\n"));
            Note($"could not start {session.Name}: {e.Message}");
        }

        Terminal.Focus();
    }

    private void Detach()
    {
        _attached?.Dispose();
        _attached = null;
    }

    // ---- appearance --------------------------------------------------------------------------------

    /// <summary>
    /// The faces to offer. The first is embedded in this demo; the rest are asked of the system, and
    /// what happens when they are not there is worth seeing.
    /// </summary>
    private static readonly (string Label, string[] Families)[] FontChoices =
    [
        ("JetBrains Mono", ["avares://AvaTerminal.Demo/Fonts#JetBrains Mono NL"]),
        ("System default", []),
        ("Menlo", ["Menlo"]),
        ("Consolas", ["Consolas"]),
        ("DejaVu Sans Mono", ["DejaVu Sans Mono"]),
    ];

    private void BuildAppearancePanel()
    {
        Fonts.ItemsSource = FontChoices.Select(f => f.Label);
        Fonts.SelectedIndex = 0;
        Fonts.SelectionChanged += (_, _) => SetFont();
        SetFont();

        FontSizeSlider.Value = Terminal.TerminalFontSize;
        FontSizeSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property != Slider.ValueProperty) return;

            Terminal.TerminalFontSize = FontSizeSlider.Value;
            ShowFontSize();
        };
        ShowFontSize();

        Themes.ItemsSource = DemoThemes.All.Select(t => t.Name);
        Themes.SelectedIndex = 0;
        Themes.SelectionChanged += (_, _) =>
        {
            Terminal.TerminalTheme = DemoThemes.All[Math.Max(0, Themes.SelectedIndex)].Get();
            Note($"theme: {DemoThemes.All[Math.Max(0, Themes.SelectedIndex)].Name}");
        };
    }

    private void ShowFontSize() => FontSizeLabel.Text = $"Size {Terminal.TerminalFontSize:0}";

    /// <summary>
    /// Resolving a face, and saying which one answered.
    /// </summary>
    /// <remarks>
    /// <c>TerminalFont.Resolve</c> takes families in preference order and returns the first that is
    /// really fixed-pitch — asking macOS for a family it does not have returns a proportional one, and
    /// accepting that would give a terminal whose columns drift apart. So the answer is not always the
    /// question, and the demo shows both.
    /// </remarks>
    private void SetFont()
    {
        var choice = FontChoices[Math.Max(0, Fonts.SelectedIndex)];
        var font = TerminalFont.Resolve(choice.Families);

        Terminal.Font = font;

        FontNote.Text = font.FamilyName == font.RequestedFamily || choice.Families.Length == 0
            ? $"drawing with {font.FamilyName}"
            : $"asked for {choice.Label}; this machine answered with {font.FamilyName}";
    }

    // ---- size --------------------------------------------------------------------------------------

    private void BuildSizePanel()
    {
        AutoSize.IsChecked = Terminal.AutoSize;
        AutoSize.IsCheckedChanged += (_, _) =>
        {
            Terminal.AutoSize = AutoSize.IsChecked == true;
            Note(Terminal.AutoSize ? "AutoSize on — the console follows the control"
                                   : $"AutoSize off — pinned at {Terminal.Columns}x{Terminal.Rows}");
        };

        Columns.Value = Terminal.Columns;
        Rows.Value = Terminal.Rows;

        Columns.ValueChanged += (_, _) => PinSize();
        Rows.ValueChanged += (_, _) => PinSize();
    }

    private void PinSize()
    {
        if (_syncing) return;

        Terminal.Columns = (int)(Columns.Value ?? Terminal.Columns);
        Terminal.Rows = (int)(Rows.Value ?? Terminal.Rows);

        // Writing to either one is what turns the fitting off, so the checkbox has to follow.
        AutoSize.IsChecked = Terminal.AutoSize;
    }

    // ---- send --------------------------------------------------------------------------------------

    private void BuildSendPanel() =>
        ToSend.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;

            Terminal.Send(ToSend.Text + "\r");
            Note($"sent: {ToSend.Text}");
            ToSend.Text = "";
            e.Handled = true;
        };

    // ---- feed --------------------------------------------------------------------------------------

    private void BuildPlaysPanel()
    {
        foreach (var play in ScreenPlays.All)
        {
            var button = new Button { Content = play.Name, Classes = { "play" } };
            ToolTip.SetTip(button, play.Description);

            button.Click += async (_, _) =>
            {
                PlayNote.Text = play.Description;
                await play.Run(Terminal);
            };

            Plays.Children.Add(button);
        }

        PlayNote.Text = "Feed puts bytes on the screen as though a program had printed them, and sends "
                      + "nothing. That is why these work with no process running.";
    }

    // ---- events ------------------------------------------------------------------------------------

    private void BuildEventsPanel()
    {
        ReadScreen.Click += (_, _) =>
        {
            var text = Terminal.Screen.GetScreenText().TrimEnd();
            var lines = text.Split('\n').Length;
            Note($"read {lines} lines from the screen; the last one is \"{text.Split('\n')[^1].Trim()}\"");
        };

        ClearLog.Click += (_, _) => Log.Text = "";
    }

    /// <summary>
    /// Every event the control raises, wired once.
    /// </summary>
    /// <remarks>
    /// <c>ScreenChanged</c> fires for every chunk of output, so it updates the status line and nothing
    /// else — a log entry per chunk would be a log nobody can read. <c>Input</c> is the same and is
    /// behind a checkbox, which is also the easiest way to watch the terminal answer a question the
    /// program asked: those bytes come out here too.
    /// </remarks>
    private void WatchTheTerminal()
    {
        Terminal.ScreenChanged += ShowStatus;

        Terminal.TitleChanged += title => Note($"TitleChanged: {title}");
        Terminal.Bell += () => Note("Bell — rung by the program, and deliberately not sounded");
        Terminal.Exited += code => Note($"Exited: {code}. IsRunning is now {Terminal.IsRunning}");

        Terminal.Input += bytes =>
        {
            if (LogInput.IsChecked == true) Note($"Input: {Readable(bytes.Span)}");
        };

        ShowStatus();
    }

    private void ShowStatus()
    {
        _syncing = true;
        Columns.Value = Terminal.Columns;
        Rows.Value = Terminal.Rows;
        _syncing = false;

        var cell = Terminal.Metrics;
        var parts = new StringBuilder()
            .Append($"{Terminal.Columns}x{Terminal.Rows}")
            .Append($"   cell {cell.Width:0.##}x{cell.Height:0.##} dip")
            .Append(Terminal.IsRunning ? "   running" : "   nothing running")
            .Append(Terminal.Pid is { } pid ? $"   pid {pid}" : "")
            .Append(Terminal.Screen.IsAlternateScreen ? "   alternate screen" : "")
            .Append(Terminal.Title.Length > 0 ? $"   title: {Terminal.Title}" : "");

        Status.Text = parts.ToString();
    }

    /// <summary>Bytes as something a person can read: escapes named, the rest as they were typed.</summary>
    private static string Readable(ReadOnlySpan<byte> bytes)
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

    private void Note(string line)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Note(line));
            return;
        }

        Log.Text += $"{DateTime.Now:HH:mm:ss}  {line}\n";
        Log.CaretIndex = Log.Text.Length;
        ShowStatus();
    }
}
