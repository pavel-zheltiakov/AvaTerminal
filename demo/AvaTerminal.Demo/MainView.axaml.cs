using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using AvaTerminal.Pty;
using AvaTerminal.Scene;

namespace AvaTerminal.Demo;

/// <summary>
/// The demo: a guided tour on the left, a terminal in the middle, and the code on the right.
/// </summary>
/// <remarks>
/// Written as ordinary code-behind with named controls rather than as a view model, because it is a
/// sample — the interesting line is the one that touches <see cref="AvaTerminal"/>, and a binding
/// layer between the two would hide exactly the thing somebody came here to read.
/// </remarks>
public partial class MainView : UserControl
{
    /// <summary>An entry in the list on the left: one scenario, or the Sandbox.</summary>
    private sealed record Tab(string Name, Scenario? Scenario)
    {
        public override string ToString() => Name;
    }

    private readonly DemoHost _host;

    /// <summary>What the current step subscribed to, released when the reader moves on.</summary>
    private StepContext? _context;

    /// <summary>Whatever the current Sandbox session left behind — null when the control owns it all.</summary>
    private IDisposable? _session;

    private Scenario? _scenario;
    private int _step;

    /// <summary>
    /// True while the panels are being brought into line with the control.
    /// </summary>
    /// <remarks>
    /// The size panel is two-way: it writes <see cref="AvaTerminal.Columns"/>, and it also shows what
    /// the console became when the window was dragged. Without this guard the second would trigger the
    /// first, and merely resizing the window would pin the size and turn AutoSize off.
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

        BuildTour();
        BuildSandbox();
        WatchTheTerminal();

