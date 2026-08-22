using Lyo.FileMetadataStore.Models;
using Lyo.IO.Temp.Models;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Right-hand inspector for the Tree tab: directory upload / new folder, or file metadata and actions.</summary>
public partial class FileStoragePathInspector : IAsyncDisposable
{
    private bool _fileBusy;
    private LyoFileUpload? _fileUpload;
    private Guid? _loadedFileId;
    private FileStoreResult? _metadata;
    private bool _metadataBusy;
    private string _metadataStatus = string.Empty;
    private string _newFolderName = string.Empty;
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

    /// <summary>Tree root, used to resolve breadcrumb prefixes.</summary>
    [Parameter]
    [EditorRequired]
    public FileStoragePathTreeNode Root { get; set; } = default!;

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
        if (Selected is not { IsDirectory: false, FileId: { } fileId }) {
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

    private Task SelectNodeAsync(FileStoragePathTreeNode node) => SelectedChanged.InvokeAsync(node);

    private Task SelectPrefixAsync(string? prefix)
    {
        var node = FileStoragePathTreeBuilder.FindDirectory(Root, prefix);
        return node == null ? Task.CompletedTask : SelectedChanged.InvokeAsync(node);
    }

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

    private Task ViewMetadataAsync()
        => Selected?.FileId is { } id ? Actions.ViewAsync(id) : Task.CompletedTask;

    private Task AccessLinkAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.AccessLinkAsync(id, Selected.Name) : Task.CompletedTask;

    private Task DownloadAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.DownloadAsync(id) : Task.CompletedTask;

    private Task MoveAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.MoveAsync(id, Selected.PathPrefix) : Task.CompletedTask;

    private Task CopyAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.CopyAsync(id, Selected.PathPrefix) : Task.CompletedTask;

    private Task RenameAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.RenameAsync(id, Selected.Name) : Task.CompletedTask;

    private Task RotateDekAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.RotateDekAsync(id) : Task.CompletedTask;

    private Task DeleteAsync()
        => Selected?.FileId is { } id && !Selected.IsDeleted ? Actions.DeleteAsync(id) : Task.CompletedTask;

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
