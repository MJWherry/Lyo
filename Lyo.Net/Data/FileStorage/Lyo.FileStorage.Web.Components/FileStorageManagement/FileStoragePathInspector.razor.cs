using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.IO.FileSystem;
using Lyo.IO.Temp.Models;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Tree-tab inspector: directory upload / new folder, or file metadata and actions.</summary>
public partial class FileStoragePathInspector : IAsyncDisposable
{
    private bool _fileBusy;
    private LyoFileUpload? _fileUpload;
    private Guid? _loadedFileId;
    private FileStoreResult? _metadata;
    private bool _metadataBusy;
    private string _metadataStatus = string.Empty;
    private string _newFolderName = string.Empty;
    private IReadOnlyList<FileSystemEntry> _contents = [];
    private bool _contentsBusy;
    private string? _contentsPath;
    private int _loadedListingRevision = -1;
    private StagedUpload? _selectedStaged;
    private readonly List<StagedUpload> _staged = [];
    private IIOTempSession? _stagingSession;
    private string _uploadStatus = "No file selected.";

    /// <summary>Cascaded parent (API, temp sessions, snackbar).</summary>
    [CascadingParameter]
    public FileStorageManagement Host { get; set; } = default!;

    /// <summary>Shared mutation handlers.</summary>
    [Parameter]
    [EditorRequired]
    public FileStorageBrowserActions Actions { get; set; } = default!;

    /// <summary>Folder-list source. Contents and breadcrumbs do not walk a detached tree <c>Root</c>.</summary>
    [Parameter]
    [EditorRequired]
    public IFileTreeSource FolderSource { get; set; } = default!;

    /// <summary>Increment to reload directory contents after the left-hand tree refreshes.</summary>
    [Parameter]
    public int ListingRevision { get; set; }

    /// <summary>Currently selected path.</summary>
    [Parameter]
    public FileStoragePathTreeNode? Selected { get; set; }

    /// <summary>Raised when the inspector selects another tree node (breadcrumb or contents list).</summary>
    [Parameter]
    public EventCallback<FileStoragePathTreeNode> SelectedChanged { get; set; }

    /// <summary>Raised after a pending folder is added so the tree can rebuild.</summary>
    [Parameter]
    public EventCallback TreeChanged { get; set; }

    private IReadOnlyList<(string Label, string? Prefix)> Breadcrumbs
    {
        get {
            if (Selected is not { IsDirectory: true })
                return [];

            List<(string Label, string? Prefix)> crumbs = [(FileStoragePathTreeBuilder.RootDisplayName, null)];
            foreach (var segment in FileStoragePathTreeBuilder.SplitSegments(Selected.PathPrefix)) {
                var prefix = crumbs[^1].Prefix == null ? segment : $"{crumbs[^1].Prefix}/{segment}";
                crumbs.Add((segment, prefix));
            }

            return crumbs;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_stagingSession is null)
            return;

        await _stagingSession.DisposeAsync();
        _stagingSession = null;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => EnsureStagingSession();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Selected is { IsDirectory: true }) {
            _loadedFileId = null;
            _metadata = null;
            _metadataStatus = string.Empty;
            await LoadContentsAsync(FileStorageHttpTreeSource.DirectoryVfsPath(Selected.PathPrefix)).ConfigureAwait(true);
            return;
        }

        _contents = [];
        _contentsPath = null;
        _loadedListingRevision = -1;

        if (Selected is not { IsDirectory: false } file) {
            _loadedFileId = null;
            _metadata = null;
            _metadataStatus = string.Empty;
            return;
        }

        if (file.Presence == FileStoragePresence.Physical) {
            _loadedFileId = null;
            _metadata = null;
            _metadataStatus = "This object is on disk only (not in the catalog). Download is the raw physical file — it is not decrypted or decompressed.";
            return;
        }

        if (file.FileId is not { } fileId) {
            _loadedFileId = null;
            _metadata = null;
            _metadataStatus = string.Empty;
            return;
        }

        if (_loadedFileId == fileId)
            return;

