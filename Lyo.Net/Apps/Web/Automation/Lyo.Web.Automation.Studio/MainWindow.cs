using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Lyo.Desktop;
using Lyo.Desktop.Components;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Record;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Studio;

internal sealed class MainWindow : Window
{
    private const int ToolNavigate = 0;
    private const int ToolElement = 1;
    private const int ToolNetwork = 2;

    private static readonly (ElementLocatorKind Kind, string Label)[] LocatorKinds = [
        (ElementLocatorKind.CssSelector, "CSS"), (ElementLocatorKind.Id, "Id"), (ElementLocatorKind.Name, "Name"), (ElementLocatorKind.XPath, "XPath"),
        (ElementLocatorKind.LinkText, "Link text"), (ElementLocatorKind.PartialLinkText, "Partial link text"), (ElementLocatorKind.ClassName, "Class"),
        (ElementLocatorKind.TagName, "Tag")
    ];

    private static readonly string[] RecordKinds = [
        "Navigate", "Reload", "Delay", "Find", "Click", "Type", "Clear", "Store literal", "Store page URL", "Store page title",
        "Capture network", "Store captured URLs", "Viewport", "Open tab", "Switch tab", "Close tab", "Grabbed text", "Grabbed images", "Grabbed links"
    ];

    private readonly TextBox _address = new() { PlaceholderText = "https://", MinWidth = 360 };
    private readonly Button _clearElement;
    private readonly Button _clearNetwork;
    private readonly Button _click;
    private readonly Button _closeSession;
    private readonly NameValueList _context = new();
    private readonly Button _delete;
    private readonly ToolDockState _dock = ToolDockState.Create(("log", "Log", ToolDockEdge.Bottom, true));
    private readonly Dictionary<string, Control> _dockPanes = [];
    private readonly NameValueList _elementInfo = new();
    private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _find;
    private readonly TextBlock _foundLabel = new() { Text = "No element", TextWrapping = TextWrapping.Wrap };
    private readonly Button _go;
    private readonly Button _grabWatch;
    private readonly StudioHotkeySettings _hotkeysSettings;