        Loaded += (_, _) =>
        {
            Tabs.SelectedIndex = 0;
            Terminal.Focus();
        };
    }

    // ---- the tour ----------------------------------------------------------------------------------

    private void BuildTour()
    {
        Tabs.ItemsSource = Scenarios.All
            .Select(scenario => new Tab(scenario.Name, scenario))
            .Append(new Tab("Sandbox — free run", null))
            .ToArray();

        Tabs.SelectionChanged += (_, _) => Show(Tabs.SelectedItem as Tab);

        Back.Click += (_, _) => GoTo(_step - 1);
        Next.Click += (_, _) => GoTo(_step + 1);
        Again.Click += (_, _) => GoTo(_step);
        Sandbox.Click += (_, _) => Tabs.SelectedIndex = Tabs.ItemCount - 1;

        CopyCode.Click += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(StepCode.Text ?? "");
        };
    }

    private void Show(Tab? tab)
    {
        Release();

        _scenario = tab?.Scenario;

        var tour = _scenario is not null;
        StepPane.IsVisible = tour;
        SandboxPane.IsVisible = !tour;
        StepBar.IsVisible = tour;

        if (tour) GoTo(0);
        else StartSelectedSession();
    }

    private void GoTo(int index)
    {
        if (_scenario is not { } scenario) return;

        _step = Math.Clamp(index, 0, scenario.Steps.Count - 1);
        var step = scenario.Steps[_step];

        Release();

        ScenarioName.Text = scenario.Name;
        StepTitle.Text = step.Title;
        StepText.Text = step.Explanation;
        StepCode.Text = step.Code;

        Progress.Text = $"step {_step + 1} of {scenario.Steps.Count}";
        Back.IsEnabled = _step > 0;
        Next.IsEnabled = _step < scenario.Steps.Count - 1;

        PtyNote.IsVisible = scenario.NeedsPty && _host.Platform == "browser";
        PtyNote.Text = "This scenario starts real programs, which needs a pty — and a browser tab has "
                     + "none. The steps say so rather than pretending; the code is what you would write, "
                     + "and it runs unchanged on a desktop.";

        Run(step);
    }

    private async void Run(Step step)
    {
        var context = new StepContext(Terminal, Note);
        _context = context;

        try
        {
            await step.Run(context);
        }
        catch (Exception e)
        {
            // A step is a piece of the demo, not of the library. One that throws should say so here
            // rather than take the window down.
            Note($"the step failed: {e.Message}");
        }
    }

    /// <summary>Lets go of whatever the last step or session was holding.</summary>
    private void Release()
    {
        _context?.Release();
        _context = null;

        _session?.Dispose();
        _session = null;

        Terminal.Stop();
    }

    // ---- the sandbox -------------------------------------------------------------------------------

    private void BuildSandbox()
    {
        Sessions.ItemsSource = _host.Sessions;
        Sessions.SelectedIndex = 0;
        Sessions.SelectionChanged += (_, _) =>
        {
            if (SandboxPane.IsVisible) StartSelectedSession();
        };

        Restart.Click += (_, _) => StartSelectedSession();
        StopIt.Click += (_, _) =>
        {
            Release();
            Note("stopped");
        };

        // The same call whether a program, a shell or nothing is running: at a prompt an interrupt
        // means "abandon the line", and there is no process to send it to.
        Interrupt.Click += (_, _) =>
        {
            Terminal.Signal(PtySignal.Int);
            Terminal.Focus();
        };

        BuildAppearance();
        BuildSize();
        BuildSend();
        BuildPlays();
        BuildEvents();
    }

    private void StartSelectedSession()
    {
        if (Sessions.SelectedItem is not DemoSession session) return;

        Release();
        SessionNote.Text = session.Description;

        try
        {
            _session = session.Start(Terminal);
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

    private void BuildAppearance()
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
            var chosen = DemoThemes.All[Math.Max(0, Themes.SelectedIndex)];
            Terminal.TerminalTheme = chosen.Get();
            Note($"theme: {chosen.Name}");
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

    private void BuildSize()
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

    private void BuildSend() =>
        ToSend.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;

            Terminal.Send(ToSend.Text + "\r");
            Note($"sent: {ToSend.Text}");
            ToSend.Text = "";
            e.Handled = true;
        };

    private void BuildPlays()
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

    private void BuildEvents()
    {
        ReadScreen.Click += (_, _) =>
        {
            var text = Terminal.Screen.GetScreenText().TrimEnd();
            var lines = text.Split('\n');
            Note($"read {lines.Length} lines from the screen; the last one is \"{lines[^1].Trim()}\"");
        };

        ClearLog.Click += (_, _) => Log.Text = "";
    }

    // ---- events ------------------------------------------------------------------------------------

    /// <summary>
    /// Every event the control raises, wired once.
    /// </summary>
    /// <remarks>
    /// <c>ScreenChanged</c> fires for every chunk of output, so it updates the status line and nothing
    /// else — a log entry per chunk would be a log nobody can read. <c>Input</c> is the same and is
    /// behind a checkbox in the Sandbox; the tour turns it on for the steps that are about it.
    /// </remarks>
    private void WatchTheTerminal()
    {
        Terminal.ScreenChanged += ShowStatus;

        Terminal.TitleChanged += title => Note($"TitleChanged: {title}");
        Terminal.Bell += () => Note("Bell — rung by the program, and deliberately not sounded");
        Terminal.Exited += code => Note($"Exited: {code}. IsRunning is now {Terminal.IsRunning}");

        Terminal.Input += bytes =>
        {
            if (LogInput.IsChecked == true) Note($"Input: {Scenarios.Readable(bytes.Span)}");
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
        Status.Text = new StringBuilder()
            .Append($"{Terminal.Columns}x{Terminal.Rows}")
            .Append($"   cell {cell.Width:0.##}x{cell.Height:0.##} dip")
            .Append(Terminal.IsRunning ? "   running" : "   nothing running")
            .Append(Terminal.Pid is { } pid ? $"   pid {pid}" : "")
            .Append(Terminal.Screen.IsAlternateScreen ? "   alternate screen" : "")
            .Append(Terminal.Title.Length > 0 ? $"   title: {Terminal.Title}" : "")
            .ToString();
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
