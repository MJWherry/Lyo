using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Lyo.Desktop.Components;
using Lyo.FileStorage;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileManager.App;

public partial class MainWindow : Window
{
    private static readonly DataFormat<FileDragPayload> DragFormat = DataFormat.CreateInProcessFormat<FileDragPayload>("lyo.file-manager.entries");
    private readonly List<(StackPanel Body, TextBlock Mark)> _accordion = [];
    private readonly FileManagerDockBubble _bubble = new();
    private readonly ToolDockState _dock = new();
    private readonly EdgeDockWindow _edge;
    private readonly LogSink _logs = new();
    private readonly ObservableCollection<FolderNode> _roots = [];
    private readonly List<FileRow> _rows = [];
    private readonly FileManagerSession _session = new();
    private readonly List<OpenTab> _tabs = [];
    private readonly Dictionary<(Guid ServerId, Guid? KeyStoreId), TreeSnapshot> _trees = [];
    private bool _applyingExpand;
    private Guid? _boundServer;
    private bool _connected;
    private FolderNode? _current;
    private FileManagerTool? _drag;
    private bool _fillingList;
    private bool _fillingServers;
    private bool _fillingTree;
    private bool _fromTree;
    private bool _headerMoved;
    private Point _headerPress;
    private bool _itemDrag;
    private PointerPressedEventArgs? _itemEvent;
    private Control? _itemHost;
    private Point? _itemPress;
    private List<FileRow>? _keptFiles;
    private List<FolderNode>? _keptNodes;
    private FileManagerLayout _layout = FileManagerLayout.CreateDefault();
    private bool _pressKeepsSelection;
    private object? _pressedRow;
    private bool _query;
    private bool _syncingTabs;
    private bool _syncingView;
    private bool _treeFiles;
    private Guid? _treeKey;
    private Guid? _treeServer;

    public MainWindow()
    {
        InitializeComponent();
        _edge = new(this, DockBar, Shell, new(1100, 720, false));
        var document = FileManagerSettingsStore.LoadDocument(FileManagerSettingsStore.ConfigPath());
        if (document.Layout.Tools.Count == 3)
            _layout = document.Layout;

        _bubble = document.Bubble ?? new();
        ConfigureDock();
        _treeFiles = document.ShowTreeFiles;
        _logs.Written += line => FileManagerLog.Write(line.Message);
        AppLog.Sink = _logs;
        _session.Logs = new LogSinkLoggerFactory(new(_logs));
        _session.ReplaceServers(document.Servers);
        _session.ReplaceKeyStores(document.KeyStores ?? []);
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", true).Build();
        var configured = FileManagerConfiguredHost.Load(configuration, _session.Logs);
        foreach (var note in configured.Skipped)
            Log(note);

        _session.UseConfiguredStores(configured);
        BindServers();
        BindKeyStores(true);
        Folders.ItemsSource = _roots;
        DragDrop.SetAllowDrop(Files, true);
        DragDrop.SetAllowDrop(Folders, true);
        DragDrop.SetAllowDrop(TreePanel, true);
        DragDrop.SetAllowDrop(Connections, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        Files.AddHandler(PointerPressedEvent, OnItemPressed, RoutingStrategies.Tunnel);
        Files.AddHandler(PointerMovedEvent, OnItemMoved, RoutingStrategies.Tunnel);
        Files.AddHandler(PointerReleasedEvent, OnItemReleased, RoutingStrategies.Tunnel);
        Folders.AddHandler(PointerPressedEvent, OnItemPressed, RoutingStrategies.Tunnel);
        Folders.AddHandler(PointerMovedEvent, OnItemMoved, RoutingStrategies.Tunnel);
        Folders.AddHandler(PointerReleasedEvent, OnItemReleased, RoutingStrategies.Tunnel);
        Files.LoadingRow += (_, e) => DragDrop.SetAllowDrop(e.Row, true);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        Folders.ContainerPrepared += (_, e) => {
            if (e.Container is not TreeViewItem item)
                return;

            DragDrop.SetAllowDrop(item, true);
            item.PropertyChanged -= OnTreeItemProperty;
            item.PropertyChanged += OnTreeItemProperty;
            if (item.DataContext is FolderNode prepared) {
                var canonical = prepared.Source ?? prepared;
                if (item.IsExpanded == canonical.IsExpanded)
                    return;

                _applyingExpand = true;
                item.IsExpanded = canonical.IsExpanded;
                _applyingExpand = false;
            }
        };

        Connections.SelectionChanged += (_, _) => OnConnectionSelected();
        Connections.TabClosed += (_, index) => OnConnectionClosed(index);
        ApplyLayout();
        EmptyState.IsVisible = true;
        BreadcrumbText();
        ApplyCapabilities();
    }

    private void ApplyLayout()
    {
        CopyLayoutToDock();
        ToolDockBuilder.Fill(DockHost, _dock, Panes(), CenterFrame, OnDockChanged);
        SyncViewChecks();
    }

    private void OnDockChanged()
    {
        CopyDockToLayout();
        ApplyLayout();
        SaveSettings();
    }

    private Dictionary<string, Control> Panes()
        => new() { [nameof(FileManagerTool.Tree)] = TreePanel, [nameof(FileManagerTool.Metadata)] = MetadataPanel, [nameof(FileManagerTool.Log)] = AppLog };

    private void CopyLayoutToDock()
    {
        _dock.Left = _layout.Left;
        _dock.Right = _layout.Right;
        _dock.Bottom = _layout.Bottom;
        _dock.Panes.Clear();
        foreach (var tool in _layout.Tools) {
            _dock.Panes.Add(
                new() {
                    Id = tool.Tool.ToString(),
                    Title = tool.Tool.ToString(),
                    Edge = ToDockEdge(tool.Edge),
                    Visible = tool.Visible
                });
        }
    }

    private void CopyDockToLayout()
    {
        _layout.Left = _dock.Left;
        _layout.Right = _dock.Right;
        _layout.Bottom = _dock.Bottom;
        foreach (var pane in _dock.Panes) {
            var window = _layout.Window(Enum.Parse<FileManagerTool>(pane.Id));
            window.Edge = FromDockEdge(pane.Edge);
            window.Visible = pane.Visible;
        }
    }

    private static ToolDockEdge ToDockEdge(FileManagerDockEdge edge)
        => edge switch {
            FileManagerDockEdge.Left => ToolDockEdge.Left,
            FileManagerDockEdge.Right => ToolDockEdge.Right,
            var _ => ToolDockEdge.Bottom
        };

    private static FileManagerDockEdge FromDockEdge(ToolDockEdge edge)
        => edge switch {
            ToolDockEdge.Left => FileManagerDockEdge.Left,
            ToolDockEdge.Right => FileManagerDockEdge.Right,
            var _ => FileManagerDockEdge.Bottom
        };

    private void SyncViewChecks()
    {
        _syncingView = true;
        ViewTree.IsChecked = _layout.IsVisible(FileManagerTool.Tree);
        ViewMetadata.IsChecked = _layout.IsVisible(FileManagerTool.Metadata);
        ViewLog.IsChecked = _layout.IsVisible(FileManagerTool.Log);
        ViewTreeFiles.IsChecked = _treeFiles;
        _syncingView = false;
    }

    private void SaveSettings()
    {
        CopyBubble();
        FileManagerSettingsStore.Save(
            FileManagerSettingsStore.ConfigPath(), _session.Servers.Where(server => !server.Configured).ToArray(), _layout, _bubble, _treeFiles, _session.KeyStores);
    }

    private void BindServers()
    {
        _fillingServers = true;
        ServerBox.ItemsSource = null;
        ServerBox.ItemsSource = _session.Servers.ToArray();
        ServerBox.SelectedItem = _session.Active;
        _fillingServers = false;
        if (_boundServer != _session.Active?.Id)
            BindKeyStores(true);
    }

    private void BindKeyStores(bool resetToServer)
    {
        var selectedId = resetToServer ? _session.Active?.KeyStoreId : (KeyStoreBox.SelectedItem as KeyStoreChoice)?.Id;
        var items = new List<KeyStoreChoice> { new() { Label = "(None)" } };
        foreach (var store in _session.KeyStores)
            items.Add(new() { Id = store.Id, Label = string.IsNullOrWhiteSpace(store.Name) ? "Key store" : store.Name });

        KeyStoreBox.ItemsSource = items;
        KeyStoreBox.SelectedItem = items.FirstOrDefault(item => item.Id == selectedId) ?? items[0];
        KeyStoreBox.IsEnabled = _session.Active == null || _session.Active.Kind != FileManagerServerKind.Remote;
        _boundServer = _session.Active?.Id;
    }

    private void OnServerChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_fillingServers || ServerBox.SelectedItem is not FileManagerServer server || _session.Active?.Id == server.Id)
            return;