    private readonly StudioLaunch _launch;
    private readonly Button _listen;
    private readonly TextBox _locator = new() { PlaceholderText = "Locator", MinWidth = 240 };
    private readonly ComboBox _locatorKind = new() { MinWidth = 140 };
    private readonly LogSink _log = new();
    private readonly LogSinkLoggerProvider _logProvider;
    private readonly LogView _logView = new();
    private readonly TextBox _netFilter = new() { PlaceholderText = "URL regex", MinWidth = 220 };
    private readonly List<AutomationStudioNetworkRow> _netRows = [];
    private readonly ListBox _network = new() { MinHeight = 120, MaxHeight = 200 };
    private readonly List<BrowserSession> _open = [];
    private readonly StackPanel _outcomeBody = new() { Spacing = 8, IsVisible = false };
    private readonly TextBlock _outcomeHint = new() { Text = "This step has not been run.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _pageTitle = new() { Text = "Title unavailable", TextWrapping = TextWrapping.Wrap };
    private readonly Button _pause;
    private readonly ServiceProvider _provider;
    private readonly TextBox _recordDelay = new() { PlaceholderText = "Milliseconds", Text = "250", Width = 140 };
    private readonly TextBox _recordHeight = new() { PlaceholderText = "Height", Text = "800", Width = 80 };
    private readonly ComboBox _recordKind = new() { MinWidth = 220 };
    private readonly TextBox _recordLocator = new() { PlaceholderText = "Locator", MinWidth = 240 };
    private readonly ComboBox _recordLocatorKind = new() { MinWidth = 140 };
    private readonly StackPanel _recordLocatorRow = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly TextBox _recordPattern = new() { PlaceholderText = "URL regex" };
    private readonly TextBox _recordTab = new() { PlaceholderText = "Tab index", Text = "0", Width = 90 };
    private readonly TextBox _recordText = new() { PlaceholderText = "Text" };
    private readonly Button _recordToggle;
    private readonly TextBox _recordUrl = new() { PlaceholderText = "URL" };
    private readonly TextBox _recordVariable = new() { PlaceholderText = "Variable", Text = "value", MinWidth = 160 };
    private readonly StackPanel _recordViewportRow = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly TextBox _recordWidth = new() { PlaceholderText = "Width", Text = "1280", Width = 80 };
    private readonly Button _reload;
    private readonly Button _replay;
    private readonly NameValueList _request = new();
    private readonly JsonEditor _requestJson = new();
    private readonly NameValueList _response = new();
    private readonly JsonEditor _responseJson = new();
    private readonly Button _resume;
    private readonly Button _run;
    private readonly Button _runFrom;
    private readonly Button _runStep;
    private readonly ListBox _runSteps = new();
    private readonly Button _save;
    private readonly Tabber _sessions = new();
    private readonly Image _shot = new() { MaxHeight = 180, Stretch = Stretch.Uniform };

    private readonly TextBlock _shotHint = new() {
        Text = "No screenshot",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Opacity = 0.7
    };

    private readonly Button _showLog;
    private readonly Button _startChord;
    private readonly Chip _status = new() { Text = "idle" };
    private readonly IdText _stepId = new();
    private readonly TextBox _stepLog = new() { IsReadOnly = true, AcceptsReturn = true, MinHeight = 60 };
    private readonly ListBox _steps = new();
    private readonly Button _stop;
    private readonly Button _stopChord;
    private readonly Button _tabClose;
    private readonly ListBox _tabList = new() { MinHeight = 80, MaxHeight = 140 };
    private readonly Button _tabOpen;
    private readonly List<AutomationTabInfo> _tabRows = [];
    private readonly Button _tabSwitch;
    private readonly DispatcherTimer _tick;
    private readonly Button _type;
    private readonly List<(string Type, Chip Chip)> _typeChips = [];
    private readonly TextBox _typeText = new() { PlaceholderText = "Text to type", MinWidth = 200 };

    private readonly TextBlock _url = new() {
        Text = "No browser",
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxWidth = 520
    };

    private readonly Button _viewportApply;
    private readonly TextBox _viewportHeight = new() { Text = "800", PlaceholderText = "Height", Width = 80 };
    private readonly TextBox _viewportWidth = new() { Text = "1280", PlaceholderText = "Width", Width = 80 };
    private readonly Tabber _views;
    private Bitmap? _bitmap;
    private string? _chordBeforeListen;
    private bool _closing;
    private int _describing;
    private Grid? _dockHost;
    private int _dragFrom = -1;
    private IWebAutomationElement? _found;
    private IGlobalHotkeys? _hotkeys;
    private Button? _listening;
    private string? _netType;
    private int _nextSession = 1;
    private BrowserSession? _selected;
    private string? _targetKey;
    private int _titling;
    private int _toolView;
    private int _view;

    public MainWindow(StudioLaunch launch)
    {
        _launch = launch;
        Title = "Automation studio";
        Width = 1200;
        Height = 800;
        var services = new ServiceCollection();
        services.AddPlaywrightBrowserService(options => options.Headless = false);
        services.AddWebAutomationPlanRunner();
        _provider = services.BuildServiceProvider();
        _logProvider = new(_log);
        foreach (var kind in LocatorKinds) {
            _locatorKind.Items.Add(kind.Label);
            _recordLocatorKind.Items.Add(kind.Label);
        }

        _locatorKind.SelectedIndex = 0;
        _recordLocatorKind.SelectedIndex = 0;
        _recordLocatorRow.Children.Add(_recordLocatorKind);
        _recordLocatorRow.Children.Add(_recordLocator);
        _recordViewportRow.Children.Add(_recordWidth);
        _recordViewportRow.Children.Add(_recordHeight);
        foreach (var kind in RecordKinds)
            _recordKind.Items.Add(kind);

        _recordKind.SelectedIndex = 0;
        _recordKind.SelectionChanged += (_, _) => ShowRecordFields();
        _logView.Sink = _log;
        _dock.Bottom = 160;
        _dockPanes["log"] = _logView;
        _showLog = Button("Log", (_, _) => ShowLog());
        _showLog.IsVisible = false;
        _closeSession = Button("Close session", (_, _) => CloseSelectedSession());
        (_run, _runFrom, _runStep, _pause, _resume, _stop, _save, _recordToggle, _delete) = PlanButtons();
        (_go, _reload, _find, _click, _type, _clearElement) = FindButtons();
        (_grabWatch, _listen, _clearNetwork, _replay) = ToolButtons();
        _hotkeysSettings = StudioHotkeyStore.Load();
        _startChord = Button(_hotkeysSettings.StartWatch, OnChordClick);
        _stopChord = Button(_hotkeysSettings.StopWatch, OnChordClick);
        (_viewportApply, _tabOpen, _tabSwitch, _tabClose) = PageButtons();
        _views = BuildViews();
        _views.SelectionChanged += (_, _) => OnViewChanged();
        _sessions.TabClosed += (_, index) => OnSessionClosed(index);
        _sessions.SelectionChanged += (_, _) => OnSessionChanged();
        _tick = new() { Interval = TimeSpan.FromMilliseconds(500) };
        _tick.Tick += (_, _) => RefreshLive();
        _tick.Start();
        _steps.SelectionChanged += (_, _) => ApplyCommands();
        _steps.AddHandler(PointerPressedEvent, OnStepPressed, RoutingStrategies.Tunnel);
        _steps.AddHandler(PointerMovedEvent, OnStepMoved, RoutingStrategies.Tunnel);
        _steps.AddHandler(PointerReleasedEvent, OnStepReleased, RoutingStrategies.Tunnel);
        _runSteps.SelectionChanged += (_, _) => {
            ShowExecuted();
            ApplyCommands();
        };

        _network.SelectionChanged += (_, _) => {
            ShowRequest();
            ApplyCommands();
        };

        _netFilter.TextChanged += (_, _) => {
            RefreshNetwork();
            ApplyCommands();
        };

        Content = Build();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        StartHotkeys();
        Closing += OnClosing;
        ApplyCommands();
    }

    private (Button Run, Button RunFrom, Button RunStep, Button Pause, Button Resume, Button Stop, Button Save, Button Record, Button Delete) PlanButtons()
        => (Button("Run", (_, _) => _ = RunAsync(0, null)), Button("Run from here", (_, _) => _ = RunAsync(SelectedStep(), null)), Button(
                "Run this step", (_, _) => {
                    var index = SelectedStep();
                    _ = RunAsync(index, index);
                }), Button("Pause", (_, _) => PauseRun()), Button("Resume", (_, _) => ResumeRun()), Button("Stop", (_, _) => StopRun()), Button("Save", (_, _) => Save()),
            Button("Record", (_, _) => _ = ToggleRecordAsync()), Button("Delete", (_, _) => Delete()));

    private (Button Go, Button Reload, Button Find, Button Click, Button Type, Button Clear) FindButtons()
        => (Button("Go", (_, _) => _ = GoAsync()), Button("Reload", (_, _) => _ = ReloadAsync()), Button("Find", (_, _) => _ = FindAsync()),
            Button("Click", (_, _) => _ = ClickFoundAsync()), Button("Type", (_, _) => _ = TypeFoundAsync()), Button("Clear", (_, _) => _ = ClearFoundAsync()));

    private (Button Watch, Button Listen, Button ClearNetwork, Button Replay) ToolButtons()
        => (Button("Watch cursor", (_, _) => _ = ToggleGrabberAsync()), Button("Listen", (_, _) => ToggleListen()), Button("Clear", (_, _) => ClearCaptured()),
            Button("Replay", (_, _) => _ = ReplayAsync()));

    private (Button Apply, Button Open, Button Switch, Button Close) PageButtons()
        => (Button("Apply", (_, _) => _ = ApplyViewportAsync()), Button("Open tab", (_, _) => _ = OpenTabAsync()), Button("Switch", (_, _) => _ = SwitchTabAsync()),
            Button("Close tab", (_, _) => _ = CloseTabAsync()));

    private Control Build()
    {
        var root = new DockPanel();
        var top = Row(_closeSession, _status, _url);
        top.Children.Insert(0, Button("New session", (_, _) => _ = StartBrowserAsync()));
        top.Children.Insert(1, _showLog);
        top.Margin = new(8);
        DockPanel.SetDock(top, Dock.Top);
        root.Children.Add(top);
        DockPanel.SetDock(_sessions, Dock.Top);
        root.Children.Add(_sessions);
        _dockHost = new();
        root.Children.Add(_dockHost);
        ApplyDock();
        return root;
    }

    private void ShowLog()
    {
        _dock.SetVisible("log", true);
        if (_dock.Pane("log").Minimized)
            _dock.ToggleMinimized("log");

        ApplyDock();
    }

    private void ApplyDock()
    {
        if (_dockHost == null)
            return;

        ToolDockBuilder.Fill(_dockHost, _dock, _dockPanes, _views, ApplyDock);
        _showLog.IsVisible = !_dock.IsVisible("log");
    }

    private Tabber BuildViews()
    {
        var tabber = new Tabber();
        tabber.Add(new() { Header = "Record", Content = RecordPage() });
        tabber.Add(new() { Header = "Run", Content = RunPage() });
        tabber.Add(new() { Header = "Tools", Content = ToolsPage() });
        return tabber;
    }

    private Control RecordPage()
    {
        var root = new DockPanel();
        var form = new StackPanel { Spacing = 8, Margin = new(8, 8, 8, 0) };
        form.Children.Add(_recordKind);
        form.Children.Add(_recordUrl);
        form.Children.Add(_recordLocatorRow);
        form.Children.Add(_recordText);
        form.Children.Add(_recordVariable);
        form.Children.Add(_recordDelay);
        form.Children.Add(_recordViewportRow);
        form.Children.Add(_recordTab);
        form.Children.Add(_recordPattern);
        form.Children.Add(Row(_recordToggle, Button("Add", (_, _) => AddRecordStep()), _delete, _save));
        DockPanel.SetDock(form, Dock.Top);
        root.Children.Add(form);
        _steps.Margin = new(8);
        root.Children.Add(_steps);
        ShowRecordFields();
        return root;
    }

    private Control RunPage()
    {
        var root = new DockPanel();
        var bar = Row(_run, _runFrom, _runStep, _pause, _resume, _stop);
        bar.Margin = new(8, 8, 8, 0);
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);
        _outcomeBody.Children.Add(_error);
        _outcomeBody.Children.Add(_stepId);
        _outcomeBody.Children.Add(_context);
        _outcomeBody.Children.Add(_stepLog);
        var outcome = new StackPanel { Spacing = 8, Margin = new(8) };
        outcome.Children.Add(_outcomeHint);
        outcome.Children.Add(_outcomeBody);
        var outcomeScroll = new ScrollViewer { Content = outcome, MaxHeight = 180 };
        DockPanel.SetDock(outcomeScroll, Dock.Bottom);
        root.Children.Add(outcomeScroll);
        root.Children.Add(RunSplit());
        return root;
    }

