using Avalonia.Controls;
using Lyo.Desktop.Components;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Lyo.FileStorage;

namespace Lyo.FileManager.App;

internal sealed record FileManagerMoveCopyChoice(
    bool ToConnection,
    string? Prefix,
    Guid DestinationId,
    FileStorageKeyMode KeyMode,
    string? TargetKeyId,
    string? TargetKeyVersion);

internal static class Dialogs
{
    public static async Task<FileManagerServer?> AddServerAsync(Window owner, Action<string> log)
    {
        var name = new TextBox { PlaceholderText = "Name" };
        var kind = new ComboBox { ItemsSource = new[] { "Remote", "Local", "S3", "Azure Blob", "FTP", "SFTP" }, SelectedIndex = 0 };
        var baseUrl = new TextBox { PlaceholderText = "Base URL", Text = "http://localhost:5000" };
        var route = new TextBox { PlaceholderText = "Route", Text = "FileStorage" };
        var metadataRoute = new TextBox { PlaceholderText = "FileMetadata route (optional)" };
        var root = new TextBox { PlaceholderText = "Folder for file bytes" };
        var browse = new Button { Content = "Browse" };
        var metadata = new ComboBox { ItemsSource = new[] { "SQLite", "Postgres" }, SelectedIndex = 0 };
        var catalogDir = new TextBox { PlaceholderText = "Folder for the catalog file" };
        var browseCatalog = new Button { Content = "Browse" };
        var catalogName = new TextBox { PlaceholderText = "Filename" };
        var connection = new TextBox { PlaceholderText = "Postgres connection string" };
        var error = new TextBlock { Foreground = Avalonia.Media.Brushes.IndianRed, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var dbTouched = false;
        var fillingDatabase = false;
        var authKind = new ComboBox { ItemsSource = new[] { "None", "Bearer token", "API key" }, SelectedIndex = 0 };
        var authSecret = new TextBox { PlaceholderText = "Secret", PasswordChar = '•' };
        var auth = new Expander {
            Header = "Authentication",
            IsExpanded = false,
            Content = new StackPanel { Spacing = 8, Children = { PromptDialog.Labeled("Kind", authKind), PromptDialog.Labeled("Secret", authSecret) } }
        };
        var remote = PromptDialog.Stack(PromptDialog.Labeled("Base URL", baseUrl), PromptDialog.Labeled("Route", route), PromptDialog.Labeled("FileMetadata route", metadataRoute), auth);
        var folder = FieldRow(root, browse);
        var catalogFolder = PromptDialog.Labeled("Catalog folder", FieldRow(catalogDir, browseCatalog));
        var catalogFile = PromptDialog.Labeled("Filename", catalogName);
        var local = PromptDialog.Stack(
            PromptDialog.Labeled("File folder", folder),
            PromptDialog.Labeled("Database", metadata),
            catalogFolder,
            catalogFile,
            connection,
            error);
        var bucket = new TextBox { PlaceholderText = "Bucket" };
        var region = new TextBox { PlaceholderText = "Region (optional)" };
        var accessKey = new TextBox { PlaceholderText = "Access key (optional)" };
        var secretKey = new TextBox { PlaceholderText = "Secret key (optional)", PasswordChar = '•' };
        var profile = new TextBox { PlaceholderText = "AWS profile (optional)" };
        var serviceUrl = new TextBox { PlaceholderText = "Service URL (optional)" };
        var keyPrefix = new TextBox { PlaceholderText = "Key prefix (optional)" };
        var azureConnection = new TextBox { PlaceholderText = "Storage connection string" };
        var container = new TextBox { PlaceholderText = "Container" };
        var host = new TextBox { PlaceholderText = "Host" };
        var port = new TextBox { PlaceholderText = "Port (optional)" };
        var username = new TextBox { PlaceholderText = "Username" };
        var password = new TextBox { PlaceholderText = "Password", PasswordChar = '•' };
        var privateKey = new TextBox { PlaceholderText = "Private key path (SFTP)" };
        var fingerprints = new TextBox { PlaceholderText = "Host key fingerprints" };
        var remoteRoot = new TextBox { PlaceholderText = "Remote root", Text = "/" };
        var s3 = PromptDialog.Stack(
            PromptDialog.Labeled("Bucket", bucket), PromptDialog.Labeled("Region", region), PromptDialog.Labeled("Access key", accessKey),
            PromptDialog.Labeled("Secret key", secretKey), PromptDialog.Labeled("Profile", profile), PromptDialog.Labeled("Service URL", serviceUrl),
            PromptDialog.Labeled("Key prefix", keyPrefix));
        var azure = PromptDialog.Stack(PromptDialog.Labeled("Connection string", azureConnection), PromptDialog.Labeled("Container", container));
        var protocol = PromptDialog.Stack(
            PromptDialog.Labeled("Host", host), PromptDialog.Labeled("Port", port), PromptDialog.Labeled("Username", username),
            PromptDialog.Labeled("Password", password), PromptDialog.Labeled("Private key", privateKey), PromptDialog.Labeled("Fingerprints", fingerprints),
            PromptDialog.Labeled("Remote root", remoteRoot));
        var store = new StackPanel();
        connection.IsVisible = false;
        local.IsVisible = false;
        kind.SelectionChanged += (_, _) => {
            var index = kind.SelectedIndex;
            remote.IsVisible = index == 0;
            local.IsVisible = index > 0;
            folder.IsVisible = index == 1;
            s3.IsVisible = index == 2;
            azure.IsVisible = index == 3;
            protocol.IsVisible = index is 4 or 5;
            privateKey.IsVisible = index == 5;
            fingerprints.IsVisible = index == 5;
            store.Children.Clear();
            if (index == 2)
                store.Children.Add(s3);
            else if (index == 3)
                store.Children.Add(azure);
            else if (index is 4 or 5)
                store.Children.Add(protocol);
        };
        metadata.SelectionChanged += (_, _) => {
            var postgres = metadata.SelectedIndex == 1;
            catalogFolder.IsVisible = !postgres;
            catalogFile.IsVisible = !postgres;
            connection.IsVisible = postgres;
        };
        root.TextChanged += (_, _) => {
            if (dbTouched || string.IsNullOrWhiteSpace(root.Text))
                return;
            fillingDatabase = true;
            catalogDir.Text = FileManagerPaths.DefaultSqliteDirectory(root.Text);
            catalogName.Text = FileManagerPaths.DefaultSqliteFileName(root.Text);
            fillingDatabase = false;
        };
        catalogDir.TextChanged += (_, _) => {
            if (!fillingDatabase)
                dbTouched = true;
        };
        catalogName.TextChanged += (_, _) => {
            if (!fillingDatabase)
                dbTouched = true;
        };
        browse.Click += async (_, _) => {
            var picked = await owner.StorageProvider.OpenFolderPickerAsync(new() { Title = "File folder", AllowMultiple = false });
            var path = picked.Count > 0 ? picked[0].TryGetLocalPath() : null;
            if (!string.IsNullOrWhiteSpace(path))
                root.Text = path;
        };
        browseCatalog.Click += async (_, _) => {
            var picked = await owner.StorageProvider.OpenFolderPickerAsync(new() { Title = "Catalog folder", AllowMultiple = false });
            var path = picked.Count > 0 ? picked[0].TryGetLocalPath() : null;
            if (!string.IsNullOrWhiteSpace(path))
                catalogDir.Text = path;
        };
        var ok = false;
        string CatalogPath()
        {
            if (metadata.SelectedIndex == 1)
                return connection.Text ?? "";
            if (string.IsNullOrWhiteSpace(catalogDir.Text) || string.IsNullOrWhiteSpace(catalogName.Text))
                return "";
            return FileManagerPaths.CombineSqliteFile(catalogDir.Text, catalogName.Text);
        }
        var add = PromptDialog.Buttons(() => {
            var selected = KindAt(kind.SelectedIndex);
            var catalog = CatalogPath();
            var problem = FileManagerServerCheck.Validate(name.Text, selected, baseUrl.Text, root.Text);
            if (problem == null)
                problem = FileManagerServerCheck.ValidateStore(
                    selected,
                    bucket.Text,
                    azureConnection.Text,
                    container.Text,
                    host.Text,
                    username.Text,
                    password.Text,
                    privateKey.Text,
                    fingerprints.Text,
                    catalog,
                    metadata.SelectedIndex == 1);
            if (problem == null && selected == FileManagerServerKind.Local && metadata.SelectedIndex == 0
                && !string.IsNullOrWhiteSpace(root.Text) && !string.IsNullOrWhiteSpace(catalog))
                problem = FileManagerPaths.RejectDatabaseInsideRoot(root.Text, catalog);
            if (problem != null) {
                error.Text = problem;
                log(problem);
                return false;
            }

            ok = true;
            return true;
        }, "Add");
        add.Margin = new Avalonia.Thickness(12, 0, 12, 12);
        var form = PromptDialog.Stack(PromptDialog.Labeled("Name", name), PromptDialog.Labeled("Kind", kind), remote, store, local);
        var body = new DockPanel();
        DockPanel.SetDock(add, Dock.Bottom);
        body.Children.Add(add);
        body.Children.Add(new ScrollViewer { Content = form, MaxHeight = 640 });
        var window = new Window {
            Title = "Add server",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            MaxHeight = 720,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = body
        };
        PromptDialog.ApplyChrome(window);
        await window.ShowDialog(owner);
        if (!ok)
            return null;
        if (kind.SelectedIndex > 0) {
            var postgres = metadata.SelectedIndex == 1;
            var selected = KindAt(kind.SelectedIndex);
            return new FileManagerServer {
                Name = name.Text!.Trim(),
                Kind = selected,
                RootDirectory = selected == FileManagerServerKind.Local ? root.Text!.Trim() : remoteRoot.Text?.Trim() ?? "",
                MetadataKind = postgres ? FileManagerMetadataKind.Postgres : FileManagerMetadataKind.Sqlite,
                ConnectionString = postgres ? connection.Text?.Trim() : SqliteConnection(CatalogPath()),
                BucketName = Trim(bucket.Text),
                AwsRegion = Trim(region.Text),
                AwsAccessKeyId = Trim(accessKey.Text),
                AwsSecretAccessKey = secretKey.Text,
                AwsProfile = Trim(profile.Text),
                ServiceUrl = Trim(serviceUrl.Text),
                StorageKeyPrefix = Trim(keyPrefix.Text),
                AzureStorageConnectionString = Trim(azureConnection.Text),
                ContainerName = Trim(container.Text),
                Host = Trim(host.Text),
                Port = int.TryParse(port.Text, out var parsed) ? parsed : null,
                Username = Trim(username.Text),
                Password = password.Text,
                PrivateKeyPath = Trim(privateKey.Text),
                HostKeyFingerprints = Trim(fingerprints.Text)
            };
        }

        return new FileManagerServer {
            Name = name.Text!.Trim(),
            Kind = FileManagerServerKind.Remote,
            BaseUrl = baseUrl.Text!.Trim(),
            Route = string.IsNullOrWhiteSpace(route.Text) ? "FileStorage" : route.Text.Trim().Trim('/'),
            FileMetadataRoute = metadataRoute.Text?.Trim().Trim('/') ?? "",
            AuthKind = authKind.SelectedIndex switch {
                1 => FileManagerAuthKind.Bearer,
                2 => FileManagerAuthKind.ApiKey,
                _ => FileManagerAuthKind.None
            },
            Secret = authKind.SelectedIndex == 0 || string.IsNullOrWhiteSpace(authSecret.Text) ? null : authSecret.Text.Trim()
        };
    }

    public static async Task<(FileManagerAuthKind Kind, string? Secret)?> AuthenticateAsync(Window owner, FileManagerServer server)
    {
        var kind = new ComboBox { ItemsSource = new[] { "Bearer token", "API key" }, SelectedIndex = server.AuthKind == FileManagerAuthKind.ApiKey ? 1 : 0 };
        var secret = new TextBox { PlaceholderText = "Secret", PasswordChar = '•', Text = server.Secret };
        var note = new TextBlock { Text = "Saved on " + server.Name + " only.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, Opacity = 0.8 };
        var ok = false;
        var window = new Window {
            Title = "Authenticate " + server.Name,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = PromptDialog.Stack(note, kind, secret, PromptDialog.Buttons(() => ok = true, "Save"))
        };
        PromptDialog.ApplyChrome(window);
        await window.ShowDialog(owner);
        if (!ok)
            return null;
        return (kind.SelectedIndex == 1 ? FileManagerAuthKind.ApiKey : FileManagerAuthKind.Bearer, secret.Text);
    }

    public static async Task<FileManagerUploadOptions?> UploadOptionsAsync(Window owner, IReadOnlyList<string> keyIds, bool canEncrypt, Action<string> log)
    {
        var compress = new CheckBox { Content = "Compress" };
        var compression = new ComboBox { ItemsSource = new[] { "Brotli", "GZip", "Deflate", "ZLib" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 160 };
        var encrypt = new CheckBox { Content = "Encrypt", IsEnabled = canEncrypt };
        var encryption = new ComboBox { ItemsSource = new[] { "AesSiv", "AesGcm", "ChaCha20Poly1305" }, SelectedIndex = 0, IsEnabled = canEncrypt, HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 180 };
        var key = new TextBox { PlaceholderText = "Key id", Text = keyIds.FirstOrDefault(), IsEnabled = canEncrypt };
        var picker = new ComboBox { ItemsSource = keyIds, PlaceholderText = "Known keys", IsVisible = keyIds.Count > 0, IsEnabled = canEncrypt };
        picker.SelectionChanged += (_, _) => {
            if (picker.SelectedItem is string id)
                key.Text = id;
        };
        var charset = new TextBox { PlaceholderText = "Charset (optional)" };
        var error = new TextBlock { Foreground = Avalonia.Media.Brushes.IndianRed, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        const string noKey = "No key store is configured. Add one under Keys → Key store.";
        if (!canEncrypt)
            error.Text = noKey;
        var encryptHeader = new TextBlock { Text = "Encrypt", FontSize = 13 };
        ToolTip.SetTip(encryptHeader, canEncrypt ? null : noKey);
        ToolTip.SetShowOnDisabled(encryptHeader, true);
        var encryptTab = new TabItem {
            Header = encryptHeader,
            IsEnabled = canEncrypt,
            FontSize = 13,
            Padding = new Avalonia.Thickness(8, 2),
            Content = PromptDialog.Stack(encrypt, PromptDialog.Labeled("Method", encryption), picker, key)
        };
        ToolTip.SetTip(encryptTab, canEncrypt ? null : noKey);
        ToolTip.SetShowOnDisabled(encryptTab, true);
        var tabs = new TabControl {
            VerticalAlignment = VerticalAlignment.Top,
            MaxHeight = 280,
            FontSize = 13,
            Items = {
                new TabItem {
                    Header = new TextBlock { Text = "Compress", FontSize = 13 },
                    FontSize = 13,
                    Padding = new Avalonia.Thickness(8, 2),
                    Content = PromptDialog.Stack(compress, PromptDialog.Labeled("Method", compression))
                },
                encryptTab
            }
        };
        var ok = false;
        string? chosenKey = null;
        var window = new Window {
            Title = "Upload",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            MaxHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = PromptDialog.Stack(tabs, charset, error, PromptDialog.Buttons(() => {
                if (encrypt.IsChecked == true && !canEncrypt) {
                    error.Text = noKey;
                    log(noKey);
                    return false;
                }

                chosenKey = FileManagerKeys.Resolve(encrypt.IsChecked == true, key.Text, keyIds);
                if (encrypt.IsChecked == true && chosenKey == null) {
                    error.Text = keyIds.Count == 0 ? noKey : "Choose a key id.";
                    log(error.Text);
                    return false;
                }

                ok = true;
                return true;
            }, "Upload"))
        };
        PromptDialog.ApplyChrome(window);
        await window.ShowDialog(owner);
        if (!ok)
            return null;
        return new() {
            Compress = compress.IsChecked == true,
            CompressionAlgorithm = compression.SelectedItem as string,
            Encrypt = encrypt.IsChecked == true,
            EncryptionAlgorithm = encryption.SelectedItem as string,
            KeyId = chosenKey,
            Charset = string.IsNullOrWhiteSpace(charset.Text) ? null : charset.Text.Trim()
        };
    }

    public static async Task<List<FileManagerKeyStore>?> KeyStoresAsync(Window owner, IReadOnlyList<FileManagerKeyStore> stores)
    {
        var editing = stores.Select(CloneKeyStore).ToList();
        var list = new ListBox();
        var name = new TextBox();
        var kind = new ComboBox { ItemsSource = new[] { "Local", "AWS" }, SelectedIndex = 0 };
        var keys = new ListBox { Height = 140 };
        var stored = new List<FileManagerStoredKey>();
        var id = new TextBox { PlaceholderText = "Key id" };
        var version = new TextBox { PlaceholderText = "Version", Text = "1" };
        var secret = new TextBox { PlaceholderText = "Secret", PasswordChar = '•' };
        var passphrase = new TextBox { PlaceholderText = "Passphrase (optional)", PasswordChar = '•' };
        var addKey = new Button { Content = "Add version" };
        var removeKey = new Button { Content = "Remove" };
        var region = new TextBox { PlaceholderText = "us-east-1" };
        var prefix = new TextBox { PlaceholderText = "lyo/kek" };
        var access = new TextBox { PlaceholderText = "Access key (optional)" };
        var awsSecret = new TextBox { PlaceholderText = "Secret key", PasswordChar = '•' };
        var profiles = AwsSharedConfig.LoadDefault();
        var profileBox = new ComboBox {
            ItemsSource = profiles.Select(profile => profile.Name).ToArray(),
            PlaceholderText = profiles.Count == 0 ? "No ~/.aws profiles" : "AWS profile",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var current = -1;
        var loading = false;
        StackPanel local = null!;
        StackPanel aws = null!;
        void ShowKeys()
        {
            keys.ItemsSource = null;
            keys.ItemsSource = stored.Select(key => key.Id + "    version " + (string.IsNullOrWhiteSpace(key.Version) ? "1" : key.Version)).ToArray();
        }

        void ShowList(int select)
        {
            loading = true;
            list.ItemsSource = editing.Select(store => string.IsNullOrWhiteSpace(store.Name) ? "Key store" : store.Name).ToArray();
            if (select >= 0 && select < editing.Count)
                list.SelectedIndex = select;
            loading = false;
        }

        void Read(int index)
        {
            if (loading || index < 0 || index >= editing.Count)
                return;
            var store = editing[index];
            store.Name = string.IsNullOrWhiteSpace(name.Text) ? "Key store" : name.Text.Trim();
            store.Kind = kind.SelectedIndex == 1 ? FileManagerKeyStoreKind.Aws : FileManagerKeyStoreKind.Local;
            store.Passphrase = string.IsNullOrWhiteSpace(passphrase.Text) ? null : passphrase.Text;
            store.Keys = stored.Select(CopyStoredKey).ToList();
            store.AwsRegion = Trim(region.Text);
            store.AwsSecretPrefix = Trim(prefix.Text);
            store.AwsAccessKeyId = Trim(access.Text);
            store.AwsSecretAccessKey = awsSecret.Text;
            store.AwsProfile = profileBox.SelectedItem as string;
        }

        void Write(int index)
        {
            loading = true;
            var enabled = index >= 0 && index < editing.Count;
            name.IsEnabled = enabled;
            kind.IsEnabled = enabled;
            local.IsEnabled = enabled;
            aws.IsEnabled = enabled;
            if (!enabled) {
                loading = false;
                return;
            }

            var store = editing[index];
            name.Text = store.Name;
            kind.SelectedIndex = store.Kind == FileManagerKeyStoreKind.Aws ? 1 : 0;
            passphrase.Text = store.Passphrase;
            stored.Clear();
            if (store.Keys != null)
                stored.AddRange(store.Keys.Select(CopyStoredKey));
            ShowKeys();
            region.Text = store.AwsRegion;
            prefix.Text = store.AwsSecretPrefix;
            access.Text = store.AwsAccessKeyId;
            awsSecret.Text = store.AwsSecretAccessKey;
            profileBox.SelectedItem = store.AwsProfile;
            local.IsVisible = kind.SelectedIndex != 1;
            aws.IsVisible = kind.SelectedIndex == 1;
            loading = false;
        }

        addKey.Click += (_, _) => {
            if (string.IsNullOrWhiteSpace(id.Text) || string.IsNullOrWhiteSpace(secret.Text))
                return;
            stored.Add(new() { Id = id.Text.Trim(), Version = string.IsNullOrWhiteSpace(version.Text) ? "1" : version.Text.Trim(), Secret = secret.Text });
            id.Text = "";
            secret.Text = "";
            ShowKeys();
        };
        removeKey.Click += (_, _) => {
            if (keys.SelectedIndex < 0 || keys.SelectedIndex >= stored.Count)
                return;
            stored.RemoveAt(keys.SelectedIndex);
            ShowKeys();
        };
        profileBox.SelectionChanged += (_, _) => {
            if (loading)
                return;
            var profile = profiles.FirstOrDefault(item => item.Name == profileBox.SelectedItem as string);
            if (profile == null)
                return;
            if (!string.IsNullOrWhiteSpace(profile.Region))
                region.Text = profile.Region;
            if (!string.IsNullOrWhiteSpace(profile.AccessKeyId))
                access.Text = profile.AccessKeyId;
            if (!string.IsNullOrWhiteSpace(profile.SecretAccessKey))
                awsSecret.Text = profile.SecretAccessKey;
        };
        var keyActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { addKey, removeKey } };
        local = PromptDialog.Stack(
            PromptDialog.Labeled("Passphrase", passphrase),
            new TextBlock { Text = "Saved keys. Add another version with the same id to rotate.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, Opacity = 0.8 },
            keys,
            PromptDialog.Labeled("Key id", id),
            PromptDialog.Labeled("Version", version),
            PromptDialog.Labeled("Secret", secret),
            keyActions);
        local.Margin = new Avalonia.Thickness(0);
        aws = PromptDialog.Stack(
            PromptDialog.Labeled("Profile", profileBox),
            PromptDialog.Labeled("Region", region),
            PromptDialog.Labeled("Secret prefix", prefix),
            access,
            awsSecret);
        aws.Margin = new Avalonia.Thickness(0);
        kind.SelectionChanged += (_, _) => {
            if (loading)
                return;
            local.IsVisible = kind.SelectedIndex != 1;
            aws.IsVisible = kind.SelectedIndex == 1;
        };
        var add = new Button { Content = "Add" };
        var load = new Button { Content = "Load" };
        var remove = new Button { Content = "Remove" };
        var loadError = new TextBlock { Foreground = Avalonia.Media.Brushes.IndianRed, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        add.Click += (_, _) => {
            Read(current);
            editing.Add(new() { Name = NextKeyStoreName(editing) });
            current = editing.Count - 1;
            ShowList(current);
            Write(current);
            loadError.Text = "";
        };
        load.Click += async (_, _) => {
            try {
                var picked = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                    Title = "Local key store",
                    AllowMultiple = false,
                    FileTypeFilter = [new("JSON") { Patterns = ["*.json"] }, new("All files") { Patterns = ["*"] }]
                });
                var path = picked.Count > 0 ? picked[0].TryGetLocalPath() : null;
                if (string.IsNullOrWhiteSpace(path))
                    return;
                Read(current);
                var loaded = FileManagerKeyStores.ReadLocal(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
                var first = editing.Count;
                foreach (var store in loaded) {
                    if (editing.Exists(item => item.Id == store.Id))
                        store.Id = new FileManagerKeyStore().Id;
                    store.Name = UniqueKeyStoreName(editing, store.Name);
                    editing.Add(store);
                }

                current = first;
                ShowList(current);
                Write(current);
                loadError.Text = "";
            }
            catch (Exception ex) {
                loadError.Text = ex.Message;
            }
        };
        remove.Click += (_, _) => {
            if (current < 0 || current >= editing.Count)
                return;
            editing.RemoveAt(current);
            current = editing.Count == 0 ? -1 : Math.Min(current, editing.Count - 1);
            ShowList(current);
            Write(current);
        };
        list.SelectionChanged += (_, _) => {
            if (loading)
                return;
            Read(current);
            current = list.SelectedIndex;
            Write(current);
        };
        name.TextChanged += (_, _) => {
            if (loading || current < 0 || current >= editing.Count)
                return;
            editing[current].Name = name.Text ?? "";
            ShowList(current);
        };
        var commands = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Avalonia.Thickness(0, 8, 0, 0), Children = { add, load, remove } };
        var left = new DockPanel { Width = 200 };
        DockPanel.SetDock(commands, Dock.Bottom);
        left.Children.Add(commands);
        left.Children.Add(list);
        var editor = new StackPanel { Spacing = 8 };
        editor.Children.Add(PromptDialog.Labeled("Name", name));
        editor.Children.Add(PromptDialog.Labeled("Kind", kind));
        editor.Children.Add(local);
        editor.Children.Add(aws);
        var columns = new Grid {
            ColumnDefinitions = new ColumnDefinitions("200,*"),
            RowDefinitions = new RowDefinitions("*"),
            ColumnSpacing = 12
        };
        var editorScroll = new ScrollViewer { Content = editor };
        Grid.SetColumn(editorScroll, 1);
        columns.Children.Add(left);
        columns.Children.Add(editorScroll);
        var ok = false;
        var save = PromptDialog.Buttons(() => {
            Read(current);
            ok = true;
            return true;
        }, "Save");
        DockPanel.SetDock(save, Dock.Bottom);
        DockPanel.SetDock(loadError, Dock.Bottom);
        var root = new DockPanel { Margin = new Avalonia.Thickness(12) };
        root.Children.Add(save);
        root.Children.Add(loadError);
        root.Children.Add(columns);
        current = editing.Count == 0 ? -1 : 0;
        ShowList(current);
        Write(current);
        var window = new Window {
            Title = "Key stores",
            Width = 760,
            Height = 560,
            SizeToContent = SizeToContent.Manual,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = root
        };
        PromptDialog.ApplyChrome(window);
        await window.ShowDialog(owner);
        return ok ? editing : null;
    }

    public static async Task<FileManagerMoveCopyChoice?> MoveOrCopyAsync(
        Window owner,
        bool move,
        IReadOnlyList<FileManagerServer> destinations,
        IReadOnlyList<string> destinationNames,
        IReadOnlyList<string> localFolders,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> connectionFolders,
        Guid? connectionId = null)
    {
        var prefix = FolderBox("Destination prefix", localFolders);
        var names = destinationNames.Count == destinations.Count ? destinationNames : destinations.Select(server => server.Name).ToArray();
        var destination = new ComboBox { ItemsSource = names, SelectedIndex = destinations.Count == 0 ? -1 : 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        var keyMode = new ComboBox { ItemsSource = new[] { "Preserve keys", "New key", "Existing key" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        var keyId = new TextBox { PlaceholderText = "Key id", IsEnabled = false };
        var keyVersion = new TextBox { PlaceholderText = "Key version", IsEnabled = false };
        keyMode.SelectionChanged += (_, _) => {
            var needsKey = keyMode.SelectedIndex > 0;
            keyId.IsEnabled = needsKey;
            keyVersion.IsEnabled = needsKey;
        };
        var none = new TextBlock {
            Text = "Connect another in-process store.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            IsVisible = destinations.Count == 0,
            Opacity = 0.8
        };
        var error = new TextBlock { Foreground = Avalonia.Media.Brushes.IndianRed, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var ok = false;
        var onConnection = false;
        bool Accept()
        {
            if (!onConnection) {
                ok = true;
                return true;
            }

            if (destinations.Count == 0 || destination.SelectedIndex < 0) {
                error.Text = "Connect another in-process store.";
                return false;
            }

            if (keyMode.SelectedIndex > 0 && string.IsNullOrWhiteSpace(keyId.Text)) {
                error.Text = "Key id is required.";
                return false;
            }

            ok = true;
            return true;
        }

        var target = FolderBox("Target folder", []);
        void ShowTargetFolders()
        {
            if (destination.SelectedIndex < 0 || destination.SelectedIndex >= destinations.Count) {
                target.ItemsSource = Array.Empty<string>();
                return;
            }

            var id = destinations[destination.SelectedIndex].Id;
            target.ItemsSource = connectionFolders.TryGetValue(id, out var list) ? list : Array.Empty<string>();
        }
        var action = PromptDialog.Buttons(Accept, move ? "Move" : "Copy");
        var connectionFields = new Grid {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 8,
            RowSpacing = 8,
            IsVisible = destinations.Count > 0
        };
        Place(connectionFields, PromptDialog.Labeled("Destination", destination), 0, 0, 2);
        Place(connectionFields, PromptDialog.Labeled("Keys", keyMode), 0, 2, 2);
        Place(connectionFields, PromptDialog.Labeled("Key id", keyId), 1, 0, 2);
        Place(connectionFields, PromptDialog.Labeled("Key version", keyVersion), 1, 2, 2);
        Place(connectionFields, PromptDialog.Labeled("Target", target), 2, 0, 3);
        Place(connectionFields, PromptDialog.Labeled(" ", action), 2, 3);
        destination.SelectionChanged += (_, _) => ShowTargetFolders();
        ShowTargetFolders();
        var tabs = new TabControl {
            VerticalAlignment = VerticalAlignment.Top,
            FontSize = 13,
            Items = {
                new TabItem {
                    Header = new TextBlock { Text = "Local", FontSize = 13 },
                    FontSize = 13,
                    Padding = new Avalonia.Thickness(8, 2),
                    Content = PromptDialog.Stack(PromptDialog.Labeled("Destination prefix", prefix))
                },
                new TabItem {
                    Header = new TextBlock { Text = "Other connection", FontSize = 13 },
                    FontSize = 13,
                    Padding = new Avalonia.Thickness(8, 2),
                    Content = PromptDialog.Stack(none, connectionFields)
                }
            }
        };
        var localAction = PromptDialog.Buttons(Accept, move ? "Move" : "Copy");
        tabs.SelectionChanged += (_, _) => {
            onConnection = tabs.SelectedIndex != 0;
            localAction.IsVisible = !onConnection;
        };
        if (connectionId is Guid id) {
            var index = 0;
            foreach (var server in destinations) {
                if (server.Id == id) {
                    destination.SelectedIndex = index;
                    break;
                }

                index++;
            }

            tabs.SelectedIndex = 1;
            onConnection = true;
            localAction.IsVisible = false;
            ShowTargetFolders();
        }

        var window = new Window {
            Title = move ? "Move" : "Copy",
            Width = 640,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = PromptDialog.Stack(tabs, error, localAction)
        };
        PromptDialog.ApplyChrome(window);
        await window.ShowDialog(owner);
        if (!ok)
            return null;
        if (!onConnection)
            return new(false, prefix.Text ?? "", Guid.Empty, FileStorageKeyMode.PreserveKeys, null, null);
        var mode = keyMode.SelectedIndex switch {
            1 => FileStorageKeyMode.NewKey,
            2 => FileStorageKeyMode.ExistingKey,
            _ => FileStorageKeyMode.PreserveKeys
        };
        return new(
            true,
            target.Text ?? "",
            destinations[destination.SelectedIndex].Id,
            mode,
            string.IsNullOrWhiteSpace(keyId.Text) ? null : keyId.Text.Trim(),
            string.IsNullOrWhiteSpace(keyVersion.Text) ? null : keyVersion.Text.Trim());
    }

    private static FileManagerStoredKey CopyStoredKey(FileManagerStoredKey key)
        => new() { Id = key.Id, Version = key.Version, Secret = key.Secret, RawKey = key.RawKey, Current = key.Current, Metadata = key.Metadata };

    private static FileManagerKeyStore CloneKeyStore(FileManagerKeyStore store)
        => new() {
            Id = store.Id,
            Name = store.Name,
            Kind = store.Kind,
            Passphrase = store.Passphrase,
            Keys = store.Keys?.Select(CopyStoredKey).ToList() ?? [],
            AwsRegion = store.AwsRegion,
            AwsSecretPrefix = store.AwsSecretPrefix,
            AwsAccessKeyId = store.AwsAccessKeyId,
            AwsSecretAccessKey = store.AwsSecretAccessKey,
            AwsProfile = store.AwsProfile
        };

    private static string UniqueKeyStoreName(List<FileManagerKeyStore> stores, string name)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? "Key store" : name.Trim();
        if (!stores.Exists(store => string.Equals(store.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            return trimmed;
        var number = 2;
        while (stores.Exists(store => string.Equals(store.Name, trimmed + " " + number, StringComparison.OrdinalIgnoreCase)))
            number++;
        return trimmed + " " + number;
    }

    private static string NextKeyStoreName(List<FileManagerKeyStore> stores)
    {
        var name = "Key store";
        var number = 2;
        while (stores.Exists(store => string.Equals(store.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = "Key store " + number++;
        return name;
    }

    private static AutoCompleteBox FolderBox(string watermark, IEnumerable<string> folders)
    {
        var box = new AutoCompleteBox {
            PlaceholderText = watermark,
            ItemsSource = folders.ToArray(),
            FilterMode = AutoCompleteFilterMode.Custom,
            MinimumPrefixLength = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        box.ItemFilter = (search, item) => FileManagerFolders.Matches(item as string ?? "", search ?? "");
        return box;
    }

    private static void Place(Grid grid, Control child, int row, int column, int columnSpan = 1)
    {
        Grid.SetRow(child, row);
        Grid.SetColumn(child, column);
        Grid.SetColumnSpan(child, columnSpan);
        grid.Children.Add(child);
    }

    private static Grid FieldRow(Control field, Button browse)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        Grid.SetColumn(browse, 1);
        browse.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(field);
        row.Children.Add(browse);
        return row;
    }

    private static FileManagerServerKind KindAt(int index) => index switch {
        1 => FileManagerServerKind.Local,
        2 => FileManagerServerKind.S3,
        3 => FileManagerServerKind.AzureBlob,
        4 => FileManagerServerKind.Ftp,
        5 => FileManagerServerKind.Sftp,
        _ => FileManagerServerKind.Remote
    };

    private static string? Trim(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static string? SqliteConnection(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var trimmed = text.Trim();
        return trimmed.Contains('=') ? trimmed : "Data Source=" + trimmed;
    }

}