        _session.Select(server.Id);
        BindKeyStores(true);
        if (_treeServer == server.Id && _treeKey == OpenKey(server)) {
            ApplyCapabilities();
            return;
        }

        ClearBrowser();
        _treeServer = null;
        _treeKey = null;
        EmptyState.Text = "Add a server and connect.";
        EmptyState.IsVisible = true;
        Breadcrumb.Children.Clear();
        ApplyCapabilities();
    }

    private async void OnAddServer(object? sender, RoutedEventArgs e)
    {
        try {
            var server = await Dialogs.AddServerAsync(this, Log);
            if (server == null)
                return;

            _session.Add(server);
            SaveSettings();
            BindServers();
            Log("Added " + server.Name + ".");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnRenameServer(object? sender, RoutedEventArgs e)
    {
        if (_session.Active == null)
            return;

        var name = await PromptDialog.AskAsync(this, "Rename server", "Name", _session.Active.Name);
        if (string.IsNullOrWhiteSpace(name))
            return;

        _session.Rename(_session.Active.Id, name);
        SaveSettings();
        BindServers();
        SyncTabs();
    }

    private void OnRemoveServer(object? sender, RoutedEventArgs e)
    {
        if (_session.Active == null)
            return;

        _session.Remove(_session.Active.Id);
        SaveSettings();
        BindServers();
        SyncTabs();
        _ = ShowActiveAsync();
    }

    private async void OnAuthenticate(object? sender, RoutedEventArgs e)
    {
        var server = _session.Active;
        if (server == null || server.Kind != FileManagerServerKind.Remote) {
            Log("Authenticate is for a remote server.");
            return;
        }

        var auth = await Dialogs.AuthenticateAsync(this, server);
        if (auth == null)
            return;

        _session.SetAuth(auth.Value.Kind, auth.Value.Secret);
        SaveSettings();
        Log("Credential saved on " + server.Name + ".");
    }

    private async void OnConnect(object? sender, RoutedEventArgs e) => await ConnectCurrent();

    private async void OnKeyStore(object? sender, RoutedEventArgs e)
    {
        try {
            var saved = await Dialogs.KeyStoresAsync(this, _session.KeyStores);
            if (saved == null)
                return;

            _session.ReplaceKeyStores(saved);
            SaveSettings();
            BindKeyStores(false);
            Log("Key stores saved. Connect to use the selected store.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async Task ConnectCurrent()
    {
        var server = _session.Active;
        if (server != null && server.Kind != FileManagerServerKind.Remote)
            server.KeyStoreId = (KeyStoreBox.SelectedItem as KeyStoreChoice)?.Id;

        var id = server?.Id;
        try {
            await _session.ConnectAsync(CancellationToken.None);
            if (_session.Error != null) {
                Log(_session.Error);
                if (_treeServer != id || _treeKey != OpenKey(server))
                    ClearBrowser();

                ApplyCapabilities();
                return;
            }

            Log("Connected.");
            SyncTabs();
            await ShowActiveAsync();
        }
        catch (Exception ex) {
            Log(ex.Message);
            if (_treeServer != id || _treeKey != OpenKey(server))
                ClearBrowser();
        }

        ApplyCapabilities();
    }

    private async Task ShowActiveAsync(bool restore = false)
    {
        var key = OpenKey(_session.Active);
        var open = _session.Active != null && _session.Connections.Any(connection => connection.ServerId == _session.Active.Id && connection.KeyStoreId == key);
        _connected = open;
        _treeServer = open ? _session.Active!.Id : null;
        _treeKey = open ? key : null;
        if (!open) {
            ClearBrowser();
            EmptyState.IsVisible = true;
            BreadcrumbText();
            return;
        }

        EmptyState.IsVisible = false;
        if (restore && TryRestoreTree(_session.Active!.Id, key))
            return;

        _trees.Remove((_session.Active!.Id, key));
        ResetTree();
        await OpenNode(_roots[0]);
    }

    private void CaptureTree()
    {
        if (_treeServer is not Guid server || _roots.Count == 0)
            return;

        _trees[(server, _treeKey)] = new([.. _roots], _current);
    }

    private bool TryRestoreTree(Guid serverId, Guid? keyStoreId)
    {
        if (!_trees.TryGetValue((serverId, keyStoreId), out var snapshot) || snapshot.Roots.Count == 0)
            return false;

        _fillingTree = true;
        _roots.Clear();
        foreach (var root in snapshot.Roots)
            _roots.Add(root);

        Folders.ItemsSource = _roots;
        _fillingTree = false;
        var current = snapshot.Current ?? _roots[0];
        _ = FinishRestore(current);
        return true;
    }

    private async Task FinishRestore(FolderNode current)
    {
        _applyingExpand = true;
        try {
            await OpenNode(current);
            _fillingTree = true;
            Folders.SelectedItem = Shown(current);
            _fillingTree = false;
        }
        finally {
            _applyingExpand = false;
        }
    }

    private void SyncTabs()
    {
        _syncingTabs = true;
        try {
            var open = _session.Connections;
            for (var i = _tabs.Count - 1; i >= 0; i--) {
                var tab = _tabs[i];
                if (open.All(connection => connection.ServerId != tab.ServerId || connection.KeyStoreId != tab.KeyStoreId)) {
                    _tabs.RemoveAt(i);
                    Connections.Close(i);
                }
            }

            foreach (var connection in open) {
                var index = _tabs.FindIndex(tab => tab.ServerId == connection.ServerId && tab.KeyStoreId == connection.KeyStoreId);
                if (index >= 0) {
                    Connections.Rename(index, connection.Title);
                    continue;
                }

                _tabs.Add(new(connection.ServerId, connection.KeyStoreId));
                Connections.Add(new() { Header = connection.Title, Content = new Panel { Height = 0 }, CanClose = true });
            }

            var activeKey = OpenKey(_session.Active);
            var selected = _tabs.FindIndex(tab => tab.ServerId == _session.Active?.Id && tab.KeyStoreId == activeKey);
            if (selected >= 0)
                Connections.Select(selected);
        }
        finally {
            _syncingTabs = false;
        }
    }

    private void OnConnectionClosed(int index)
    {
        if (_syncingTabs || index < 0 || index >= _tabs.Count)
            return;

        var tab = _tabs[index];
        _tabs.RemoveAt(index);
        _session.CloseConnection(tab.ServerId, tab.KeyStoreId);
        BindServers();
        BindKeyStores(true);
    }

    private void OnConnectionSelected()
    {
        if (_syncingTabs)
            return;

        var index = Connections.SelectedIndex;
        if (index < 0 || index >= _tabs.Count) {
            if (_session.Connections.Count == 0)
                _ = ShowActiveAsync();

            return;
        }

        var tab = _tabs[index];
        var server = _session.Servers.FirstOrDefault(item => item.Id == tab.ServerId);
        if (server != null && server.Kind != FileManagerServerKind.Remote)
            server.KeyStoreId = tab.KeyStoreId;

        if (_session.Active?.Id == tab.ServerId && _treeServer == tab.ServerId && _treeKey == tab.KeyStoreId)
            return;

        CaptureTree();
        _session.Select(tab.ServerId);
        BindServers();
        BindKeyStores(true);
        _ = ShowActiveAsync(true);
    }

    private static Guid? OpenKey(FileManagerServer? server) => server == null || server.Kind == FileManagerServerKind.Remote ? null : server.KeyStoreId;

    private void ApplyCapabilities()
    {
        var commands = FileManagerCommandState.From(_session.Capabilities);
        EditMove.IsEnabled = commands.Move;
        EditCopy.IsEnabled = commands.Copy;
        FolderMove.IsEnabled = commands.Move;
        FolderCopy.IsEnabled = commands.Copy;
        FileMove.IsEnabled = commands.Move;
        FileCopy.IsEnabled = commands.Copy;
        KeysAccess.IsEnabled = commands.AccessLinks;
        FileAccess.IsEnabled = commands.AccessLinks;
        KeysPresigned.IsEnabled = commands.PresignedRead;
        FilePresigned.IsEnabled = commands.PresignedRead;
        KeysRotate.IsEnabled = commands.Encryption;
        KeysMigrate.IsEnabled = commands.Encryption;
        FileRotate.IsEnabled = commands.Encryption;
    }

    private void OnShowFiles(object? sender, RoutedEventArgs e)
    {
        _query = false;
        FileBar.IsVisible = true;
        QueryBar.IsVisible = false;
        PrefixColumn().IsVisible = false;
        ShowRows(_session.Entries.Select(FileRow.FromEntry));
    }

    private void OnShowQuery(object? sender, RoutedEventArgs e)
    {
        _query = true;
        FileBar.IsVisible = false;
        QueryBar.IsVisible = true;
        PrefixColumn().IsVisible = true;
    }

    private async void OnToggleTreeFiles(object? sender, RoutedEventArgs e)
    {
        if (_syncingView)
            return;

        _treeFiles = ViewTreeFiles.IsChecked;
        SaveSettings();
        if (_roots.Count == 0)
            return;

        await ReloadTree(_roots[0]);
    }

    private void OnViewOpened(object? sender, RoutedEventArgs e) => SyncViewChecks();

    private void OnToggleTree(object? sender, RoutedEventArgs e) => Toggle(FileManagerTool.Tree, ViewTree.IsChecked);

    private void OnToggleMetadata(object? sender, RoutedEventArgs e) => Toggle(FileManagerTool.Metadata, ViewMetadata.IsChecked);

    private void OnToggleLog(object? sender, RoutedEventArgs e) => Toggle(FileManagerTool.Log, ViewLog.IsChecked);

    private void Toggle(FileManagerTool tool, bool visible)
    {
        if (_syncingView)
            return;

        _layout.SetVisible(tool, visible);
        ApplyLayout();
        SaveSettings();
    }

    private void OnResetLayout(object? sender, RoutedEventArgs e)
    {
        _layout = FileManagerLayout.CreateDefault();
        ApplyLayout();
        SaveSettings();
    }

    private async void OnFolderSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_fillingTree)
            return;

        var nodes = SelectedTreeNodes();
        if (nodes.Count == 0)
            return;

        _fromTree = true;
        if (nodes.Count == 1 && nodes[0].Entry is { IsDirectory: true })
            await OpenNode(nodes[0]);
        else if (nodes.Count == 1 && nodes[0].Entry == null)
            await OpenNode(nodes[0]);

        await ShowMetadata();
    }

    private async void OnTreeItemProperty(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TreeViewItem.IsExpandedProperty || sender is not TreeViewItem { DataContext: FolderNode node })
            return;

        var canonical = node.Source ?? node;
        if (canonical.IsPlaceholder)
            return;

        var expanded = e.NewValue is true;
        canonical.IsExpanded = expanded;
        if (!ReferenceEquals(node, canonical))
            node.IsExpanded = expanded;

        if (!expanded || _applyingExpand || canonical.Entry is { IsDirectory: false })
            return;

        await OpenNode(canonical);
    }

    private async void OnUp(object? sender, RoutedEventArgs e)
    {
        try {
            await _session.UpAsync(CancellationToken.None);
            _current = ParentOf(_current) ?? _roots[0];
            _fillingTree = true;
            Folders.SelectedItem = Shown(_current);
            _fillingTree = false;
            ShowRows(_session.Entries.Select(FileRow.FromEntry));
            BreadcrumbText();
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e)
    {
        try {
            if (_current != null)
                _current.Loaded = false;

            await _session.RefreshAsync(CancellationToken.None);
            if (_current != null)
                await EnsureChildren(_current);

            ShowRows(_session.Entries.Select(FileRow.FromEntry));
            BreadcrumbText();
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnUpload(object? sender, RoutedEventArgs e)
    {
        try {
            var picked = await StorageProvider.OpenFilePickerAsync(new() { AllowMultiple = true, Title = "Upload" });
            if (picked.Count == 0)
                return;

            IReadOnlyList<string> keys = [];
            try {
                keys = await _session.ListKeyIdsAsync(CancellationToken.None);
            }
            catch (Exception ex) {
                Log(ex.Message);
            }

            var store = _session.KeyStoreFor(_session.Active);
            keys = FileManagerKeys.Ids(store, keys);
            var canEncrypt = FileManagerCommandState.From(_session.Capabilities).CanEncrypt(store?.HasMaterial() == true, keys.Count);
            var options = await Dialogs.UploadOptionsAsync(this, keys, canEncrypt, Log);
            if (options == null)
                return;

            await UploadPaths(picked.Select(file => file.TryGetLocalPath()).Where(path => path != null)!, options);
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var tab = Connections.IndexAt(e.Source as StyledElement);
        if (tab >= 0) {
            var carried = e.DataTransfer.TryGetValue(DragFormat);
            e.DragEffects = carried != null && carried.Entries.Count > 0 && !IsCurrentTab(tab) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (!OverBrowser(e))
            return;

        var payload = e.DataTransfer.TryGetValue(DragFormat);
        if (payload != null) {
            var plan = FileManagerDrop.Plan(payload.Entries, DropTarget(e));
            var copy = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            e.DragEffects = plan.HasWork ? copy ? DragDropEffects.Copy : DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (LocalPaths(e).Count == 0)
            return;

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var tab = Connections.IndexAt(e.Source as StyledElement);
        if (tab >= 0) {
            e.Handled = true;
            var carried = e.DataTransfer.TryGetValue(DragFormat);
            if (carried == null || carried.Entries.Count == 0 || IsCurrentTab(tab))
                return;

            await MoveOrCopy(true, carried.Entries, _tabs[tab].ServerId);
            return;
        }

        if (!OverBrowser(e))
            return;

        try {
            var payload = e.DataTransfer.TryGetValue(DragFormat);
            if (payload != null) {
                await ApplyDrop(payload.Entries, DropTarget(e), e.KeyModifiers.HasFlag(KeyModifiers.Control));
                return;
            }

            var paths = LocalPaths(e);
            if (paths.Count == 0)
                return;

            await UploadPaths(paths, new(), DropTarget(e));
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async Task ApplyDrop(IReadOnlyList<FileManagerEntry> entries, string? destination, bool copy)
    {
        var plan = FileManagerDrop.Plan(entries, destination);
        if (plan.Rejected.Count > 0)
            Log("Cannot move " + string.Join(", ", plan.Rejected) + " into itself.");

        if (!plan.HasWork) {
            if (plan.Rejected.Count == 0)
                Log("Already in that folder.");

            return;
        }

        foreach (var folder in plan.Folders) {
            var prefix = plan.FolderPrefix(folder);
            if (copy)
                await _session.CopyFolderAsync(folder, prefix, CancellationToken.None);
            else
                await _session.MoveFolderAsync(folder, prefix, CancellationToken.None);
        }

        if (plan.Files.Count > 0) {
            if (copy)
                await _session.CopyAsync(plan.Files, plan.Destination, CancellationToken.None);
            else
                await _session.MoveAsync(plan.Files, plan.Destination, CancellationToken.None);
        }

        if (plan.Physical.Count > 0) {
            if (copy)
                await _session.CopyPhysicalAsync(plan.Physical, plan.Destination, CancellationToken.None);
            else
                await _session.MovePhysicalAsync(plan.Physical, plan.Destination, CancellationToken.None);
        }

        await ReloadCurrent();
        List<string?> prefixes = [plan.Destination];
        foreach (var folder in plan.Folders)
            prefixes.Add(PrefixRewrite.Parent(folder.PathPrefix));

        foreach (var file in plan.Files)
            prefixes.Add(file.PathPrefix);

        foreach (var file in plan.Physical)
            prefixes.Add(file.PathPrefix);

        await RefreshPrefixes(prefixes);
        Log(copy ? "Copied." : "Moved.");
    }

    private async Task UploadPaths(IEnumerable<string> paths, FileManagerUploadOptions options, string? destination = null)
    {
        try {
            var keyId = FileManagerKeys.Resolve(options.Encrypt, options.KeyId, FileManagerKeys.Ids(_session.KeyStoreFor(_session.Active), null));
            if (options.Encrypt && keyId == null) {
                Log("Encryption needs a key id. Add one under Keys → Key stores.");
                return;
            }

            options.KeyId = keyId;
            var prefix = destination ?? _session.Prefix;
            var uploaded = 0;
            foreach (var path in paths) {
                var info = new FileInfo(path);
                if (!info.Exists) {
                    Log(path + " was not found.");
                    continue;
                }

                if (info.Length == 0) {
                    Log(info.Name + " is empty.");
                    continue;
                }

                await _session.UploadAsync(path, prefix, options, CancellationToken.None);
                uploaded++;
            }

            if (uploaded == 0)
                return;

            await ReloadCurrent();
            await RefreshPrefixes([prefix]);
            Log("Uploaded.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private string? DropTarget(DragEventArgs e)
    {
        var row = FindAncestor<DataGridRow>(e.Source);
        if (row?.DataContext is FileRow file && file.Entry != null)
            return FileManagerDrop.Target(file.Entry, _session.Prefix);

        var current = e.Source as Visual;
        while (current != null) {
            if (current is TreeViewItem item && item.DataContext is FolderNode node) {
                node = node.Source ?? node;
                if (!node.IsPlaceholder)
                    return FileManagerDrop.Target(node.Entry, node.Prefix);
            }

            current = current.GetVisualParent();
        }

        return IsInside(e.Source, Files) ? FileManagerDrop.Target(null, _session.Prefix) : null;
    }

    private bool OverBrowser(DragEventArgs e) => IsInside(e.Source, Files) || IsInside(e.Source, TreePanel);

    private async Task<IReadOnlyList<string>> FolderNames(Guid? serverId)
    {
        if (serverId is not Guid id)
            return [];

        try {
            return await _session.FolderPrefixesAsync(id, CancellationToken.None);
        }
        catch (Exception ex) {
            Log(ex.Message);
            return [];
        }
    }

    private bool IsCurrentTab(int index)
    {
        if (index < 0 || index >= _tabs.Count)
            return false;

        var tab = _tabs[index];
        return tab.ServerId == _session.Active?.Id && tab.KeyStoreId == OpenKey(_session.Active);
    }

    private List<string> LocalPaths(DragEventArgs e)
    {
        List<string> paths = [];
        foreach (var item in e.DataTransfer.Items) {
            var path = item.TryGetFile()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
                paths.Add(path);
        }

        return paths;
    }

    private void OnItemPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_itemDrag || sender is not Control host || e.Source is Button or TextBox)
            return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        if (!e.GetCurrentPoint(host).Properties.IsLeftButtonPressed)
            return;

        _pressKeepsSelection = false;
        _pressedRow = null;
        _keptFiles = null;
        _keptNodes = null;
        if (ReferenceEquals(host, Files) && FindAncestor<DataGridRow>(e.Source)?.DataContext is FileRow file && file.Entry != null) {
            var rows = Files.SelectedItems?.OfType<FileRow>().Where(row => row.Entry != null).ToList() ?? [];
            if (FileManagerDragSelection.Keeps(rows.Count, rows.Any(row => SameEntry(row.Entry!, file.Entry)))) {
                e.Handled = true;
                _pressKeepsSelection = true;
                _pressedRow = file;
                _keptFiles = rows;
            }
        }
        else if (FindAncestor<TreeViewItem>(e.Source)?.DataContext is FolderNode node && (node.Source ?? node).Entry != null) {
            var pressed = node.Source ?? node;
            var nodes = Folders.SelectedItems?.OfType<FolderNode>().Select(item => item.Source ?? item).Where(item => item.Entry != null).ToList() ?? [];
            if (FileManagerDragSelection.Keeps(nodes.Count, nodes.Any(item => SameEntry(item.Entry!, pressed.Entry!)))) {
                e.Handled = true;
                _pressKeepsSelection = true;
                _pressedRow = pressed;
                _keptNodes = nodes;
            }
        }

        _itemHost = host;
        _itemEvent = e;
        _itemPress = e.GetPosition(host);
    }

    private void OnItemReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_pressKeepsSelection || _itemDrag)
            return;

        _pressKeepsSelection = false;
        _keptFiles = null;
        _keptNodes = null;
        if (_pressedRow is FileRow file) {
            _fillingList = true;
            Files.SelectedItem = file;
            _fillingList = false;
        }
        else if (_pressedRow is FolderNode node) {
            _fillingTree = true;
            Folders.SelectedItem = Shown(node);
            _fillingTree = false;
        }
    }

    private async void OnItemMoved(object? sender, PointerEventArgs e)
    {
        if (_itemDrag || _itemPress == null || _itemEvent == null || sender is not Control host || !ReferenceEquals(host, _itemHost))
            return;

        if (!e.GetCurrentPoint(host).Properties.IsLeftButtonPressed) {
            _itemPress = null;
            _itemEvent = null;
            return;
        }

        var delta = e.GetPosition(host) - _itemPress.Value;
        if (Math.Abs(delta.X) < 8 && Math.Abs(delta.Y) < 8)
            return;

        var origin = _itemPress.Value;
        var press = _itemEvent;
        _itemPress = null;
        _itemEvent = null;
        _pressKeepsSelection = false;
        RestoreKeptSelection();
        var entries = Collect(host, origin);
        if (entries.Count == 0 || !_connected)
            return;

        _itemDrag = true;
        e.Handled = true;
        try {
            var transfer = new DataTransfer();
            transfer.Add(DataTransferItem.Create(DragFormat, new FileDragPayload { Entries = entries }));
            await DragDrop.DoDragDropAsync(press, transfer, DragDropEffects.Move | DragDropEffects.Copy);
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
        finally {
            _itemDrag = false;
        }
    }

    private void RestoreKeptSelection()
    {
        if (_keptFiles != null && Files.SelectedItems != null) {
            _fillingList = true;
            Files.SelectedItems.Clear();
            foreach (var row in _keptFiles)
                Files.SelectedItems.Add(row);

            _fillingList = false;
        }

        if (_keptNodes != null && Folders.SelectedItems != null) {
            _fillingTree = true;
            Folders.SelectedItems.Clear();
            foreach (var node in _keptNodes)
                Folders.SelectedItems.Add(Shown(node));

            _fillingTree = false;
        }

        _keptFiles = null;
        _keptNodes = null;
    }

    private List<FileManagerEntry> Collect(Control host, Point point)
    {
        var hit = host.InputHitTest(point);
        if (ReferenceEquals(host, Files)) {
            var row = FindAncestor<DataGridRow>(hit);
            if (row?.DataContext is not FileRow file || file.Entry == null)
                return [];

            var selected = Files.SelectedItems?.OfType<FileRow>().Select(item => item.Entry).OfType<FileManagerEntry>().ToList() ?? [];
            return selected.Any(entry => SameEntry(entry, file.Entry)) ? selected : [file.Entry];
        }

        var item = FindAncestor<TreeViewItem>(hit);
        if (item?.DataContext is not FolderNode node)
            return [];

        node = node.Source ?? node;
        if (node.IsPlaceholder || node.Entry == null)
            return [];

        var picked = SelectedTreeNodes().Select(row => row.Entry).OfType<FileManagerEntry>().ToList();
        return picked.Any(entry => SameEntry(entry, node.Entry)) ? picked : [node.Entry];
    }

    private static bool SameEntry(FileManagerEntry left, FileManagerEntry right)
    {
        if (left.FileId != null && left.FileId == right.FileId)
            return true;

        return left.IsDirectory == right.IsDirectory && left.Name == right.Name && SameText(left.PathPrefix, right.PathPrefix) &&
            (left.PhysicalKey ?? "") == (right.PhysicalKey ?? "");
    }

    private static bool SameText(string? left, string? right) => string.Equals(left?.Trim().Trim('/') ?? "", right?.Trim().Trim('/') ?? "", StringComparison.Ordinal);

    private async Task RefreshPrefixes(IEnumerable<string?> prefixes)
    {
        foreach (var prefix in prefixes.Select(value => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('/')).Distinct()) {
            var node = prefix == null ? _roots[0] : FindByPrefix(_roots, prefix);
            if (node == null)
                continue;

            node.Loaded = false;
            await EnsureChildren(node);
        }
    }

    private static T? FindAncestor<T>(object? source)
        where T : Visual
    {
        var current = source as Visual;
        while (current != null) {
            if (current is T match)
                return match;

            current = current.GetVisualParent();
        }

        return null;
    }

    private static bool IsInside(object? source, Visual host)
    {
        var current = source as Visual;
        while (current != null) {
            if (ReferenceEquals(current, host))
                return true;

            current = current.GetVisualParent();
        }

        return false;
    }

    private async void OnDownload(object? sender, RoutedEventArgs e)
    {
        var entries = SelectedEntries();
        if (entries.Count == 0) {
            Log("Select a file.");
            return;
        }

        if (entries.Count == 1 && !entries[0].IsDirectory) {
            var picked = await StorageProvider.SaveFilePickerAsync(new() { SuggestedFileName = entries[0].Name, Title = "Download" });
            var path = picked?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
                return;

            try {
                await _session.DownloadAsync(entries[0], path, CancellationToken.None);
                Log("Saved " + path);
            }
            catch (Exception ex) {
                Log(ex.Message);
            }

            return;
        }

        await SaveArchive(entries);
    }

    private async void OnFileDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Files.SelectedItem is not FileRow { Entry: { } entry })
            return;

        await OpenShown(entry);
    }

    private async void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        var item = FindAncestor<TreeViewItem>(e.Source);
        if (item?.DataContext is not FolderNode node)
            return;

        node = node.Source ?? node;
        if (node.Entry is not { IsDirectory: false } entry)
            return;

        e.Handled = true;
        await OpenShown(entry);
    }

    private async void OnOpen(object? sender, RoutedEventArgs e)
    {
        var entry = SelectedEntries().FirstOrDefault();
        if (entry == null)
            return;

        await OpenShown(entry);
    }

    private async Task OpenShown(FileManagerEntry entry)
    {
        if (entry.IsDirectory) {
            await OpenChild(entry);
            return;
        }

        try {
            var dest = Path.Combine(Path.GetTempPath(), entry.Name);
            await _session.DownloadAsync(entry, dest, CancellationToken.None);
            Process.Start(new ProcessStartInfo(dest) { UseShellExecute = true });
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnRename(object? sender, RoutedEventArgs e)
    {
        var entries = SelectedEntries();
        if (entries.Count != 1) {
            Log("Select one item to rename.");
            return;
        }

        var name = await PromptDialog.AskAsync(this, "Rename", "New name", entries[0].Name);
        if (string.IsNullOrWhiteSpace(name))
            return;

        try {
            var result = await _session.RenameAsync(entries[0], name, CancellationToken.None);
            await ReloadCurrent();
            Log(result.Failed == 0 ? "Renamed." : _session.Error ?? "Rename failed.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnMove(object? sender, RoutedEventArgs e) => await MoveOrCopy(true);

    private async void OnCopy(object? sender, RoutedEventArgs e) => await MoveOrCopy(false);

    private async Task MoveOrCopy(bool move, IReadOnlyList<FileManagerEntry>? dragged = null, Guid? connectionId = null)
    {
        var entries = dragged ?? SelectedEntries();
        if (entries.Count == 0) {
            Log("Select a file or folder.");
            return;
        }

        var destinations = _session.TransferDestinations();
        var names = destinations.Select(server => {
                var title = _session.Connections.FirstOrDefault(connection => connection.ServerId == server.Id)?.Title;
                return string.IsNullOrWhiteSpace(title) ? server.Name : title;
            })
            .ToArray();

        var localFolders = await FolderNames(_session.Active?.Id);
        var connectionFolders = new Dictionary<Guid, IReadOnlyList<string>>();
        foreach (var server in destinations)
            connectionFolders[server.Id] = await FolderNames(server.Id);

        var choice = await Dialogs.MoveOrCopyAsync(this, move, destinations, names, localFolders, connectionFolders, connectionId);
        if (choice == null)
            return;

        try {
            if (choice.ToConnection) {
                var progress = new Progress<FileStorageTransferProgress>(snapshot => Dispatcher.UIThread.Post(() => Log(TransferLine(snapshot))));
                var disposition = move ? FileStorageTransferDisposition.Move : FileStorageTransferDisposition.Copy;
                var result = await _session.TransferAsync(
                    choice.DestinationId, entries, disposition, choice.KeyMode, choice.TargetKeyId, choice.TargetKeyVersion, progress, CancellationToken.None, choice.Prefix);

                SyncTabs();
                await ShowActiveAsync();
                Log(
                    result.FilesFailed == 0
                        ? $"Transferred {result.FilesCopied}, skipped {result.FilesSkipped}, removed {result.FilesRemoved}."
                        : $"Transferred {result.FilesCopied}, failed {result.FilesFailed}.");

                return;
            }

            var destination = choice.Prefix ?? "";
            foreach (var folder in entries.Where(entry => entry.IsDirectory)) {
                if (move)
                    await _session.MoveFolderAsync(folder, destination, CancellationToken.None);
                else
                    await _session.CopyFolderAsync(folder, destination, CancellationToken.None);
            }

            var files = entries.Where(entry => !entry.IsDirectory && entry.FileId != null).ToList();
            if (files.Count > 0) {
                if (move)
                    await _session.MoveAsync(files, destination, CancellationToken.None);
                else
                    await _session.CopyAsync(files, destination, CancellationToken.None);
            }

            var physical = entries.Where(entry => !entry.IsDirectory && entry.FileId == null).ToList();
            if (physical.Count > 0) {
                if (move)
                    await _session.MovePhysicalAsync(physical, destination, CancellationToken.None);
                else
                    await _session.CopyPhysicalAsync(physical, destination, CancellationToken.None);
            }

            await ReloadCurrent();
            Log(move ? "Moved." : "Copied.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private static string TransferLine(FileStorageTransferProgress snapshot)
        => snapshot.ItemOutcome == null
            ? $"{snapshot.Phase} {snapshot.FilesCompleted}/{snapshot.FilesTotal}"
            : $"{snapshot.ItemOutcome} {snapshot.FilesCompleted}/{snapshot.FilesTotal}";

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        var entries = SelectedEntries();
        if (entries.Count == 0) {
            Log("Select a file or folder.");
            return;
        }

        if (!await PromptDialog.ConfirmAsync(this, "Delete", "Delete the selection?", "Delete"))
            return;

        try {
            foreach (var folder in entries.Where(entry => entry.IsDirectory))
                await _session.DeleteFolderAsync(folder, CancellationToken.None);

            var files = entries.Where(entry => !entry.IsDirectory && entry.FileId != null).ToList();
            if (files.Count > 0)
                await _session.DeleteAsync(files, CancellationToken.None);

            var physical = entries.Where(entry => !entry.IsDirectory && entry.FileId == null).ToList();
            if (physical.Count > 0)
                await _session.DeletePhysicalAsync(physical, CancellationToken.None);

            await ReloadCurrent();
            Log("Deleted.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        var name = await PromptDialog.AskAsync(this, "New folder", "Folder name");
        if (string.IsNullOrWhiteSpace(name))
            return;

        _session.NewFolder(name);
        var created = _session.Entries.LastOrDefault(entry => entry.Pending && entry.Name == name.Trim().Trim('/'));
        var parent = _current ?? _roots[0];
        if (created != null) {
            if (!parent.Loaded)
                await EnsureChildren(parent);

            var child = new FolderNode { Name = created.Name, Prefix = created.PathPrefix, Entry = created };
            child.Children.Add(Placeholder());
            if (parent.Children.All(node => node.Prefix != child.Prefix))
                parent.Children.Add(child);

            var shownParent = Shown(parent);
            if (shownParent != null && Folders.ContainerFromItem(shownParent) is TreeViewItem item)
                item.IsExpanded = true;

            await OpenNode(child);
        }

        Log("Folder stays until refresh if nothing is uploaded into it.");
    }

    private async void OnZip(object? sender, RoutedEventArgs e) => await SaveArchive(SelectedEntries());

    private async Task SaveArchive(IReadOnlyList<FileManagerEntry> entries)
    {
        if (entries.Count == 0) {
            Log("Select a file or folder.");
            return;
        }

        var picked = await StorageProvider.SaveFilePickerAsync(new() { SuggestedFileName = "files.zip", Title = "Zip" });
        var path = picked?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        try {
            await _session.DownloadArchiveAsync(entries, path, CancellationToken.None);
            Log("Saved " + path);
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnAccessLink(object? sender, RoutedEventArgs e)
    {
        var id = SelectedEntries().Select(entry => entry.FileId).FirstOrDefault(value => value != null);
        if (id == null) {
            Log("Select a catalog file.");
            return;
        }

        var max = await PromptDialog.AskAsync(this, "Access link", "Max downloads (optional)");
        if (max == null)
            return;

        int? cap = int.TryParse(max, out var parsed) ? parsed : null;
        try {
            var url = await _session.CreateAccessLinkAsync(id.Value, DateTime.UtcNow.AddDays(1), cap, CancellationToken.None);
            await PromptDialog.ShowTextAsync(this, "Access link", url);
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnPresigned(object? sender, RoutedEventArgs e)
    {
        var id = SelectedEntries().Select(entry => entry.FileId).FirstOrDefault(value => value != null);
        if (id == null) {
            Log("Select a catalog file.");
            return;
        }

        try {
            var url = await _session.PresignedReadAsync(id.Value, CancellationToken.None);
            await PromptDialog.ShowTextAsync(this, "Presigned read", url);
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnRotate(object? sender, RoutedEventArgs e)
    {
        var entries = SelectedEntries();
        if (entries.Count == 0) {
            Log("Select a file or folder.");
            return;
        }

        var key = await PromptDialog.AskAsync(this, "Rotate DEKs", "Target key id (optional)");
        if (key == null)
            return;

        try {
            var result = await _session.RotateAsync(entries, string.IsNullOrWhiteSpace(key) ? null : key, null, CancellationToken.None);
            await ReloadCurrent();
            Log($"Rotated {result.SuccessfullyMigrated}, failed {result.Failed}.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnMigrate(object? sender, RoutedEventArgs e)
    {
        var source = await PromptDialog.AskAsync(this, "Migrate keys", "Source key id");
        if (string.IsNullOrWhiteSpace(source))
            return;

        var target = await PromptDialog.AskAsync(this, "Migrate keys", "Target key id (optional)");
        if (target == null)
            return;

        try {
            var result = await _session.MigrateAsync(source, null, string.IsNullOrWhiteSpace(target) ? null : target, null, CancellationToken.None);
            await ReloadCurrent();
            Log($"Migrated {result.SuccessfullyMigrated}, failed {result.Failed}.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async void OnRunQuery(object? sender, RoutedEventArgs e)
    {
        try {
            await _session.QueryAsync(NameFilter.Text, PrefixFilter.Text, CancellationToken.None);
            ShowRows(_session.QueryRows.Select(FileRow.FromQuery));
            Log($"{_session.QueryRows.Count} row(s).");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private void OnListSearch(object? sender, TextChangedEventArgs e) => ApplyListFilter();

    private void OnTreeSearch(object? sender, TextChangedEventArgs e) => ApplyTreeFilter();

    private async void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_fillingList)
            return;

        _fromTree = false;
        await ShowMetadata();
    }

    private async Task OpenNode(FolderNode node)
    {
        _current = node;
        try {
            if (!_connected)
                return;

            if (node.Entry != null)
                await _session.EnterAsync(node.Entry, CancellationToken.None);
            else
                await _session.OpenAsync(node.Prefix, CancellationToken.None);

            ShowRows(_session.Entries.Select(FileRow.FromEntry));
            BreadcrumbText();
            await EnsureChildren(node);
            if (_session.Truncated)
                Log("Listing was truncated.");
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async Task OpenChild(FileManagerEntry entry)
    {
        var parent = _current ?? _roots[0];
        if (!parent.Loaded)
            await EnsureChildren(parent);

        var child = parent.Children.FirstOrDefault(node => node.Prefix == entry.PathPrefix && !node.IsPlaceholder);
        if (child == null) {
            child = new() { Name = entry.Name, Prefix = entry.PathPrefix, Entry = entry };
            child.Children.Add(Placeholder());
            parent.Children.Add(child);
        }

        var shownParent = Shown(parent);
        if (shownParent != null && Folders.ContainerFromItem(shownParent) is TreeViewItem item)
            item.IsExpanded = true;

        await OpenNode(child);
        _fillingTree = true;
        Folders.SelectedItem = Shown(child);
        _fillingTree = false;
    }

    private async Task EnsureChildren(FolderNode node)
    {
        node = node.Source ?? node;
        if (node.Loaded || node.IsPlaceholder || !_connected)
            return;

        try {
            var listing = await _session.ListAsync(node.Prefix, CancellationToken.None);
            node.Children.Clear();
            foreach (var directory in listing.Entries.Where(entry => entry.IsDirectory)) {
                var child = new FolderNode { Name = directory.Name, Prefix = directory.PathPrefix, Entry = directory };
                child.Children.Add(Placeholder());
                node.Children.Add(child);
            }

            if (_treeFiles) {
                foreach (var file in listing.Entries.Where(entry => !entry.IsDirectory))
                    node.Children.Add(
                        new() {
                            Name = file.Name,
                            Prefix = file.PathPrefix,
                            Entry = file,
                            Loaded = true
                        });
            }

            node.Loaded = true;
            if (!string.IsNullOrWhiteSpace(TreeSearch.Text))
                ApplyTreeFilter();
        }
        catch (Exception ex) {
            Log(ex.Message);
        }
    }

    private async Task ReloadCurrent()
    {
        if (_current != null)
            _current.Loaded = false;

        await _session.OpenAsync(_session.Prefix, CancellationToken.None);
        if (_current != null)
            await EnsureChildren(_current);

        if (!_query)
            ShowRows(_session.Entries.Select(FileRow.FromEntry));
    }

    private void ShowRows(IEnumerable<FileRow> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);
        ApplyListFilter();
    }

    private void ApplyListFilter()
    {
        var term = _query ? "" : ListSearch.Text?.Trim() ?? "";
        var shown = term.Length == 0 ? _rows.ToArray() : _rows.Where(row => row.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();
        _fillingList = true;
        Files.ItemsSource = shown;
        _fillingList = false;
        EmptyState.Text = _connected ? "This folder is empty." : "Add a server and connect.";
        EmptyState.IsVisible = shown.Length == 0;
    }

    private void ApplyTreeFilter()
    {
        var term = TreeSearch.Text?.Trim() ?? "";
        _fillingTree = true;
        Folders.ItemsSource = term.Length == 0 ? _roots : Project(_roots, term);
        _fillingTree = false;
    }

    private static IReadOnlyList<FolderNode> Project(IEnumerable<FolderNode> nodes, string term)
    {
        var list = nodes.Where(node => !node.IsPlaceholder).ToList();
        var filtered = FolderTreeFilter.Apply(list.Select(ToModel).ToList(), term);
        return Pair(list, filtered);
    }

    private static FolderTreeNode ToModel(FolderNode node) => new() { Name = node.Name, Children = node.Children.Where(child => !child.IsPlaceholder).Select(ToModel).ToList() };

    private static List<FolderNode> Pair(List<FolderNode> sources, IReadOnlyList<FolderTreeNode> filtered)
    {
        List<FolderNode> result = [];
        foreach (var item in filtered) {
            var source = sources.FirstOrDefault(node => node.Name == item.Name);
            if (source == null)
                continue;

            var copy = new FolderNode {
                Name = source.Name,
                Prefix = source.Prefix,
                Entry = source.Entry,
                Loaded = source.Loaded,
                IsExpanded = source.IsExpanded,
                Source = source
            };

            foreach (var child in Pair(source.Children.Where(node => !node.IsPlaceholder).ToList(), item.Children))
                copy.Children.Add(child);

            if (!source.Loaded)
                copy.Children.Add(Placeholder());

            result.Add(copy);
        }

        return result;
    }

    private FolderNode? Shown(FolderNode canonical)
    {
        if (Folders.ItemsSource is not IEnumerable<FolderNode> nodes)
            return canonical;

        return FindShown(nodes, canonical) ?? canonical;
    }

    private static FolderNode? FindShown(IEnumerable<FolderNode> nodes, FolderNode canonical)
    {
        foreach (var node in nodes) {
            if (ReferenceEquals(node, canonical) || ReferenceEquals(node.Source, canonical) || (node.Prefix == canonical.Prefix && node.Name == canonical.Name))
                return node;

            var nested = FindShown(node.Children, canonical);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private FolderNode? ParentOf(FolderNode? node)
    {
        if (node == null)
            return null;

        return FindParent(_roots, node.Source ?? node);
    }

    private static FolderNode? FindParent(IEnumerable<FolderNode> nodes, FolderNode child)
    {
        foreach (var node in nodes) {
            if (node.Children.Any(item => ReferenceEquals(item, child) || item.Prefix == child.Prefix))
                return node;

            var nested = FindParent(node.Children, child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void BreadcrumbText()
    {
        Breadcrumb.Children.Clear();
        AddCrumb("/", null);
        var prefix = _session.Prefix;
        if (string.IsNullOrEmpty(prefix))
            return;

        var built = "";
        foreach (var segment in prefix.Split('/', StringSplitOptions.RemoveEmptyEntries)) {
            built = built.Length == 0 ? segment : built + "/" + segment;
            Breadcrumb.Children.Add(new TextBlock { Text = "/", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6 });
            AddCrumb(segment, built);
        }
    }

    private void AddCrumb(string label, string? prefix)
    {
        var button = new Button { Content = label, Padding = new(4, 0), Background = Brushes.Transparent };
        button.Click += async (_, _) => {
            var node = prefix == null ? _roots[0] : FindByPrefix(_roots, prefix) ?? _roots[0];
            await OpenNode(node);
            _fillingTree = true;
            Folders.SelectedItem = Shown(node);
            _fillingTree = false;
        };

        Breadcrumb.Children.Add(button);
    }

    private static FolderNode? FindByPrefix(IEnumerable<FolderNode> nodes, string prefix)
    {
        foreach (var node in nodes) {
            if (node.Prefix == prefix)
                return node.Source ?? node;

            var nested = FindByPrefix(node.Children, prefix);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private async Task ShowMetadata()
    {
        MetaRows.Children.Clear();
        _accordion.Clear();
        var entries = SelectedEntries();
        if (entries.Count == 0) {
            AddMeta("Selection", "Nothing selected");
            return;
        }

        if (entries.Count > 1) {
            AddMeta("Selection", entries.Count + " selected");
            return;
        }

        var entry = entries[0];
        if (entry.FileId is Guid id) {
            try {
                var metadata = await _session.GetMetadataAsync(id, true, CancellationToken.None);
                AddAccordion(
                    "Details", true,
                    [
                        ("Name", metadata.OriginalFileName ?? "", false), ("Size", metadata.OriginalFileSize + " bytes", false), ("Path", metadata.PathPrefix ?? "/", false),
                        ("Content type", metadata.ContentType ?? "", false), ("Id", metadata.Id.ToString(), true),
                        ("Hash", HashText(metadata.HashAlgorithm?.ToString(), metadata.OriginalFileHash), true), ("Stored hash", HashText(null, metadata.SourceFileHash), true)
                    ]);

                AddAccordion(
                    "Compression", false,
                    [
                        ("Algorithm", metadata.IsCompressed ? metadata.CompressionAlgorithm?.ToString() ?? "Yes" : "None", false),
                        ("Compressed size", metadata.CompressedFileSize?.ToString() ?? "-", false), ("Compressed hash", HashText(null, metadata.CompressedFileHash), true)
                    ]);

                AddAccordion(
                    "Encryption", false,
                    [
                        ("Algorithm", metadata.IsEncrypted ? metadata.DataEncryptionKeyAlgorithm?.ToString() ?? "Yes" : "None", false),
                        ("Key", metadata.IsEncrypted ? $"{metadata.DataEncryptionKeyId} / {metadata.DataEncryptionKeyVersion}" : "-", false),
                        ("Key encryption", metadata.KeyEncryptionKeyAlgorithm?.ToString() ?? "-", false), ("Encrypted size", metadata.EncryptedFileSize?.ToString() ?? "-", false),
                        ("Encrypted hash", HashText(null, metadata.EncryptedFileHash), true), ("KEK salt", HashText(null, metadata.KeyEncryptionKeySalt), true)
                    ]);
            }
            catch (Exception ex) {
                Log(ex.Message);
                AddMeta("Error", ex.Message);
            }

            return;
        }

        var folder = new List<(string Label, string Value, bool Id)> {
            ("Name", entry.Name, false), ("Path", entry.PathPrefix ?? "/", false), ("Kind", entry.IsDirectory ? "Folder" : "Physical", false)
        };

        if (!string.IsNullOrWhiteSpace(entry.PhysicalKey))
            folder.Add(("Physical key", entry.PhysicalKey, true));

        AddAccordion("Details", true, folder);
    }

    private void AddAccordion(string title, bool expanded, IReadOnlyList<(string Label, string Value, bool Id)> rows)
    {
        var body = new StackPanel {
            Spacing = 6,
            Margin = new(8, 6, 8, 8),
            IsVisible = expanded,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        foreach (var row in rows)
            body.Children.Add(row.Id ? IdLine(row.Label, row.Value) : Plain(row.Label, row.Value));

        var mark = new TextBlock { Text = expanded ? "▾" : "▸", VerticalAlignment = VerticalAlignment.Center, Margin = new(8, 0, 0, 0) };
        var heading = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center };
        var rowBar = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        rowBar.ColumnDefinitions.Add(new(GridLength.Star));
        rowBar.ColumnDefinitions.Add(new(GridLength.Auto));
        rowBar.Children.Add(heading);
        Grid.SetColumn(mark, 1);
        rowBar.Children.Add(mark);
        var header = new Border {
            Background = new SolidColorBrush(Color.Parse("#2A3140")),
            BorderBrush = new SolidColorBrush(Color.Parse("#5C677A")),
            BorderThickness = new(0, 0, 0, 1),
            Padding = new(8, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Cursor = new(StandardCursorType.Hand),
            Child = rowBar
        };

        var index = _accordion.Count;
        _accordion.Add((body, mark));
        header.PointerPressed += (_, e) => {
            e.Handled = true;
            ToggleAccordion(index);
        };

        var section = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        section.Children.Add(header);
        section.Children.Add(body);
        MetaRows.Children.Add(section);
    }

    private void ToggleAccordion(int index)
    {
        var open = !_accordion[index].Body.IsVisible;
        for (var i = 0; i < _accordion.Count; i++) {
            var show = open && i == index;
            _accordion[i].Body.IsVisible = show;
            _accordion[i].Mark.Text = show ? "▾" : "▸";
        }
    }

    private Control Plain(string label, string value)
    {
        var panel = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
        panel.Children.Add(new TextBlock { Text = label, Opacity = 0.7 });
        panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "-" : value, TextWrapping = TextWrapping.Wrap });
        return panel;
    }

    private Control IdLine(string label, string value)
    {
        var panel = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
        panel.Children.Add(new TextBlock { Text = label, Opacity = 0.7 });
        panel.Children.Add(new IdText { Value = value == "-" ? "" : value });
        return panel;
    }

    private static string HashText(string? algorithm, byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return "-";

        var hex = Convert.ToHexString(bytes);
        return string.IsNullOrWhiteSpace(algorithm) ? hex : algorithm + " " + hex;
    }

    private void AddMeta(string label, string value) => MetaRows.Children.Add(Plain(label, value));

    private DataGridColumn PrefixColumn() => Files.Columns.First(column => Equals(column.Header, "Prefix"));

    private List<FolderNode> SelectedTreeNodes()
    {
        if (Folders.SelectedItems == null)
            return [];

        return Folders.SelectedItems.OfType<FolderNode>().Select(node => node.Source ?? node).Where(node => !node.IsPlaceholder).ToList();
    }

    private async Task ReloadTree(FolderNode node)
    {
        var loaded = node.Children.Where(child => !child.IsPlaceholder && child.Loaded && child.Entry?.IsDirectory != false).Select(child => child.Prefix).ToList();
        if (node.Loaded || ReferenceEquals(node, _roots.FirstOrDefault())) {
            node.Loaded = false;
            await EnsureChildren(node);
        }

        foreach (var child in node.Children.Where(child => loaded.Contains(child.Prefix)).ToList())
            await ReloadTree(child);
    }

    private List<FileManagerEntry> SelectedEntries()
    {
        if (_fromTree)
            return SelectedTreeNodes().Select(node => node.Entry).OfType<FileManagerEntry>().ToList();

        if (Files.SelectedItems == null)
            return [];

        return Files.SelectedItems.OfType<FileRow>().Select(row => row.Entry).OfType<FileManagerEntry>().ToList();
    }

    private void ClearBrowser()
    {
        _connected = false;
        _roots.Clear();
        _current = null;
        Folders.ItemsSource = _roots;
        Files.ItemsSource = null;
        _rows.Clear();
        MetaRows.Children.Clear();
        _accordion.Clear();
        Breadcrumb.Children.Clear();
    }

    private void ResetTree()
    {
        _roots.Clear();
        _roots.Add(Root());
        _current = _roots[0];
        Folders.ItemsSource = _roots;
        Files.ItemsSource = null;
        _rows.Clear();
        BreadcrumbText();
    }

    private static FolderNode Root()
    {
        var root = new FolderNode { Name = "/" };
        root.Children.Add(Placeholder());
        return root;
    }

    private static FolderNode Placeholder() => new() { Name = " ", IsPlaceholder = true };

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox || InsideButton(e.Source as StyledElement))
            return;

        var tool = HeaderTool(e.Source as StyledElement);
        if (tool == null)
            return;

        _drag = tool;
        _headerPress = e.GetPosition(this);
        _headerMoved = false;
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_drag == null)
            return;

        if (!_headerMoved) {
            var delta = e.GetPosition(this) - _headerPress;
            if (delta.X * delta.X + delta.Y * delta.Y < 16)
                return;

            _headerMoved = true;
        }

        var edge = EdgeAt(e.GetPosition(DockArea));
        DropLeft.IsVisible = edge == ToolDockEdge.Left;
        DropRight.IsVisible = edge == ToolDockEdge.Right;
        DropBottom.IsVisible = edge == ToolDockEdge.Bottom;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_drag == null)
            return;

        var tool = _drag.Value;
        var moved = _headerMoved;
        var edge = EdgeAt(e.GetPosition(DockArea));
        _drag = null;
        _headerMoved = false;
        e.Pointer.Capture(null);
        DropLeft.IsVisible = false;
        DropRight.IsVisible = false;
        DropBottom.IsVisible = false;
        if (!moved || edge == null || FromDockEdge(edge.Value) == _layout.Edge(tool))
            return;

        _layout.Move(tool, FromDockEdge(edge.Value));
        ApplyLayout();
        SaveSettings();
    }

    private ToolDockEdge? EdgeAt(Point point) => ToolDockHit.EdgeAt(point.X, point.Y, DockArea.Bounds.Width, DockArea.Bounds.Height);

    private static bool InsideButton(StyledElement? source)
    {
        for (var node = source; node != null; node = node.Parent) {
            if (node is Button)
                return true;
        }

        return false;
    }

    private static FileManagerTool? HeaderTool(StyledElement? source)
    {
        for (var node = source; node != null; node = node.Parent) {
            if (node is Border { Tag: string id } && Enum.TryParse<FileManagerTool>(id, out var tool))
                return tool;
        }

        return null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (FocusManager?.GetFocusedElement() is TextBox)
            return;

        if (e.Key == Key.F2)
            OnRename(sender, e);
        else if (e.Key == Key.Delete)
            OnDelete(sender, e);
        else if (e.Key == Key.Enter)
            OnOpen(sender, e);
        else if (e.Key == Key.Back)
            OnUp(sender, e);
        else if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            TreeSearch.Focus();
        else
            return;

        e.Handled = true;
    }

    private void Log(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _logs.Write(message);
    }

    protected override void OnClosed(EventArgs e)
    {
        _session.Dispose();
        base.OnClosed(e);
    }

    private readonly record struct OpenTab(Guid ServerId, Guid? KeyStoreId);

    private sealed class TreeSnapshot(List<FolderNode> roots, FolderNode? current)
    {
        public List<FolderNode> Roots { get; } = roots;

        public FolderNode? Current { get; } = current;
    }

    private sealed class KeyStoreChoice
    {
        public Guid? Id { get; init; }

        public string Label { get; init; } = "";

        public override string ToString() => Label;
    }

    private sealed class FileDragPayload
    {
        public required IReadOnlyList<FileManagerEntry> Entries { get; init; }
    }
}