using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Lyo.Desktop.Components;
using Lyo.Desktop;
using Lyo.Screenshot;
using DesktopWindows = Lyo.Screenshot.DesktopWindows;

namespace Lyo.Screenshot.App;

public partial class MainWindow : Window
{
    private readonly ShotSettings _settings;
    private IScreenGrabber? _grabber;
    private IGlobalHotkeys? _hotkeys;
    private CancellationTokenSource? _capture;
    private CaptureRegion _region;
    private bool _fullScreen;
    private long _windowId;
    private string _windowTitle = "";
    private bool _recording;
    private bool _closing;
    private bool _hotkeysReady;
    private static readonly TimeSpan CaptureHideSettle = TimeSpan.FromMilliseconds(200);
    private Button? _listening;
    private string? _chordBeforeListen;
    private byte[]? _lastBytes;
    private byte[]? _previewPng;
    private ShotKind _lastKind;
    private string? _lastPath;
    private string? _tempShot;
    private IStorageFile? _shotFile;
    private bool _settingsReady;
    private Bitmap? _preview;
    private readonly EdgeDockWindow _edge;

    public MainWindow()
    {
        InitializeComponent();
        _edge = new EdgeDockWindow(this, DockBar, MainPanel, new EdgeDockRest(360, 0, true), canResizeWhenOpen: false, minWidthWhenOpen: 360);
        ConfigureDock();
        _settings = ShotSettingsStore.Load();
        _edge.Placement.Apply(_settings.Docked, (int)_settings.DockEdge, _settings.DockX, _settings.DockY, _settings.DockRestX, _settings.DockRestY);
        _edge.DockedChanged += (_, _) => {
            if (!_edge.Docked)
                PinOnTop();
            if (_settingsReady)
                SaveSettings();
        };
        ApplySettings(_settings);
        StillMode.IsCheckedChanged += (_, _) => RefreshIdleHint();
        GifMode.IsCheckedChanged += (_, _) => RefreshIdleHint();
        ReceiptMode.IsCheckedChanged += (_, _) => RefreshIdleHint();
        Closing += (_, _) => {
            _closing = true;
            _edge.Stop();
            SaveSettings();
            _capture?.Cancel();
            _hotkeys?.Dispose();
            _grabber?.Dispose();
            Preview.Source = null;
            _preview?.Dispose();
            _tempShot = null;
            if (!ShotScratch.Wipe(ShotScratch.Directory))
                ShotLog.Info("Temp: could not delete " + ShotScratch.Directory);
        };
        _settingsReady = true;
        Opened += (_, _) => {
            if (_edge.Restoring)
                return;
            if (_settings.DockRestX == 0 && _settings.DockRestY == 0) {
                _edge.Placement.RestX = Position.X;
                _edge.Placement.RestY = Position.Y;
            }

            PinOnTop();
            StartHotkeys();
            ApplyWindowIcon();
            if (_settings.Docked)
                _edge.Show(saved: true);
            Opacity = 1;
        };
    }

    private void ApplySettings(ShotSettings settings)
    {
        StillMode.IsChecked = settings.Mode == ShotKind.Still;
        GifMode.IsChecked = settings.Mode == ShotKind.Gif;
        ReceiptMode.IsChecked = settings.Mode == ShotKind.Receipt;
        SelectFps(settings.FramesPerSecond);
        SelectDelay(settings.DelaySeconds);
        SaveDirectoryBox.Text = settings.SaveDirectory;
        SaveBox.IsChecked = settings.SaveToFile != false;
        CopyBox.IsChecked = settings.CopyToClipboard;
        OpenBox.IsChecked = settings.OpenAfterSave;
        CursorBox.IsChecked = settings.IncludeCursor;
        TopmostBox.IsChecked = settings.AlwaysOnTop;
        Topmost = settings.AlwaysOnTop;
        StartChordButton.Content = settings.StartHotkey;
        StopChordButton.Content = settings.StopHotkey;
        _fullScreen = settings.FullScreen;
        _windowId = settings.FullScreen ? 0 : settings.WindowId;
        _windowTitle = settings.WindowTitle ?? "";
        _region = new(settings.RegionX, settings.RegionY, settings.RegionWidth, settings.RegionHeight);
        if (_fullScreen)
            RegionText.Text = "Full screen";
        else if (_windowId != 0)
            RegionText.Text = string.IsNullOrWhiteSpace(_windowTitle) ? "Window" : _windowTitle;
        else if (_region.Width > 0 && _region.Height > 0)
            RegionText.Text = $"{_region.Width} × {_region.Height} at {_region.X}, {_region.Y}";
        else
            RegionText.Text = "No region selected";
        StartButton.IsEnabled = HasRegion();
        RefreshIdleHint();
        if (settings.Docked)
            Opacity = 0;
    }