    private Control RunSplit()
    {
        _shot.MaxHeight = double.PositiveInfinity;
        _shot.Stretch = Stretch.Uniform;
        var shot = new Grid { Margin = new(8) };
        shot.Children.Add(_shot);
        shot.Children.Add(_shotHint);
        var grid = new Grid { Margin = new(8, 8, 8, 0) };
        grid.ColumnDefinitions.Add(new(1, GridUnitType.Star) { MinWidth = 220 });
        grid.ColumnDefinitions.Add(new(GridLength.Auto));
        grid.ColumnDefinitions.Add(new(1.2, GridUnitType.Star) { MinWidth = 220 });
        var splitter = Splitter();
        Grid.SetColumn(_runSteps, 0);
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(shot, 2);
        grid.Children.Add(_runSteps);
        grid.Children.Add(splitter);
        grid.Children.Add(shot);
        return grid;
    }

    private Control ToolsPage()
    {
        var tools = new Tabber();
        tools.Add(new() { Header = "Navigate", Content = new ScrollViewer { Content = NavigatePage() } });
        tools.Add(new() { Header = "Element", Content = new ScrollViewer { Content = ElementPage() } });
        tools.Add(new() { Header = "Network", Content = NetworkPage() });
        tools.SelectionChanged += (_, _) => {
            _toolView = tools.SelectedIndex;
            if (_toolView == ToolNetwork)
                RefreshNetwork();

            if (_toolView == ToolNavigate)
                _ = RefreshTabsAsync();
        };

        return tools;
    }

    private Control NavigatePage()
    {
        var page = new StackPanel { Spacing = 8, Margin = new(8) };
        page.Children.Add(_address);
        page.Children.Add(Row(_go, _reload));
        page.Children.Add(_pageTitle);
        page.Children.Add(Row(_viewportWidth, _viewportHeight, _viewportApply));
        page.Children.Add(_tabList);
        page.Children.Add(Row(_tabOpen, _tabSwitch, _tabClose));
        return page;
    }

    private Control ElementPage()
    {
        var page = new StackPanel { Spacing = 8, Margin = new(8) };
        page.Children.Add(Heading("Target"));
        page.Children.Add(_grabWatch);
        page.Children.Add(Row(ChordLabel("Start"), _startChord, ChordLabel("Stop"), _stopChord));
        page.Children.Add(Row(_locatorKind, _locator, _find));
        page.Children.Add(_foundLabel);
        page.Children.Add(_elementInfo);
        page.Children.Add(Heading("Actions"));
        page.Children.Add(Row(_typeText, _type));
        page.Children.Add(Row(_click, _clearElement));
        return page;
    }

    private Control NetworkPage()
    {
        _network.MinHeight = 80;
        _network.MaxHeight = double.PositiveInfinity;
        var left = new DockPanel();
        var filters = new StackPanel { Spacing = 8 };
        filters.Children.Add(Row(_listen, _netFilter));
        filters.Children.Add(TypeFilters());
        DockPanel.SetDock(filters, Dock.Top);
        left.Children.Add(filters);
        var actions = Row(_clearNetwork, _replay);
        actions.Margin = new(0, 8, 0, 0);
        DockPanel.SetDock(actions, Dock.Bottom);
        left.Children.Add(actions);
        left.Children.Add(_network);
        var right = new Tabber();
        right.Add(new() { Header = "Request", Content = Detail(_request, _requestJson) });
        right.Add(new() { Header = "Response", Content = Detail(_response, _responseJson) });
        var grid = new Grid { Margin = new(8) };
        grid.ColumnDefinitions.Add(new(1, GridUnitType.Star) { MinWidth = 280 });
        grid.ColumnDefinitions.Add(new(GridLength.Auto));
        grid.ColumnDefinitions.Add(new(1.2, GridUnitType.Star) { MinWidth = 280 });
        var splitter = Splitter();
        Grid.SetColumn(left, 0);
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(right, 2);
        grid.Children.Add(left);
        grid.Children.Add(splitter);
        grid.Children.Add(right);
        return grid;
    }