        _loadedFileId = fileId;
        await LoadMetadataAsync(fileId).ConfigureAwait(true);
    }

    private async Task LoadContentsAsync(string path)
    {
        if (_contentsPath == path && _loadedListingRevision == ListingRevision)
            return;

        _contentsBusy = true;
        _contentsPath = path;
        _loadedListingRevision = ListingRevision;
        try {
            _contents = await FolderSource.ListChildrenAsync(path).ConfigureAwait(true);
        }
        catch (Exception ex) {
            _contents = [];
            Host.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _contentsBusy = false;
        }
    }

    private void EnsureStagingSession()
    {
        if (_stagingSession != null)
            return;

        _stagingSession = Host.TempService.CreateSession();
    }

    private async Task LoadMetadataAsync(Guid fileId)
    {
        _metadataBusy = true;
        _metadataStatus = string.Empty;
        try {
            _metadata = await Actions.GetMetadataAsync(fileId).ConfigureAwait(true);
            if (_metadata == null)
                _metadataStatus = $"Metadata for {fileId} was not returned.";
        }
        catch (Exception ex) {
            _metadata = null;
            _metadataStatus = ex.Message;
            Host.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _metadataBusy = false;
        }
    }

    private Task SelectEntryAsync(FileSystemEntry entry)
        => SelectedChanged.InvokeAsync(FileStorageHttpTreeSource.ToPathNode(entry));

    private Task SelectPrefixAsync(string? prefix)
        => SelectedChanged.InvokeAsync(FileStorageHttpTreeSource.DirectoryNode(prefix));

    private async Task AddFolderAsync()
    {
        if (Selected is not { IsDirectory: true })
            return;

        if (string.IsNullOrWhiteSpace(_newFolderName)) {
            Host.SetStatus("Enter a folder name.", Severity.Warning);
            return;
        }

        try {
            var created = FileStoragePathTreeBuilder.AddPendingFolder(Selected, _newFolderName);
            _newFolderName = string.Empty;
            await TreeChanged.InvokeAsync();
            await SelectedChanged.InvokeAsync(created);
        }
        catch (Exception ex) {
            Host.SetStatus(ex.Message, Severity.Warning);
        }
    }

    private async Task OnClientFilePathReadyAsync(LocalBrowserFilePath file)
    {
        var staged = new StagedUpload {
            File = file,
            OriginalFileName = file.FileName
        };
        _staged.Add(staged);
        _selectedStaged = staged;
        _uploadStatus = $"{file.FileName} staged — select a chip, then click Upload.";
        await InvokeAsync(StateHasChanged);
    }

    private Task OnClientFilePathRemovedAsync(LocalBrowserFilePath file)
    {
        _staged.RemoveAll(s => ReferenceEquals(s.File, file));
        if (_selectedStaged != null && ReferenceEquals(_selectedStaged.File, file))
            _selectedStaged = _staged.Count > 0 ? _staged[^1] : null;

        _uploadStatus = _staged.Count == 0 ? "No file selected." : $"{file.FileName} removed.";
        return Task.CompletedTask;
    }

    private void SelectStaged(StagedUpload staged) => _selectedStaged = staged;

    private Task RemoveStagedAsync(StagedUpload staged)
        => _fileUpload?.RemoveClientFilePathAsync(staged.File) ?? Task.CompletedTask;

    private Task OnUploadStartedAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"Uploading {args.FileName}...";
        return Task.CompletedTask;
    }

    private Task OnUploadProgressAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName}: {args.Progress:F0}%";
        return Task.CompletedTask;
    }

    private Task OnUploadCompletedAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName} ready.";
        return Task.CompletedTask;
    }

    private Task OnUploadCancelledAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName} cancelled.";
        return Task.CompletedTask;
    }

    private Task OnUploadFailedAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName} failed: {args.ErrorMessage}";
        return Task.CompletedTask;
    }

    private async Task SaveFilesAsync()
    {
        if (_staged.Count == 0) {
            Host.SetStatus("Choose a file first.", Severity.Warning);
            _uploadStatus = "No file selected.";
            return;
        }

        foreach (var staged in _staged) {
            if (!staged.Encrypt || !string.IsNullOrWhiteSpace(staged.KeyId))
                continue;

            _selectedStaged = staged;
            Host.SetStatus($"Key id is required for {staged.OriginalFileName}.", Severity.Warning);
            _uploadStatus = $"Enter a key id for {staged.OriginalFileName}.";
            return;
        }

        _fileBusy = true;
        var toUpload = _staged.ToList();
        var uploaded = 0;
        var failed = 0;
        try {
            var pathPrefix = Selected?.IsDirectory == true ? Selected.PathPrefix : null;
            var total = toUpload.Count;
            foreach (var staged in toUpload) {
                _uploadStatus = $"Uploading {staged.OriginalFileName} ({uploaded + failed + 1}/{total})…";
                await InvokeAsync(StateHasChanged);
                try {
                    var originalName = string.IsNullOrWhiteSpace(staged.OriginalFileName) ? staged.File.FileName : staged.OriginalFileName;
                    var uri = Host.BuildSaveStreamUri(
                        originalName, staged.Compress, staged.Encrypt, staged.Encrypt ? NullIfWhiteSpace(staged.KeyId) : null, pathPrefix, chunkSize: null);
                    await Host.ApiClient.PostFileAsAsync<FileStoreResult>(uri, staged.File.FilePath).ConfigureAwait(true);
                    uploaded++;
                    await InvokeAsync(() => RemoveStagedAsync(staged)).ConfigureAwait(true);
                }
                catch (Exception ex) {
                    failed++;
                    Host.SetStatus($"{staged.OriginalFileName}: {ex.Message}", Severity.Error);
                }
            }

            if (uploaded > 0)
                await InvokeAsync(() => Host.NotifyFilesChangedAsync()).ConfigureAwait(true);

            _uploadStatus = failed == 0 ? $"Uploaded {uploaded} file(s)." : $"Uploaded {uploaded} file(s); {failed} failed.";
            Host.SetStatus(_uploadStatus, failed == 0 ? Severity.Success : Severity.Warning);
        }
        finally {
            _fileBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool InCatalog
        => Selected is { FileId: not null } && Selected.Presence != FileStoragePresence.Physical;

    private bool CanDownload
        => Selected is { IsDeleted: false } && (InCatalog || !string.IsNullOrEmpty(Selected.PhysicalKey));

    private bool CatalogActionsEnabled => InCatalog && Selected is { IsDeleted: false };

    private Task ViewMetadataAsync()
        => InCatalog && Selected?.FileId is { } id ? Actions.ViewAsync(id) : Task.CompletedTask;

    private Task AccessLinkAsync()
        => CatalogActionsEnabled && Selected?.FileId is { } id ? Actions.AccessLinkAsync(id, Selected.Name) : Task.CompletedTask;

    private Task DownloadAsync()
    {
        if (Selected is { IsDeleted: true })
            return Task.CompletedTask;

        if (InCatalog && Selected?.FileId is { } id)
            return Actions.DownloadAsync(id);

        return !string.IsNullOrEmpty(Selected?.PhysicalKey)
            ? Actions.DownloadPhysicalAsync(Selected.PhysicalKey)
            : Task.CompletedTask;
    }

    private Task MoveAsync()
        => CatalogActionsEnabled && Selected?.FileId is { } id ? Actions.MoveAsync(id, Selected.PathPrefix) : Task.CompletedTask;

    private Task CopyAsync()
        => CatalogActionsEnabled && Selected?.FileId is { } id ? Actions.CopyAsync(id, Selected.PathPrefix) : Task.CompletedTask;

    private Task RenameAsync()
        => CatalogActionsEnabled && Selected?.FileId is { } id ? Actions.RenameAsync(id, Selected.Name) : Task.CompletedTask;

    private Task RotateDekAsync()
        => CatalogActionsEnabled && Selected?.FileId is { } id ? Actions.RotateDekAsync(id) : Task.CompletedTask;

    private Task DeleteAsync()
        => CatalogActionsEnabled && Selected?.FileId is { } id ? Actions.DeleteAsync(id) : Task.CompletedTask;

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class StagedUpload
    {
        public required LocalBrowserFilePath File { get; init; }

        public string OriginalFileName { get; set; } = string.Empty;

        public bool Compress { get; set; }

        public bool Encrypt { get; set; }

        public string KeyId { get; set; } = string.Empty;
    }
}