    private void StartHotkeys()
    {
        if (_hotkeys != null)
            return;
        try {
            _hotkeys = GlobalHotkeyHost.Create();
            _hotkeys.StartPressed += OnStartPressed;
            _hotkeys.StopPressed += OnStopPressed;
            RegisterHotkeys(StartChordButton.Content?.ToString(), StopChordButton.Content?.ToString());
            _hotkeysReady = true;
        }
        catch (Exception ex) {
            _hotkeysReady = false;
            ShotLog.Info("Hotkeys: " + ex.Message);
            StatusText.Text = ex.Message;
        }
    }

    private async void OnSelectRegion(object? sender, RoutedEventArgs e) => await SelectRegionAsync();

    private async Task<bool> SelectRegionAsync()
    {
        if (_recording)
            return false;
        Hide();
        var overlay = new RegionOverlay();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        overlay.Closed += (_, _) => closed.TrySetResult();
        overlay.Show();
        await closed.Task;
        BringBack();
        if (overlay.Result is not { } region)
            return false;
        _fullScreen = false;
        _windowId = 0;
        _windowTitle = "";
        _region = region;
        RegionText.Text = $"{region.Width} × {region.Height} at {region.X}, {region.Y}";
        StartButton.IsEnabled = true;
        SaveSettings();
        return true;
    }

    private void OnFullScreen(object? sender, RoutedEventArgs e)
    {
        if (_recording)
            return;
        _fullScreen = true;
        _windowId = 0;
        _windowTitle = "";
        RegionText.Text = "Full screen";
        StartButton.IsEnabled = true;
        SaveSettings();
    }

    private void OnSelectWindow(object? sender, RoutedEventArgs e) => ShowWindowMenu(WindowButton, capture: false);

    private void ShowWindowMenu(Control? anchor, bool capture)
    {
        if (anchor == null || _recording)
            return;
        var windows = DesktopWindows.List();
        if (windows.Count == 0) {
            StatusText.Text = "No windows found.";
            return;
        }

        var flyout = new MenuFlyout();
        foreach (var window in windows.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)) {
            var chosen = window;
            var item = new MenuItem {
                Header = new TextBlock { Text = chosen.Title, MaxWidth = 320, TextTrimming = TextTrimming.CharacterEllipsis }
            };
            item.Click += (_, _) => {
                ChooseWindow(chosen);
                if (capture)
                    _ = CaptureFromDock();
            };
            flyout.Items.Add(item);
        }