    private static GridSplitter Splitter() => new() { Width = 6, ResizeDirection = GridResizeDirection.Columns, Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x41, 0x50)) };

    private Control TypeFilters()
    {
        var wrap = new WrapPanel();
        foreach (var type in NetworkTypeFilter.Types) {
            var chip = new Chip { Text = type == "all" ? "All" : type, Margin = new(0, 0, 6, 6) };
            var key = type;
            chip.PointerPressed += (_, e) => {
                _netType = key == "all" ? null : key;
                PaintTypeChips();
                RefreshNetwork();
                e.Handled = true;
            };

            _typeChips.Add((key, chip));
            wrap.Children.Add(chip);
        }

        PaintTypeChips();
        return wrap;
    }

    private void PaintTypeChips()
    {
        foreach (var (type, chip) in _typeChips) {
            var selected = type == "all" ? _netType == null : string.Equals(type, _netType, StringComparison.OrdinalIgnoreCase);
            chip.Hue = selected ? type == "all" ? ChipHues.Blue : NetworkChip.Type(type).Hue : -1;
        }
    }

    private void OnViewChanged()
    {
        var next = _views.SelectedIndex;
        var previous = _view;
        _view = next;
        if (previous == 2 && next != 2)
            _ = SetGrabberAsync(_selected, false);

        if (next == 2) {
            _ = SetGrabberAsync(_selected, _selected?.Grabbing == true);
            _ = RefreshTabsAsync();
            if (_toolView == ToolNetwork)
                RefreshNetwork();

            if (_toolView == ToolNavigate)
                _ = RefreshTabsAsync();
        }

        ApplyCommands();
    }

    private async Task StartBrowserAsync()
    {
        try {
            var studio = new PlaywrightAutomationStudio(_provider.GetRequiredService<IPlaywrightBrowserService>(), _provider.GetRequiredService<IAutomationPlanRunner>());
            var browser = await studio.StartAsync(
                new() {
                    StartUrl = _launch.StartUrl,
                    PlanName = _launch.PlanName,
                    PlanPath = _launch.PlanPath,
                    OutputDirectory = _launch.OutputDirectory,
                    Headless = false
                });

            var session = new BrowserSession { Number = _nextSession++, Studio = browser };
            browser.PlayLogger = _logProvider.CreateLogger("studio");
            browser.StepNotice += (_, notice) => Dispatcher.UIThread.Post(() => ApplyNotice(session, notice));
            browser.PlanChanged += (_, _) => Dispatcher.UIThread.Post(() => {
                if (session == _selected)
                    RefreshSteps();
            });

            _open.Add(session);
            _sessions.Add(new() { Header = SessionTitle(session, _launch.StartUrl), Content = new Panel { Height = 0 }, CanClose = true });
            _sessions.Select(_open.Count - 1);
            _log.Write($"Session {session.Number} started.");
            _ = WatchClosed(session);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task WatchClosed(BrowserSession session)
    {
        try {
            await session.Studio.Completion;
        }
        catch (Exception) {
            // The browser is already gone.
        }

        await Dispatcher.UIThread.InvokeAsync(() => {
            if (_closing || !_open.Contains(session) || !session.BrowserOpen)
                return;

            session.BrowserOpen = false;
            session.Playing = false;
            session.Paused = false;
            session.Listening = false;
            session.Grabbing = false;
            session.Studio.StopRecording();
            SetStatus(session, "closed", null);
            var tab = _open.IndexOf(session);
            if (tab >= 0)
                _sessions.Rename(tab, SessionTitle(session, null));

            if (session == _selected) {
                _url.Text = "Browser closed";
                _found = null;
                ShowTarget(null, "No element");
                BindChrome();
            }
        });
    }

    private void CloseSelectedSession()
    {
        if (_sessions.SelectedIndex >= 0)
            _sessions.Close(_sessions.SelectedIndex);
    }

    private void OnSessionClosed(int index)
    {
        if (index < 0 || index >= _open.Count)
            return;

        var session = _open[index];
        _open.RemoveAt(index);
        if (_selected == session)
            _selected = null;

        _ = StopQuiet(session);
    }

    private async Task StopQuiet(BrowserSession session)
    {
        try {
            await session.Studio.StopAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private void OnSessionChanged()
    {
        var previous = _selected;
        var index = _sessions.SelectedIndex;
        _selected = index >= 0 && index < _open.Count ? _open[index] : null;
        if (previous != null && previous != _selected && previous.Grabbing && previous.BrowserOpen)
            _ = previous.Studio.SetGrabberAsync(false);

        _found = null;
        _targetKey = null;
        ShowTarget(null, "No element");
        if (_selected?.Grabbing == true && _view == 2)
            _ = SetGrabberAsync(_selected, true);

        _address.Text = _selected?.BrowserOpen == true ? PageUrl(_selected) ?? "" : "";
        BindChrome();
        if (_view == 2)
            _ = RefreshTabsAsync();
    }

    private void BindChrome()
    {
        var session = _selected;
        if (session == null)
            _url.Text = "No browser";
        else if (!session.BrowserOpen)
            _url.Text = "Browser closed";
        else if (PageUrl(session) is { } url)
            _url.Text = url;

        PaintStatus(session?.Status ?? "idle", session?.StatusStep);
        RefreshSteps();
        if (_view == 2 && _toolView == ToolNetwork)
            RefreshNetwork();

        ApplyCommands();
    }

    private void ApplyCommands()
    {
        var session = _selected;
        var plan = session?.Studio.Plan;
        var state = StudioCommandState.From(
            new() {
                HasSession = session != null,
                BrowserOpen = session?.BrowserOpen == true,
                Playing = session?.Playing == true,
                Paused = session?.Paused == true,
                StepCount = plan?.Steps.Count ?? 0,
                SelectedStep = _steps.SelectedIndex,
                RunSelectedStep = _runSteps.SelectedIndex,
                NetworkCount = session == null ? 0 : session.Studio.NetworkRows(null).Count,
                NetworkRowSelected = _network.SelectedIndex >= 0 && _network.SelectedIndex < _netRows.Count,
                CaptureVariableFilled = !string.IsNullOrWhiteSpace(_recordVariable.Text),
                HasCapturedMatch = session != null && session.Studio.NetworkRows(_netFilter.Text).Count > 0
            });

        _closeSession.IsEnabled = session != null;
        _go.IsEnabled = state.Go;
        _reload.IsEnabled = state.Reload;
        _find.IsEnabled = state.Find;
        _click.IsEnabled = state.Click;
        _type.IsEnabled = state.Type;
        _clearElement.IsEnabled = state.ClearElement;
        _listen.IsEnabled = state.Listen;
        _listen.Content = session?.Listening == true ? "Stop listening" : "Listen";
        _replay.IsEnabled = state.Replay;
        _grabWatch.IsEnabled = state.Grabber;
        _grabWatch.Content = session?.Grabbing == true ? "Stop watching" : "Watch cursor";
        _viewportApply.IsEnabled = state.ViewportApply;
        _tabOpen.IsEnabled = state.TabActions;
        _tabSwitch.IsEnabled = state.TabActions;
        _tabClose.IsEnabled = state.TabActions;
        _run.IsEnabled = state.Run;
        _runFrom.IsEnabled = state.RunFromHere;
        _runStep.IsEnabled = state.RunThisStep;
        _pause.IsEnabled = state.Pause;
        _resume.IsEnabled = state.Resume;
        _stop.IsEnabled = state.Stop;
        _delete.IsEnabled = state.Delete;
        _recordToggle.IsEnabled = state.Go;
        _recordToggle.Content = session?.Studio.IsRecording == true ? "Stop recording" : "Record";
        _save.IsEnabled = state.Save;
        _clearNetwork.IsEnabled = state.ClearNetwork;
    }

    private void ApplyNotice(BrowserSession session, AutomationStudioStepNotice notice)
    {
        if (!_open.Contains(session))
            return;

        if (notice.Phase == "log") {
            var current = session.Done.LastOrDefault(step => step.Index == notice.StepIndex) ?? session.Done.LastOrDefault();
            if (current != null && notice.LogLine != null)
                current.Log.Add(notice.LogLine);

            if (session == _selected)
                ShowExecuted();

            return;
        }

        if (notice.Phase == "start") {
            session.Done.Add(new() { Index = notice.StepIndex, Label = notice.Label ?? $"Step {notice.StepIndex + 1}" });
            SetStatus(session, "running", notice.Label);
            if (session == _selected) {
                RefreshSteps();
                if (notice.StepIndex >= 0 && notice.StepIndex < _runSteps.ItemCount)
                    _runSteps.SelectedIndex = notice.StepIndex;
            }

            return;
        }

        if (notice.Phase != "outcome")
            return;

        var step = session.Done.LastOrDefault(item => item.Index == notice.StepIndex);
        if (step != null)
            step.Outcome = notice;

        var name = notice.Outcome?.ToString() ?? "failed";
        if (notice.Outcome is AutomationPlanStepOutcome.Failed or AutomationPlanStepOutcome.TimedOut or AutomationPlanStepOutcome.Cancelled)
            SetStatus(session, name.ToLowerInvariant(), step?.Label);

        if (session == _selected)
            RefreshSteps();
    }

    private async Task RunAsync(int start, int? end)
    {
        var session = _selected;
        var plan = session?.Studio.Plan;
        if (session == null || !session.BrowserOpen || plan == null || plan.Steps.Count == 0 || session.Playing)
            return;

        session.Done.Clear();
        session.Studio.StopRecording();
        session.Playing = true;
        session.Paused = false;
        ApplyCommands();
        ShowExecuted();
        var directory = Path.Combine(_launch.OutputDirectory, "runs");
        Directory.CreateDirectory(directory);
        try {
            SetStatus(session, "running", LabelOf(plan, start));
            await session.Studio.PlayAsync(plan, start, directory, CancellationToken.None, end);
            if (session.BrowserOpen && session.Status == "running")
                SetStatus(session, "completed", LabelOf(plan, Math.Min(end ?? plan.Steps.Count - 1, plan.Steps.Count - 1)));
        }
        catch (OperationCanceledException) {
            if (session.BrowserOpen)
                SetStatus(session, "cancelled", CurrentLabel(session));
        }
        catch (Exception ex) {
            if (session.BrowserOpen)
                SetStatus(session, "failed", CurrentLabel(session));

            if (session == _selected)
                _error.Text = ex.Message;

            _log.Write(ex.Message);
        }
        finally {
            session.Playing = false;
            session.Paused = false;
            ApplyCommands();
            ShowExecuted();
        }
    }

    private void PauseRun()
    {
        if (_selected == null)
            return;

        _selected.Studio.PausePlay();
        _selected.Paused = true;
        SetStatus(_selected, "paused", CurrentLabel(_selected));
        ApplyCommands();
    }

    private void ResumeRun()
    {
        if (_selected == null)
            return;

        _selected.Studio.ResumePlay();
        _selected.Paused = false;
        SetStatus(_selected, "running", CurrentLabel(_selected));
        ApplyCommands();
    }

    private void StopRun() => _selected?.Studio.CancelPlay();

    private async Task GoAsync()
    {
        var session = _selected;
        var url = StudioAddress.Normalize(_address.Text);
        if (session == null || !session.BrowserOpen || url == null)
            return;

        try {
            _address.Text = url;
            await session.Studio.Browser.NavigateToAsync(url);
            _url.Text = url;
            _log.Write("Navigated to " + url);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task ReloadAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen)
            return;

        try {
            await session.Studio.Browser.Navigator.ReloadAsync();
            _log.Write("Reloaded the page.");
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task FindAsync()
    {
        var session = _selected;
        var value = _locator.Text?.Trim();
        if (session == null || !session.BrowserOpen || string.IsNullOrWhiteSpace(value))
            return;

        try {
            var locator = new ElementLocator(SelectedKind(), value);
            _found = await session.Studio.Browser.CurrentPage.GetElementAsync(locator);
            if (_found == null) {
                ShowTarget(null, "No match");
                return;
            }

            _targetKey = locator.Kind + " " + locator.Value;
            await session.Studio.HighlightAsync(locator);
            var tag = await _found.GetTagNameAsync();
            var text = await _found.GetTextAsync();
            var info = new AutomationStudioElementInfo {
                Tag = tag,
                Text = text,
                Url = PageUrl(session),
                Locator = new() { Kind = locator.Kind.ToString(), Value = locator.Value },
                Attributes = await TargetAttributes(_found)
            };

            ShowTarget(info, "Find");
        }
        catch (Exception ex) {
            _found = null;
            _targetKey = null;
            ShowTarget(null, "No match");
            _log.Write(ex.Message);
        }
    }

    private void ShowTarget(AutomationStudioElementInfo? info, string source)
    {
        _foundLabel.Text = source;
        if (info == null) {
            _elementInfo.Rows = [];
            return;
        }

        var rows = new List<NameValueRow> {
            new() { Name = "Tag", Value = info.Tag },
            new() { Name = "Text", Value = info.Text },
            new() { Name = "Locator", Value = info.Locator == null ? null : info.Locator.Kind + " " + info.Locator.Value },
            new() { Name = "Page", Value = info.Url },
            new() { Name = "Title", Value = info.Title }
        };

        if (info.Attributes != null) {
            foreach (var pair in info.Attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                rows.Add(new() { Name = pair.Key, Value = pair.Value });
        }

        AddMany(rows, "Link", info.Links);
        AddMany(rows, "Image", info.Images);
        _elementInfo.Rows = rows;
    }

    private static void AddMany(List<NameValueRow> rows, string name, List<string>? values)
    {
        if (values == null || values.Count == 0)
            return;

        var extra = values.Count > 12 ? " (+" + (values.Count - 12) + ")" : "";
        rows.Add(new() { Name = name, Value = string.Join(Environment.NewLine, values.Take(12)) + extra });
    }

    private static ElementLocator? LocatorOf(AutomationStudioElementInfo info)
    {
        if (info.Locator?.Value is not string value || string.IsNullOrWhiteSpace(value))
            return null;

        if (!Enum.TryParse<ElementLocatorKind>(info.Locator.Kind, true, out var kind))
            return null;

        return new(kind, value);
    }

    private static async Task<Dictionary<string, string>> TargetAttributes(IWebAutomationElement element)
    {
        var attributes = new Dictionary<string, string>();
        foreach (var name in new[] { "id", "class", "name", "href", "type", "placeholder" }) {
            var value = await element.GetAttributeAsync(name);
            if (!string.IsNullOrWhiteSpace(value))
                attributes[name] = value;
        }

        return attributes;
    }

    private async Task ClickFoundAsync()
    {
        if (_found == null) {
            _log.Write("Pick an element with Find or the cursor.");
            return;
        }

        try {
            await _found.ClickAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task TypeFoundAsync()
    {
        if (_found == null) {
            _log.Write("Pick an element with Find or the cursor.");
            return;
        }

        try {
            await _found.SendKeysAsync(_typeText.Text ?? "", true);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task ClearFoundAsync()
    {
        if (_found == null) {
            _log.Write("Pick an element with Find or the cursor.");
            return;
        }

        try {
            await _found.ClearAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task ToggleGrabberAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen)
            return;

        session.Grabbing = !session.Grabbing;
        await SetGrabberAsync(session, session.Grabbing);
        ApplyCommands();
    }

    private async Task StartWatchAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen || session.Grabbing)
            return;

        session.Grabbing = true;
        await SetGrabberAsync(session, true);
        ApplyCommands();
    }

    private void StopWatch()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen || !session.Grabbing)
            return;

        session.Grabbing = false;
        _ = SetGrabberAsync(session, false);
        ApplyCommands();
    }

    private void StartHotkeys()
    {
        try {
            _hotkeys = GlobalHotkeys.Create();
            BindHotkeys();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private void BindHotkeys()
    {
        if (_hotkeys == null || _listening != null)
            return;

        var start = HotkeyChord.Parse(ChordText(_startChord, "Ctrl+Shift+W"));
        var stop = HotkeyChord.Parse(ChordText(_stopChord, "Ctrl+Shift+Q"));
        if (start == stop)
            throw new InvalidOperationException("Start and stop need different shortcuts.");

        _hotkeys.Bind([new(start, () => Dispatcher.UIThread.Post(() => _ = StartWatchAsync())), new(stop, () => Dispatcher.UIThread.Post(StopWatch))]);
    }

    private void OnChordClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        _listening = button;
        _chordBeforeListen = button.Content?.ToString();
        button.Content = "Press a shortcut";
        try {
            _hotkeys?.Bind([]);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_listening == null)
            return;

        e.Handled = true;
        if (e.Key == Key.Escape) {
            _listening.Content = _chordBeforeListen;
            _listening = null;
            RebindHotkeys();
            return;
        }

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            return;

        var button = _listening;
        _listening = null;
        try {
            button.Content = HotkeyChord.Parse(FormatChord(e.KeyModifiers, e.Key)).ToString();
            BindHotkeys();
            _hotkeysSettings.StartWatch = ChordText(_startChord, "Ctrl+Shift+W");
            _hotkeysSettings.StopWatch = ChordText(_stopChord, "Ctrl+Shift+Q");
            StudioHotkeyStore.Save(_hotkeysSettings);
        }
        catch (Exception ex) {
            button.Content = _chordBeforeListen;
            _log.Write(ex.Message);
            RebindHotkeys();
        }
    }

    private void RebindHotkeys()
    {
        try {
            BindHotkeys();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private static string ChordText(Button button, string fallback)
    {
        var text = button.Content?.ToString();
        return string.IsNullOrWhiteSpace(text) || text == "Press a shortcut" ? fallback : text;
    }

    private static string FormatChord(KeyModifiers modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control))
            parts.Add("Ctrl");

        if (modifiers.HasFlag(KeyModifiers.Shift))
            parts.Add("Shift");

        if (modifiers.HasFlag(KeyModifiers.Alt))
            parts.Add("Alt");

        if (modifiers.HasFlag(KeyModifiers.Meta))
            parts.Add("Meta");

        parts.Add(KeyName(key));
        return string.Join('+', parts);
    }

    private static string KeyName(Key key)
        => key switch {
            >= Key.A and <= Key.Z => key.ToString(),
            >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
            Key.Escape => "Escape",
            Key.Space => "Space",
            Key.Tab => "Tab",
            var _ => key.ToString()
        };

    private static TextBlock ChordLabel(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center };

    private async Task SetGrabberAsync(BrowserSession? session, bool enabled)
    {
        if (session == null || !session.BrowserOpen)
            return;

        try {
            await session.Studio.SetGrabberAsync(enabled);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task InsertGrabAsync(string tool)
    {
        if (_selected == null)
            return;

        var error = await _selected.Studio.InsertGrabbedAsync(tool, VariableOr(tool));
        _log.Write(error ?? $"Added an extract step for {tool}.");
        RefreshSteps();
    }

    private void ToggleListen()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen)
            return;

        if (session.Listening)
            session.Studio.StopNetworkCapture();
        else
            session.Studio.StartNetworkCapture();

        session.Listening = !session.Listening;
        ApplyCommands();
    }

    private void ClearCaptured()
    {
        if (_selected == null)
            return;

        _selected.Studio.ClearNetwork();
        RefreshNetwork();
        ApplyCommands();
    }

    private async Task ReplayAsync()
    {
        if (_selected == null || _network.SelectedIndex < 0 || _network.SelectedIndex >= _netRows.Count)
            return;

        try {
            await _selected.Studio.ReplayNetworkAsync(_netRows[_network.SelectedIndex].Id);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private void StoreCaptured()
    {
        var error = _selected?.Studio.StoreCapturedUrls(VariableOr("urls"), _recordPattern.Text);
        _log.Write(error ?? "Added the captured URLs.");
        RefreshSteps();
    }

    private async Task ApplyViewportAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen || !TryViewport(out var width, out var height))
            return;

        try {
            await session.Studio.Browser.CurrentPage.SetViewportSizeAsync(width, height);
            _log.Write($"Viewport set to {width}×{height}.");
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task OpenTabAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen)
            return;

        try {
            await session.Studio.Browser.Tabs.OpenNewTabAsync(StudioAddress.Normalize(_address.Text));
            await RefreshTabsAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task SwitchTabAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen || _tabList.SelectedIndex < 0 || _tabList.SelectedIndex >= _tabRows.Count)
            return;

        try {
            await session.Studio.Browser.Tabs.SwitchToTabAsync(_tabRows[_tabList.SelectedIndex].Index);
            await RefreshTabsAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task CloseTabAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen)
            return;

        try {
            await session.Studio.Browser.Tabs.CloseCurrentTabAsync();
            await RefreshTabsAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private async Task RefreshTabsAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen) {
            _tabRows.Clear();
            _tabList.Items.Clear();
            return;
        }

        try {
            var tabs = await session.Studio.Browser.Tabs.ListTabsAsync();
            await Dispatcher.UIThread.InvokeAsync(() => {
                if (session != _selected)
                    return;

                var selected = _tabList.SelectedIndex;
                _tabRows.Clear();
                _tabRows.AddRange(tabs);
                _tabList.Items.Clear();
                foreach (var tab in tabs)
                    _tabList.Items.Add($"{tab.Index}. {tab.Title ?? tab.Url}");

                if (selected >= 0 && selected < _tabList.ItemCount)
                    _tabList.SelectedIndex = selected;
            });
        }
        catch (Exception) {
            // The window can close while the tab list is loading.
        }
    }

    private void ShowRequest()
    {
        if (_selected == null || _network.SelectedIndex < 0 || _network.SelectedIndex >= _netRows.Count) {
            ClearRequest();
            return;
        }

        var detail = _selected.Studio.ReadNetwork(_netRows[_network.SelectedIndex].Id);
        if (detail == null) {
            ClearRequest();
            return;
        }

        var request = new List<NameValueRow> { new() { Name = "Method", Value = detail.Method }, new() { Name = "URL", Value = detail.Url } };
        foreach (var header in detail.RequestHeaders)
            request.Add(new() { Name = header.Key, Value = header.Value });

        _request.Rows = request;
        var response = new List<NameValueRow> { new() { Name = "Status", Value = detail.Status?.ToString() } };
        foreach (var header in detail.ResponseHeaders)
            response.Add(new() { Name = header.Key, Value = header.Value });

        _response.Rows = response;
        _requestJson.Text = detail.RequestBody;
        _responseJson.Text = detail.ResponseBody;
    }

    private void ClearRequest()
    {
        _request.Rows = [];
        _response.Rows = [];
        _requestJson.Text = null;
        _responseJson.Text = null;
    }

    private static Control Detail(NameValueList headers, JsonEditor body)
    {
        var scroll = new ScrollViewer { Content = headers, MaxHeight = 180, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        DockPanel.SetDock(scroll, Dock.Top);
        var panel = new DockPanel();
        panel.Children.Add(scroll);
        panel.Children.Add(body);
        return panel;
    }

    private void RefreshNetwork()
    {
        if (_selected == null) {
            _netRows.Clear();
            _network.Items.Clear();
            ClearRequest();
            return;
        }

        var rows = _selected.Studio.NetworkRows(_netFilter.Text).Where(row => NetworkTypeFilter.Matches(row.ResourceType, _netType)).ToList();
        if (NetworkSameIds(rows)) {
            for (var i = 0; i < rows.Count; i++) {
                if (_network.Items[i] is NetworkRowView view)
                    view.Apply(rows[i]);
            }

            _netRows.Clear();
            _netRows.AddRange(rows);
            return;
        }

        if (NetworkAppended(rows)) {
            for (var i = 0; i < _netRows.Count; i++) {
                if (_network.Items[i] is NetworkRowView view)
                    view.Apply(rows[i]);
            }

            for (var i = _netRows.Count; i < rows.Count; i++)
                _network.Items.Add(new NetworkRowView(rows[i]));

            _netRows.Clear();
            _netRows.AddRange(rows);
            return;
        }

        var selected = _network.SelectedIndex >= 0 && _network.SelectedIndex < _netRows.Count ? _netRows[_network.SelectedIndex].Id : -1;
        _netRows.Clear();
        _netRows.AddRange(rows);
        _network.Items.Clear();
        foreach (var row in rows)
            _network.Items.Add(new NetworkRowView(row));

        var index = _netRows.FindIndex(row => row.Id == selected);
        if (index >= 0)
            _network.SelectedIndex = index;

        ApplyCommands();
    }

    private bool NetworkSameIds(IReadOnlyList<AutomationStudioNetworkRow> rows)
    {
        if (rows.Count != _netRows.Count)
            return false;

        for (var i = 0; i < rows.Count; i++) {
            if (rows[i].Id != _netRows[i].Id)
                return false;
        }

        return true;
    }

    private bool NetworkAppended(IReadOnlyList<AutomationStudioNetworkRow> rows)
    {
        if (rows.Count <= _netRows.Count)
            return false;

        for (var i = 0; i < _netRows.Count; i++) {
            if (rows[i].Id != _netRows[i].Id)
                return false;
        }

        return true;
    }

    private void ShowRecordFields()
    {
        var kind = _recordKind.SelectedIndex;
        _recordUrl.IsVisible = kind is 0 or 13;
        _recordLocatorRow.IsVisible = kind is 3 or 4 or 5 or 6;
        _recordText.IsVisible = kind is 5 or 7;
        _recordVariable.IsVisible = kind is 7 or 8 or 9 or 10 or 11 or 16 or 17 or 18;
        _recordDelay.IsVisible = kind is 2 or 10;
        _recordViewportRow.IsVisible = kind == 12;
        _recordTab.IsVisible = kind == 14;
        _recordPattern.IsVisible = kind is 10 or 11;
    }

    private void AddRecordStep()
    {
        if (_selected == null)
            return;

        switch (_recordKind.SelectedIndex) {
            case 0:
                var url = StudioAddress.Normalize(_recordUrl.Text);
                if (url == null) {
                    _log.Write("Enter a URL.");
                    return;
                }

                _recordUrl.Text = url;
                AddInsert(new("navigate", Value: url));
                break;
            case 1:
                AddInsert(new("reload"));
                break;
            case 2:
                var delay = int.TryParse(_recordDelay.Text, out var parsed) && parsed > 0 ? parsed : 250;
                AddInsert(new("delay", DelayMilliseconds: delay));
                break;
            case 3:
                AddRecordLocator("findElement", null);
                break;
            case 4:
                AddRecordLocator("findAndAct", "click");
                break;
            case 5:
                AddRecordLocator("findAndAct", "input");
                break;
            case 6:
                AddRecordLocator("findAndAct", "clear");
                break;
            case 7:
                AddInsert(new("storeLiteral", VariableOr("value"), _recordText.Text));
                break;
            case 8:
                AddInsert(new("storePageUrl", VariableOr("url")));
                break;
            case 9:
                AddInsert(new("storePageTitle", VariableOr("title")));
                break;
            case 10:
                var listen = int.TryParse(_recordDelay.Text, out var duration) && duration > 0 ? duration : 3000;
                AddInsert(new("captureNetwork", VariableOr("urls"), _recordPattern.Text, listen));
                break;
            case 11:
                StoreCaptured();
                break;
            case 12:
                if (!int.TryParse(_recordWidth.Text, out var width) || !int.TryParse(_recordHeight.Text, out var height) || width <= 0 || height <= 0) {
                    _log.Write("Enter a viewport width and height.");
                    return;
                }

                AddInsert(new("setViewport", Value: $"{width}x{height}"));
                break;
            case 13:
                AddInsert(new("openTab", Value: StudioAddress.Normalize(_recordUrl.Text)));
                break;
            case 14:
                if (!int.TryParse(_recordTab.Text, out var tab) || tab < 0) {
                    _log.Write("Enter a tab index.");
                    return;
                }

                AddInsert(new("switchTab", Value: tab.ToString()));
                break;
            case 15:
                AddInsert(new("closeTab"));
                break;
            case 16:
                _ = InsertGrabAsync("text");
                break;
            case 17:
                _ = InsertGrabAsync("images");
                break;
            case 18:
                _ = InsertGrabAsync("links");
                break;
        }
    }

    private void AddRecordLocator(string step, string? action)
    {
        var value = _recordLocator.Text?.Trim();
        if (string.IsNullOrWhiteSpace(value)) {
            _log.Write("Enter a locator.");
            return;
        }

        var kind = RecordLocatorKind();
        if (action == null)
            AddInsert(new(step, Value: value, LocatorKind: kind));
        else
            AddInsert(new(step, Value: value, LocatorKind: kind, Action: action, Text: _recordText.Text));
    }

    private void AddInsert(RecordedInsert insert)
    {
        if (_selected == null)
            return;

        _selected.Studio.InsertStep(insert);
        RefreshSteps();
    }

    private async Task ToggleRecordAsync()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen || session.Playing)
            return;

        try {
            if (session.Studio.IsRecording)
                session.Studio.StopRecording();
            else
                await session.Studio.StartRecordingAsync();
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }

        ApplyCommands();
    }

    private void OnStepPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_steps).Properties.IsLeftButtonPressed)
            return;

        _dragFrom = StepIndexAt(e.Source);
    }

    private void OnStepMoved(object? sender, PointerEventArgs e)
    {
        if (_dragFrom < 0 || !e.GetCurrentPoint(_steps).Properties.IsLeftButtonPressed)
            return;

        var index = StepIndexAt(e.Source);
        if (index >= 0)
            _steps.SelectedIndex = index;
    }

    private void OnStepReleased(object? sender, PointerReleasedEventArgs e)
    {
        var from = _dragFrom;
        _dragFrom = -1;
        if (from < 0 || _selected == null)
            return;

        var to = StepIndexAt(e.Source);
        if (to < 0 || to == from)
            return;

        _selected.Studio.MoveStep(from, to);
        RefreshSteps();
        _steps.SelectedIndex = to;
    }

    private int StepIndexAt(object? source)
    {
        var current = source as Visual;
        while (current != null && current is not ListBoxItem)
            current = current.GetVisualParent();

        return current is ListBoxItem item ? _steps.IndexFromContainer(item) : -1;
    }

    private void Delete()
    {
        if (_selected == null || _steps.SelectedIndex < 0)
            return;

        _selected.Studio.RemoveStep(_steps.SelectedIndex);
        RefreshSteps();
    }

    private void Save()
    {
        if (_selected == null)
            return;

        try {
            var path = _selected.Studio.SavePlan(_launch.OutputDirectory);
            _log.Write("Saved " + path);
        }
        catch (Exception ex) {
            _log.Write(ex.Message);
        }
    }

    private void RefreshLive()
    {
        var session = _selected;
        if (session == null || !session.BrowserOpen)
            return;

        var url = PageUrl(session);
        if (url == null)
            return;

        _url.Text = url;
        var index = _sessions.SelectedIndex;
        if (index >= 0)
            _sessions.Rename(index, SessionTitle(session, url));

        if (_view != 2)
            return;

        if (_toolView == ToolNetwork)
            RefreshNetwork();

        if (_toolView == ToolNavigate && Interlocked.Exchange(ref _titling, 1) == 0)
            _ = RefreshTitleAsync(session);

        if (_toolView == ToolElement && session.Grabbing && Interlocked.Exchange(ref _describing, 1) == 0)
            _ = DescribeAsync(session);
    }

    private async Task RefreshTitleAsync(BrowserSession session)
    {
        try {
            var title = await session.Studio.Browser.GetTitleAsync();
            await Dispatcher.UIThread.InvokeAsync(() => {
                if (session == _selected)
                    _pageTitle.Text = string.IsNullOrWhiteSpace(title) ? "Untitled page" : title;
            });
        }
        catch (Exception) {
            // The page can close between ticks.
        }
        finally {
            Interlocked.Exchange(ref _titling, 0);
        }
    }

    private async Task DescribeAsync(BrowserSession session)
    {
        try {
            var info = await session.Studio.DescribeElementAsync();
            if (session != _selected)
                return;

            if (info == null) {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    if (session != _selected)
                        return;

                    _found = null;
                    _targetKey = null;
                    ShowTarget(null, "Cursor");
                });

                return;
            }

            var locator = LocatorOf(info);
            var key = locator == null ? null : locator.Kind + " " + locator.Value;
            IWebAutomationElement? element = null;
            if (locator != null && key != _targetKey) {
                try {
                    element = await session.Studio.Browser.CurrentPage.GetElementAsync(locator);
                    await session.Studio.HighlightAsync(locator);
                }
                catch (Exception) {
                    element = null;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => {
                if (session != _selected)
                    return;

                if (element != null) {
                    _found = element;
                    _targetKey = key;
                }

                ShowTarget(info, "Cursor");
            });
        }
        catch (Exception) {
            // The page can navigate while the grabber is reading it.
        }
        finally {
            Interlocked.Exchange(ref _describing, 0);
        }
    }

    private void RefreshSteps()
    {
        FillSteps(_steps, false);
        FillSteps(_runSteps, true);
        ShowExecuted();
        ApplyCommands();
    }

    private void FillSteps(ListBox list, bool withOutcome)
    {
        var plan = _selected?.Studio.Plan;
        var selected = list.SelectedIndex;
        list.Items.Clear();
        if (plan == null)
            return;

        for (var i = 0; i < plan.Steps.Count; i++) {
            var label = $"{i + 1}. {LabelOf(plan, i)}";
            if (withOutcome)
                label += RunMark(i);

            list.Items.Add(label);
        }

        if (selected >= 0 && selected < list.ItemCount)
            list.SelectedIndex = selected;
    }

    private string RunMark(int index)
    {
        var done = _selected?.Done.LastOrDefault(step => step.Index == index);
        if (done?.Outcome?.Outcome is { } outcome)
            return "  " + outcome;

        return done == null ? "" : "  running";
    }

    private void ShowExecuted()
    {
        var done = _selected?.Done;
        var index = _runSteps.SelectedIndex;
        var step = done?.LastOrDefault(item => item.Index == index);
        var outcome = step?.Outcome;
        if (outcome == null) {
            _outcomeHint.IsVisible = true;
            _outcomeHint.Text = index < 0 ? "Select a step to see its last run." : "This step has not been run.";
            _outcomeBody.IsVisible = false;
            ShowShot(null);
            return;
        }

        _outcomeHint.IsVisible = false;
        _outcomeBody.IsVisible = true;
        _error.Text = outcome.Error ?? "";
        _stepId.Value = outcome.StepExecutionId.ToString();
        _stepLog.Text = string.Join(Environment.NewLine, step!.Log);
        _context.Rows = ContextRows(outcome.Context);
        ShowShot(outcome.ScreenshotPath);
    }

    private void ShowShot(string? path)
    {
        _bitmap?.Dispose();
        _bitmap = null;
        if (path != null && File.Exists(path)) {
            _bitmap = new(path);
            _shot.Source = _bitmap;
        }
        else
            _shot.Source = null;

        _shotHint.IsVisible = _bitmap == null;
    }

    private static IReadOnlyList<NameValueRow> ContextRows(AutomationPlanContextSnapshot? context)
    {
        if (context == null)
            return [];

        var rows = new List<NameValueRow>();
        foreach (var pair in context.Strings)
            rows.Add(new() { Name = pair.Key, Value = pair.Value });

        foreach (var pair in context.StringLists)
            rows.Add(new() { Name = pair.Key, Value = string.Join(", ", pair.Value) });

        foreach (var pair in context.ContextItems)
            rows.Add(new() { Name = pair.Key, Value = pair.Value });

        return rows;
    }

    private void SetStatus(BrowserSession session, string status, string? step)
    {
        session.Status = status;
        session.StatusStep = step;
        if (session == _selected)
            PaintStatus(status, step);
    }

    private void PaintStatus(string status, string? step)
    {
        _status.Text = string.IsNullOrWhiteSpace(step) ? status : $"{status} {step}";
        _status.Hue = status switch {
            "open" => ChipHues.Teal,
            "running" => ChipHues.Blue,
            "paused" => ChipHues.Amber,
            "completed" => ChipHues.Green,
            "failed" => ChipHues.Rose,
            "cancelled" => ChipHues.Coral,
            var _ => -1
        };
    }

    private int SelectedStep() => Math.Max(0, _runSteps.SelectedIndex);

    private string VariableOr(string fallback) => string.IsNullOrWhiteSpace(_recordVariable.Text) ? fallback : _recordVariable.Text.Trim();

    private ElementLocatorKind RecordLocatorKind()
    {
        var index = _recordLocatorKind.SelectedIndex;
        if (index < 0 || index >= LocatorKinds.Length)
            return ElementLocatorKind.CssSelector;

        return LocatorKinds[index].Kind;
    }

    private ElementLocatorKind SelectedKind()
    {
        var index = _locatorKind.SelectedIndex;
        if (index < 0 || index >= LocatorKinds.Length)
            return ElementLocatorKind.CssSelector;

        return LocatorKinds[index].Kind;
    }

    private bool TryViewport(out int width, out int height)
    {
        var widthOk = int.TryParse(_viewportWidth.Text, out width);
        var heightOk = int.TryParse(_viewportHeight.Text, out height);
        return widthOk && heightOk && width > 0 && height > 0;
    }

    private static string? CurrentLabel(BrowserSession session) => session.Done.LastOrDefault()?.Label;

    private static string LabelOf(AutomationPlan plan, int index)
    {
        if (index < 0 || index >= plan.Steps.Count)
            return "";

        return plan.Steps[index] switch {
            NavigateAutomationStep navigate => "Navigate " + Trim(navigate.Url, 80),
            ReloadAutomationStep => "Reload",
            DelayAutomationStep delay => $"Delay {delay.DelayMilliseconds} ms",
            FindElementAutomationStep find => "Find " + find.Locator,
            FindAndActAutomationStep act => ActionLabel(act.Action) + " " + act.Locator,
            CaptureNetworkAutomationStep capture => "Capture network " + capture.VariableName,
            StoreLiteralAutomationStep literal => "Store " + literal.VariableName,
            StorePageUrlAutomationStep url => "Store URL " + url.VariableName,
            StorePageTitleAutomationStep title => "Store title " + title.VariableName,
            StoreStringListAutomationStep list => "Store list " + list.VariableName,
            SetViewportSizeAutomationStep viewport => $"Viewport {viewport.Width}×{viewport.Height}",
            OpenNewTabAutomationStep tab => string.IsNullOrWhiteSpace(tab.Url) ? "Open tab" : "Open tab " + Trim(tab.Url, 60),
            CloseCurrentTabAutomationStep => "Close tab",
            SwitchToTabByIndexAutomationStep switchTab => "Switch to tab " + switchTab.TabIndex,
            ExtractElementDataAutomationStep extract => "Extract " + extract.VariableName,
            var _ => Fallback(plan.Steps[index])
        };
    }

    private static string ActionLabel(ElementAction action)
        => action switch {
            ClickElementAction => "Click",
            InputTextElementAction input => "Type " + Trim(input.Text, 40),
            ClearElementAction => "Clear",
            var _ => "Act"
        };

    private static string Fallback(AutomationStepDefinition step)
    {
        var type = step.GetType().Name;
        const string suffix = "AutomationStep";
        if (type.EndsWith(suffix, StringComparison.Ordinal))
            type = type[..^suffix.Length];

        return string.IsNullOrWhiteSpace(step.Name) ? type : step.Name!;
    }

    private static string? PageUrl(BrowserSession? session)
    {
        var page = session?.Studio.Browser.Page;
        if (page == null)
            return null;

        try {
            return page.IsClosed ? null : page.Url;
        }
        catch (Exception) {
            return null;
        }
    }

    private static string SessionTitle(BrowserSession session, string? url)
    {
        if (!session.BrowserOpen)
            return $"Session {session.Number} · closed";

        var host = "browser";
        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var parsed) && !string.IsNullOrWhiteSpace(parsed.Host))
            host = parsed.Host;

        return $"Session {session.Number} · {host}";
    }

    private static string? BlankToNull(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string Trim(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var flat = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return flat.Length <= max ? flat : flat[..max] + "…";
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _hotkeys?.Dispose();
        _hotkeys = null;
        if (_closing || _open.Count == 0)
            return;

        e.Cancel = true;
        _closing = true;
        _tick.Stop();
        foreach (var session in _open.ToArray()) {
            try {
                await session.Studio.StopAsync();
            }
            catch (Exception ex) {
                _log.Write(ex.Message);
            }
        }

        _open.Clear();
        Close();
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> click)
    {
        var button = new Button { Content = text };
        button.Click += click;
        return button;
    }

    private static StackPanel Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var child in children)
            row.Children.Add(child);

        return row;
    }

    private static TextBlock Heading(string text) => new() { Text = text, FontWeight = FontWeight.SemiBold, Margin = new(0, 12, 0, 0) };

    private sealed class BrowserSession
    {
        public int Number { get; init; }

        public required IPlaywrightAutomationStudio Studio { get; init; }

        public List<ExecutedStep> Done { get; } = [];

        public string Status { get; set; } = "open";

        public string? StatusStep { get; set; }

        public bool BrowserOpen { get; set; } = true;

        public bool Playing { get; set; }

        public bool Paused { get; set; }

        public bool Listening { get; set; }

        public bool Grabbing { get; set; }
    }

    private sealed class ExecutedStep
    {
        public int Index { get; init; }

        public string Label { get; init; } = "";

        public AutomationStudioStepNotice? Outcome { get; set; }

        public List<string> Log { get; } = [];
    }
}