        DockBar.HoldOpen = true;
        flyout.Closed += (_, _) => {
            DockBar.HoldOpen = false;
            DockBar.CollapseIfIdle();
        };
        flyout.ShowAt(anchor);
    }

    private void ChooseWindow(DesktopWindow window)
    {
        _fullScreen = false;
        _windowId = window.Id;
        _windowTitle = window.Title;
        _region = DesktopWindows.Region(window);
        RegionText.Text = window.Title;
        StartButton.IsEnabled = true;
        SaveSettings();
    }

    private void OnToggleMore(object? sender, RoutedEventArgs e)
    {
        MorePanel.IsVisible = !MorePanel.IsVisible;
        MoreChevron.Text = MorePanel.IsVisible ? "▴" : "▾";
    }

    private void OnTopmostChanged(object? sender, RoutedEventArgs e)
    {
        if (_edge.Docked) {
            Topmost = true;
            return;
        }

        Topmost = TopmostBox.IsChecked == true;
        if (_settingsReady)
            SaveSettings();
    }

    private void OnStartPressed() => _ = Dispatcher.InvokeAsync(StartAsync);

    private void OnStopPressed() => Dispatcher.Post(StopCapture, DispatcherPriority.Normal);

    private async void OnStart(object? sender, RoutedEventArgs e) => await StartAsync();

    private void OnStop(object? sender, RoutedEventArgs e) => StopCapture();

    private async Task StartAsync()
    {
        if (_recording || !HasRegion())
            return;
        if (!SelectedWindowOpen())
            return;
        var kind = CurrentKind();
        var fps = ReadFps();
        var delay = ReadDelay();
        var cursor = CursorBox.IsChecked == true;
        var fullScreen = _fullScreen;
        var windowId = _windowId;
        var selected = _region;
        _recording = true;
        StartButton.IsVisible = false;
        StopButton.IsVisible = kind != ShotKind.Still || delay > 0;
        _capture = new CancellationTokenSource();
        try {
            _hotkeys?.SetEscapeStops(kind != ShotKind.Still);
        }
        catch (Exception ex) {
            ShotLog.Info("Hotkeys: " + ex.Message);
            StatusText.Text = ex.Message;
        }

        var token = _capture.Token;
        var hidden = false;
        try {
            if (delay > 0)
                await WaitDelayAsync(delay, token);
            if (kind == ShotKind.Still)
                StopButton.IsVisible = false;
            else
                StatusText.Text = "Scroll the page. Stop when you are done.";
            // A GIF or receipt has no Stop button while hidden. Leave the window up when the stop shortcut is not grabbed.
            if (kind == ShotKind.Still || _hotkeysReady) {
                Hide();
                hidden = true;
                await Task.Delay(CaptureHideSettle, token);
            }
            ShotLog.Info($"Capture started ({kind}).");
            var shot = await Task.Run(async () => {
                var grabber = Grabber();
                var region = ResolveRegion(grabber, fullScreen, windowId, selected);
                if (region.Width < 1 || region.Height < 1)
                    throw new InvalidOperationException("The capture region is empty.");
                var session = new ShotSession(new GrabFrameSource(grabber));
                return await session.CaptureAsync(kind, region, fps, cursor, token);
            });
            await FinishAsync(kind, shot);
        }
        catch (OperationCanceledException) {
            ShotLog.Info("Capture cancelled before a frame was saved.");
            StatusText.Text = "Capture cancelled.";
        }
        catch (Exception ex) {
            ShotLog.Info(ex.ToString());
            StatusText.Text = ex.Message;
            if (_windowId != 0 && DesktopWindowFilter.Missing(_windowId, DesktopWindows.List()))
                ClearWindowSelection();
        }
        finally {
            if (hidden)
                BringBack();
            ShotEncoder.ReleaseScratchMemory();
            try {
                _hotkeys?.SetEscapeStops(false);
            }
            catch (Exception ex) {
                ShotLog.Info("Hotkeys: " + ex.Message);
            }

            _capture.Dispose();
            _capture = null;
            _recording = false;
            StopButton.IsVisible = false;
            StartButton.IsVisible = true;
            StartButton.IsEnabled = HasRegion();
        }
    }

    private async Task WaitDelayAsync(int seconds, CancellationToken token)
    {
        for (var left = seconds; left > 0; left--) {
            StatusText.Text = left == 1 ? "Capturing in 1 second." : $"Capturing in {left} seconds.";
            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }
    }

    private static CaptureRegion ResolveRegion(IScreenGrabber grabber, bool fullScreen, long windowId, CaptureRegion selected)
    {
        if (fullScreen)
            return grabber.VirtualScreen;
        if (windowId == 0)
            return selected;
        foreach (var window in DesktopWindows.List()) {
            if (window.Id == windowId)
                return DesktopWindows.Region(window);
        }

        throw new InvalidOperationException("That window is no longer open.");
    }

    private void StopCapture()
    {
        if (_capture is null)
            return;
        ShotLog.Info("Stop.");
        StatusText.Text = CurrentKind() == ShotKind.Receipt ? "Stitching…" : "Finishing…";
        BringBack();
        _capture.Cancel();
    }

    private async Task FinishAsync(ShotKind kind, CapturedShot shot)
    {
        var bytes = shot.Bytes;
        ShowResult(kind, shot);
        var saving = SaveBox.IsChecked == true;
        var directory = saving
            ? (string.IsNullOrWhiteSpace(SaveDirectoryBox.Text) ? ShotSettingsStore.DefaultSaveDirectory() : SaveDirectoryBox.Text)
            : ShotScratch.Directory;
        Directory.CreateDirectory(directory);
        var ext = kind == ShotKind.Gif ? "gif" : "png";
        var path = Path.Combine(directory, $"lyo-shot-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}");
        if (File.Exists(path))
            path = Path.Combine(directory, $"lyo-shot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.{ext}");
        if (!saving)
            ForgetTempShot();
        // Stop cancels the capture token. The write must not observe that token, or the finished shot is dropped.
        await File.WriteAllBytesAsync(path, bytes);
        if (!saving)
            _tempShot = path;
        ShotLog.Info($"Saved {path} ({bytes.Length} bytes).");
        _lastPath = path;
        _shotFile = await StorageProvider.TryGetFileFromPathAsync(path);
        OpenSavedButton.IsVisible = true;
        OpenFolderButton.IsVisible = true;
        StatusText.Text = saving ? $"Saved to {path}" : $"Kept at {path}";

        var copied = false;
        if (CopyBox.IsChecked == true)
            copied = await CopyToClipboardAsync(kind, bytes, path, CancellationToken.None);
        if (!saving && copied)
            StatusText.Text = $"Copied. Kept at {path}";
        if (saving && OpenBox.IsChecked == true) {
            try {
                OpenFile(path);
            }
            catch (Exception ex) {
                ShotLog.Info("Open: " + ex.Message);
                StatusText.Text = $"{StatusText.Text} Open: {ex.Message}";
            }
        }

        SaveSettings();
    }

    private async Task<bool> CopyToClipboardAsync(ShotKind kind, byte[] bytes, string? path, CancellationToken ct)
    {
        try {
            var png = _previewPng is not null && ReferenceEquals(bytes, _lastBytes)
                ? _previewPng
                : kind == ShotKind.Gif ? ShotEncoder.FirstFramePng(bytes) : bytes;
            await ShotClipboard.CopyShotAsync(Clipboard, StorageProvider, path ?? _lastPath, png, ct);
            return true;
        }
        catch (Exception ex) {
            StatusText.Text = $"{StatusText.Text} Clipboard: {ex.Message}";
            return false;
        }
    }

    private void ShowResult(ShotKind kind, CapturedShot shot)
    {
        _lastKind = kind;
        _lastBytes = shot.Bytes;
        _lastPath = null;
        _shotFile = null;
        OpenSavedButton.IsVisible = false;
        OpenFolderButton.IsVisible = false;
        var png = shot.PreviewPng;
        _previewPng = png;
        Preview.Source = null;
        _preview?.Dispose();
        using var stream = new MemoryStream(png);
        _preview = new Bitmap(stream);
        Preview.Source = _preview;
        PreviewHost.IsVisible = true;
        ResultActions.IsVisible = true;
    }

    private void ForgetTempShot()
    {
        if (_tempShot is null)
            return;
        try {
            if (File.Exists(_tempShot))
                File.Delete(_tempShot);
        }
        catch (Exception ex) {
            ShotLog.Info("Temp: " + ex.Message);
        }

        _tempShot = null;
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_shotFile is null || !e.GetCurrentPoint(PreviewHost).Properties.IsLeftButtonPressed)
            return;
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateFile(_shotFile));
        e.Handled = true;
        _ = DragPreviewAsync(e, transfer);
    }

    private async Task DragPreviewAsync(PointerPressedEventArgs e, DataTransfer transfer)
    {
        try {
            await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Copy);
        }
        catch (Exception ex) {
            ShotLog.Info("Drag: " + ex.Message);
            StatusText.Text = ex.Message;
        }
    }

    private async void OnCopyLast(object? sender, RoutedEventArgs e)
    {
        if (_lastBytes is null)
            return;
        if (await CopyToClipboardAsync(_lastKind, _lastBytes, _lastPath, CancellationToken.None))
            StatusText.Text = "Copied the file.";
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_lastBytes is null)
            return;
        try {
            var directory = string.IsNullOrWhiteSpace(SaveDirectoryBox.Text) ? ShotSettingsStore.DefaultSaveDirectory() : SaveDirectoryBox.Text;
            Directory.CreateDirectory(directory);
            var savedDirectory = _lastPath is null ? null : Path.GetDirectoryName(_lastPath);
            var alreadySaved = savedDirectory != null && string.Equals(Path.GetFullPath(savedDirectory), Path.GetFullPath(directory), StringComparison.Ordinal);
            if (alreadySaved) {
                StatusText.Text = $"Saved to {_lastPath}";
                return;
            }

            var ext = _lastKind == ShotKind.Gif ? "gif" : "png";
            var path = Path.Combine(directory, $"lyo-shot-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}");
            if (File.Exists(path))
                path = Path.Combine(directory, $"lyo-shot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.{ext}");
            await File.WriteAllBytesAsync(path, _lastBytes);
            ForgetTempShot();
            _lastPath = path;
            _shotFile = await StorageProvider.TryGetFileFromPathAsync(path);
            OpenSavedButton.IsVisible = true;
            OpenFolderButton.IsVisible = true;
            StatusText.Text = $"Saved to {path}";
            ShotLog.Info($"Saved {path} ({_lastBytes.Length} bytes).");
        }
        catch (Exception ex) {
            ShotLog.Info("Save: " + ex.Message);
            StatusText.Text = ex.Message;
        }
    }

    private void OnOpenSaved(object? sender, RoutedEventArgs e)
    {
        if (_lastPath is null)
            return;
        try {
            OpenFile(_lastPath);
        }
        catch (Exception ex) {
            ShotLog.Info("Open: " + ex.Message);
            StatusText.Text = ex.Message;
        }
    }

    private void OnOpenFolder(object? sender, RoutedEventArgs e)
    {
        if (_lastPath is null)
            return;
        var directory = Path.GetDirectoryName(_lastPath);
        if (string.IsNullOrEmpty(directory))
            return;
        try {
            OpenFile(directory);
        }
        catch (Exception ex) {
            ShotLog.Info("Open folder: " + ex.Message);
            StatusText.Text = ex.Message;
        }
    }

    private static void OpenFile(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, Title = "Save screenshots to" });
        if (folders.Count == 0)
            return;
        var path = folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;
        SaveDirectoryBox.Text = path;
        SaveSettings();
    }

    private void OnChordClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        _listening = button;
        _chordBeforeListen = button.Content?.ToString();
        button.Content = "Press a shortcut";
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_listening is null) {
            if (_recording && IsStopChord(e)) {
                StopCapture();
                e.Handled = true;
            }

            return;
        }

        e.Handled = true;
        if (e.Key == Key.Escape) {
            _listening.Content = _chordBeforeListen;
            _listening = null;
            return;
        }

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            return;
        var formatted = FormatChord(e.KeyModifiers, e.Key);
        var button = _listening;
        _listening = null;
        try {
            var parsed = HotkeyChord.Parse(formatted);
            button.Content = parsed.ToString();
            RegisterHotkeys(StartChordButton.Content?.ToString(), StopChordButton.Content?.ToString());
            SaveSettings();
        }
        catch (Exception ex) {
            ShotLog.Info("Hotkeys: " + ex.Message);
            button.Content = _chordBeforeListen;
            StatusText.Text = ex.Message;
        }
    }

    private bool IsStopChord(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            return true;
        try {
            var chord = HotkeyChord.Parse(StopChordButton.Content?.ToString() ?? "");
            return chord.Modifiers == ToHotkeyModifiers(e.KeyModifiers) && chord.Key.Equals(KeyName(e.Key), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) {
            return false;
        }
    }

    private static HotkeyModifiers ToHotkeyModifiers(KeyModifiers modifiers)
    {
        var mods = HotkeyModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control))
            mods |= HotkeyModifiers.Ctrl;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            mods |= HotkeyModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            mods |= HotkeyModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Meta))
            mods |= HotkeyModifiers.Meta;
        return mods;
    }

    private void RegisterHotkeys(string? startText, string? stopText)
    {
        if (_hotkeys is null)
            return;
        var start = HotkeyChord.Parse(string.IsNullOrWhiteSpace(startText) ? "Ctrl+Shift+S" : startText);
        var stop = HotkeyChord.Parse(string.IsNullOrWhiteSpace(stopText) ? "Ctrl+Shift+X" : stopText);
        if (start == stop)
            throw new InvalidOperationException("Start and stop need different shortcuts.");
        _hotkeys.Update(start, stop);
    }

    private void RefreshIdleHint()
    {
        if (_recording)
            return;
        StatusText.Text = CurrentKind() == ShotKind.Still
            ? "Press Start to grab one frame."
            : "Press Start, scroll, then Stop.";
    }

    private ShotKind CurrentKind()
    {
        if (StillMode.IsChecked == true)
            return ShotKind.Still;
        if (GifMode.IsChecked == true)
            return ShotKind.Gif;
        return ShotKind.Receipt;
    }

    private bool HasRegion() => _fullScreen || _windowId != 0 || (_region.Width > 0 && _region.Height > 0);

    private bool SelectedWindowOpen()
    {
        if (_windowId == 0 || !DesktopWindowFilter.Missing(_windowId, DesktopWindows.List()))
            return true;
        ClearWindowSelection();
        const string closed = "That window is no longer open.";
        StatusText.Text = closed;
        ShotLog.Info(closed);
        return false;
    }

    private void ClearWindowSelection()
    {
        _windowId = 0;
        _windowTitle = "";
        _fullScreen = false;
        _region = default;
        RegionText.Text = "No region selected";
        StartButton.IsEnabled = false;
        if (_settingsReady)
            SaveSettings();
    }

    private IScreenGrabber Grabber() => _grabber ??= ScreenGrabber.Create();

    private void BringBack()
    {
        if (_closing)
            return;
        if (!IsVisible) {
            PinOnTop();
            Show();
            Dispatcher.UIThread.Post(PinOnTop, DispatcherPriority.Background);
        }

        Activate();
        if (_edge.Docked)
            _edge.Refit();
    }

    // Hiding the window drops the above hint, and assigning Topmost again does nothing while it is already true.
    private void PinOnTop()
    {
        if (_closing)
            return;
        if (!_edge.Docked && TopmostBox.IsChecked != true)
            return;
        Topmost = false;
        Topmost = true;
    }

    private void OnDock(object? sender, RoutedEventArgs e)
    {
        if (_recording || _edge.Docked)
            return;
        _edge.Show(saved: false);
    }

    private void ConfigureDock()
    {
        DockBar.Icon = new CameraIcon { Width = 16, Height = 16, Foreground = Brushes.White };
        DockBar.ExpandOnHover = true;
        DockBar.Buttons.Add(new EdgeDockButton { Text = "Region", Pressed = (_, _) => _ = CaptureRegionFromDock() });
        DockBar.Buttons.Add(new EdgeDockButton { Text = "Window", Pressed = (sender, _) => ShowWindowMenu(sender as Control, capture: true) });
        DockBar.Buttons.Add(new EdgeDockButton { Text = "Full screen", Pressed = (_, _) => _ = CaptureFullScreenFromDock() });
    }

    private async Task CaptureRegionFromDock()
    {
        if (_recording)
            return;
        DockBar.Expanded = false;
        if (await SelectRegionAsync())
            await CaptureFromDock();
    }

    private async Task CaptureFullScreenFromDock()
    {
        if (_recording)
            return;
        OnFullScreen(this, new RoutedEventArgs());
        await CaptureFromDock();
    }

    private async Task CaptureFromDock()
    {
        if (_recording || !HasRegion())
            return;
        DockBar.HoldOpen = false;
        DockBar.Expanded = false;
        await StartAsync();
    }

    private void ApplyWindowIcon()
    {
        var geometry = Geometry.Parse(CameraMark.Data);
        var bitmap = new RenderTargetBitmap(new PixelSize(32, 32), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext()) {
            using (context.PushTransform(Matrix.CreateScale(32d / 24d, 32d / 24d)))
                context.DrawGeometry(Brushes.Black, null, geometry);
        }

        Icon = new WindowIcon(bitmap);
    }

    private void SaveSettings()
    {
        _settings.Mode = CurrentKind();
        _settings.FramesPerSecond = ReadFps();
        _settings.DelaySeconds = ReadDelay();
        _settings.SaveToFile = SaveBox.IsChecked == true;
        _settings.SaveDirectory = SaveDirectoryBox.Text ?? ShotSettingsStore.DefaultSaveDirectory();
        _settings.CopyToClipboard = CopyBox.IsChecked == true;
        _settings.OpenAfterSave = OpenBox.IsChecked == true;
        _settings.IncludeCursor = CursorBox.IsChecked == true;
        _settings.AlwaysOnTop = TopmostBox.IsChecked == true;
        _settings.StartHotkey = StartChordButton.Content?.ToString() ?? "Ctrl+Shift+S";
        _settings.StopHotkey = StopChordButton.Content?.ToString() ?? "Ctrl+Shift+X";
        _settings.FullScreen = _fullScreen;
        _settings.WindowId = _fullScreen ? 0 : _windowId;
        _settings.WindowTitle = _windowTitle;
        _settings.RegionX = _region.X;
        _settings.RegionY = _region.Y;
        _settings.RegionWidth = _region.Width;
        _settings.RegionHeight = _region.Height;
        var place = _edge.Placement;
        _settings.Docked = place.Docked;
        _settings.DockEdge = place.Edge;
        _settings.DockX = place.X;
        _settings.DockY = place.Y;
        _settings.DockRestX = place.RestX;
        _settings.DockRestY = place.RestY;
        ShotSettingsStore.Save(_settings);
    }

    private int ReadFps()
    {
        var text = (FpsBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return int.TryParse(text, out var fps) ? Math.Clamp(fps, 1, 30) : 5;
    }

    private void SelectFps(int fps)
    {
        var label = Math.Clamp(fps, 1, 30).ToString();
        foreach (var item in FpsBox.Items) {
            if (item is ComboBoxItem option && option.Content?.ToString() == label) {
                FpsBox.SelectedItem = option;
                return;
            }
        }

        FpsBox.SelectedIndex = 2;
    }

    private int ReadDelay()
    {
        var text = (DelayBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return int.TryParse(text, out var seconds) ? Math.Clamp(seconds, 0, 10) : 0;
    }

    private void SelectDelay(int seconds)
    {
        var label = Math.Clamp(seconds, 0, 10).ToString();
        foreach (var item in DelayBox.Items) {
            if (item is ComboBoxItem option && option.Content?.ToString() == label) {
                DelayBox.SelectedItem = option;
                return;
            }
        }

        DelayBox.SelectedIndex = 0;
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

    private static string KeyName(Key key) => key switch {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        Key.Escape => "Escape",
        Key.Space => "Space",
        Key.Tab => "Tab",
        _ => key.ToString()
    };
}
