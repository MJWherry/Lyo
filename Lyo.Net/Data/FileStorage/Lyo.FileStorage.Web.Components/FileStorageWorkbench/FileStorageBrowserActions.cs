using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.Web.Components.Dialog;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>Shared row and bulk actions for the file storage Browser grids (view, download, move, copy, rename, rotate DEK, delete).</summary>
public sealed class FileStorageBrowserActions
{
    private readonly Func<Task> _onMutated;
    private readonly FileStorageWorkbench _workbench;

    /// <summary>Creates actions bound to a workbench host. <paramref name="onMutated" /> runs after successful mutations so grids can refresh.</summary>
    public FileStorageBrowserActions(FileStorageWorkbench workbench, Func<Task> onMutated)
    {
        _workbench = ArgumentHelpers.ThrowIfNullReturn(workbench);
        _onMutated = ArgumentHelpers.ThrowIfNullReturn(onMutated);
    }

    /// <summary>Loads metadata (including tombstones) and opens the read-only metadata dialog.</summary>
    public async Task ViewFromRowAsync(object? row)
    {
        var storage = RequireStorage();
        if (storage == null)
            return;

        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var fileId)) {
            _workbench.SetStatus("Could not read file id from the grid row.", Severity.Warning);
            return;
        }

        try {
            FileStoreResult result;
            if (FileStorageGridRowHelper.IsRowDeleted(row)) {
                var uri = $"{_workbench.FileStorageApiRoutePrefix.TrimEnd('/')}/files/{fileId:D}/metadata?includeDeleted=true";
                result = await _workbench.ApiClient.GetAsAsync<FileStoreResult>(uri).ConfigureAwait(false) ??
                    throw new InvalidOperationException($"Metadata for deleted file {fileId} was not returned.");
            }
            else
                result = await storage.GetMetadataAsync(fileId).ConfigureAwait(false);

            var parameters = new DialogParameters<FileStoreMetadataDialog> { { d => d.Metadata, result } };
            await _workbench.DialogService.ShowAsync<FileStoreMetadataDialog>("File metadata", parameters, LyoDialogPresets.Medium);
            _workbench.SetStatus($"Loaded metadata for {fileId}.", Severity.Success);
        }
        catch (Exception ex) {
            _workbench.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Opens the access-link dialog for an active file.</summary>
    public async Task AccessLinkFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "Access links cannot be created for deleted files.", out var fileId))
            return;

        var parameters = new DialogParameters<FileStoreAccessLinkDialog> { { d => d.Workbench, _workbench }, { d => d.FileId, fileId } };
        await _workbench.DialogService.ShowAsync<FileStoreAccessLinkDialog>("Create access link", parameters, LyoDialogPresets.Medium);
    }

    /// <summary>Downloads via presigned URL when the object is plaintext; otherwise opens the host download proxy.</summary>
    public async Task DownloadFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "Deleted files cannot be downloaded; the backing object was removed.", out var fileId))
            return;

        var storage = RequireStorage();
        if (storage == null)
            return;

        try {
            var meta = await storage.GetMetadataAsync(fileId);
            if (!meta.IsEncrypted && !meta.IsCompressed) {
                var url = await storage.GetPreSignedReadUrlAsync(fileId);
                await _workbench.JsRuntime.InvokeVoidAsync("open", url, "_blank");
                _workbench.SetStatus($"Opened presigned download for {fileId}.", Severity.Success);
            }
            else {
                var url = _workbench.NavigationManager.ToAbsoluteUri($"/{_workbench.ProxyDownloadPath}/{fileId:D}").AbsoluteUri;
                await _workbench.JsRuntime.InvokeVoidAsync("open", url, "_blank");
                _workbench.SetStatus($"Started download for {fileId}.", Severity.Success);
            }
        }
        catch (Exception ex) {
            _workbench.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Confirms and tombstone-deletes one file.</summary>
    public async Task DeleteFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "This file is already deleted.", out var fileId))
            return;

        await DeleteFilesAsync([fileId]);
    }

    /// <summary>Moves one file after prompting for a path prefix.</summary>
    public async Task MoveFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "Deleted files cannot be moved.", out var fileId))
            return;

        var parameters = new DialogParameters<FileStoreMoveDialog> { { d => d.InitialPathPrefix, FileStorageGridRowHelper.GetPathPrefixFromRow(row) } };
        var dialog = await _workbench.DialogService.ShowAsync<FileStoreMoveDialog>("Move file", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        await MoveFilesAsync([fileId], prefix.PathPrefix);
    }

    /// <summary>Copies one file after prompting for an optional path prefix.</summary>
    public async Task CopyFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "Deleted files cannot be copied.", out var fileId))
            return;

        var storage = RequireStorage();
        if (storage == null)
            return;

        var parameters = new DialogParameters<FileStoreCopyDialog> { { d => d.InitialPathPrefix, FileStorageGridRowHelper.GetPathPrefixFromRow(row) } };
        var dialog = await _workbench.DialogService.ShowAsync<FileStoreCopyDialog>("Copy file", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        try {
            var copied = await storage.CopyFileAsync(fileId, new CopyFileRequest { PathPrefix = prefix.PathPrefix });
            _workbench.SetStatus($"Copied to {copied.Id}.", Severity.Success);
            await NotifyMutatedAsync();
        }
        catch (Exception ex) {
            _workbench.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Renames the display name for one file.</summary>
    public async Task RenameFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "Deleted files cannot be renamed.", out var fileId))
            return;

        var storage = RequireStorage();
        if (storage == null)
            return;

        var parameters = new DialogParameters<FileStoreRenameDialog> { { d => d.InitialOriginalFileName, FileStorageGridRowHelper.GetOriginalFileNameFromRow(row) } };
        var dialog = await _workbench.DialogService.ShowAsync<FileStoreRenameDialog>("Rename file", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStoreRenameDialogResult rename })
            return;

        try {
            await storage.RenameFileAsync(fileId, new RenameFileRequest { OriginalFileName = rename.OriginalFileName });
            _workbench.SetStatus($"Renamed file {fileId}.", Severity.Success);
            await NotifyMutatedAsync();
        }
        catch (Exception ex) {
            _workbench.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Rotates the DEK for one file after prompting for target key options.</summary>
    public async Task RotateDekFromRowAsync(object? row)
    {
        if (!TryGetActiveFileId(row, "Deleted files cannot have DEKs rotated.", out var fileId))
            return;

        await RotateDeksAsync([fileId]);
    }

    /// <summary>Copies the expected storage key for the row to the clipboard.</summary>
    public async Task CopyExpectedKeyFromRowAsync(object? row)
    {
        var key = FileStorageGridRowHelper.GetExpectedStorageKey(row);
        if (string.IsNullOrWhiteSpace(key)) {
            _workbench.SetStatus("Could not build an expected storage key from the grid row.", Severity.Warning);
            return;
        }

        try {
            await _workbench.Js.SendToClipboard(key);
            _workbench.SetStatus("Copied expected storage key.", Severity.Success);
        }
        catch (Exception ex) {
            _workbench.SetStatus($"Copy failed: {ex.Message}", Severity.Warning);
        }
    }

    /// <summary>Moves every selected active file to the same path prefix.</summary>
    public async Task BulkMoveAsync(IReadOnlyList<object?> rows)
    {
        var ids = GetActiveFileIds(rows);
        if (ids.Count == 0) {
            _workbench.SetStatus("Select at least one active file to move.", Severity.Warning);
            return;
        }

        var dialog = await _workbench.DialogService.ShowAsync<FileStoreMoveDialog>("Move files", new DialogParameters(), LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        await MoveFilesAsync(ids, prefix.PathPrefix);
    }

    /// <summary>Deletes every selected active file after confirmation.</summary>
    public async Task BulkDeleteAsync(IReadOnlyList<object?> rows)
    {
        var ids = GetActiveFileIds(rows);
        if (ids.Count == 0) {
            _workbench.SetStatus("Select at least one active file to delete.", Severity.Warning);
            return;
        }

        await DeleteFilesAsync(ids);
    }

    /// <summary>Rotates DEKs for every selected active file.</summary>
    public async Task BulkRotateDeksAsync(IReadOnlyList<object?> rows)
    {
        var ids = GetActiveFileIds(rows);
        if (ids.Count == 0) {
            _workbench.SetStatus("Select at least one active file to rotate.", Severity.Warning);
            return;
        }

        await RotateDeksAsync(ids);
    }

    private async Task MoveFilesAsync(IReadOnlyList<Guid> fileIds, string? pathPrefix)
    {
        var storage = RequireStorage();
        if (storage == null)
            return;

        var request = new MoveFileRequest { PathPrefix = pathPrefix };
        var failed = 0;
        foreach (var fileId in fileIds) {
            try {
                await storage.MoveFileAsync(fileId, request);
            }
            catch (Exception ex) {
                failed++;
                _workbench.SetStatus($"Move failed for {fileId}: {ex.Message}", Severity.Error);
            }
        }

        var moved = fileIds.Count - failed;
        if (moved > 0)
            _workbench.SetStatus(failed == 0 ? $"Moved {moved} file(s)." : $"Moved {moved} file(s); {failed} failed.", failed == 0 ? Severity.Success : Severity.Warning);

        if (moved > 0)
            await NotifyMutatedAsync();
    }

    private async Task DeleteFilesAsync(IReadOnlyList<Guid> fileIds)
    {
        var storage = RequireStorage();
        if (storage == null)
            return;

        var label = fileIds.Count == 1 ? $"Delete file {fileIds[0]}?" : $"Delete {fileIds.Count} files?";
        var confirm = await _workbench.DialogService.ShowMessageBoxAsync("Delete file", label, "Delete", cancelText: "Cancel");
        if (confirm != true)
            return;

        var failed = 0;
        foreach (var fileId in fileIds) {
            try {
                var deleted = await storage.DeleteFileAsync(fileId);
                if (!deleted)
                    failed++;
            }
            catch (Exception ex) {
                failed++;
                _workbench.SetStatus($"Delete failed for {fileId}: {ex.Message}", Severity.Error);
            }
        }

        var deletedCount = fileIds.Count - failed;
        if (deletedCount > 0)
            _workbench.SetStatus(
                failed == 0 ? $"Deleted {deletedCount} file(s)." : $"Deleted {deletedCount} file(s); {failed} failed.", failed == 0 ? Severity.Success : Severity.Warning);
        else
            _workbench.SetStatus("No files were deleted.", Severity.Warning);

        if (deletedCount > 0)
            await NotifyMutatedAsync();
    }

    private async Task RotateDeksAsync(IReadOnlyList<Guid> fileIds)
    {
        var storage = RequireStorage();
        if (storage == null)
            return;

        var parameters = new DialogParameters<FileStoreRotateDekDialog> { { d => d.Workbench, _workbench }, { d => d.FileCount, fileIds.Count } };
        var dialog = await _workbench.DialogService.ShowAsync<FileStoreRotateDekDialog>("Rotate DEKs", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStoreRotateDekDialogResult rotate })
            return;

        try {
            var rotation = await storage.RotateDeksAsync(fileIds, rotate.TargetKeyId, rotate.TargetKeyVersion, rotate.BatchSize);
            _workbench.SetStatus(
                rotation.AllSucceeded ? "DEK rotation completed." : "DEK rotation completed with failures.", rotation.AllSucceeded ? Severity.Success : Severity.Warning);
            await NotifyMutatedAsync();
        }
        catch (Exception ex) {
            _workbench.SetStatus(ex.Message, Severity.Error);
        }
    }

    private IFileStorageService? RequireStorage()
    {
        if (_workbench.FileStorage != null)
            return _workbench.FileStorage;

        _workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
        return null;
    }

    private bool TryGetActiveFileId(object? row, string deletedMessage, out Guid fileId)
    {
        fileId = default;
        if (FileStorageGridRowHelper.IsRowDeleted(row)) {
            _workbench.SetStatus(deletedMessage, Severity.Info);
            return false;
        }

        if (FileStorageGridRowHelper.TryGetFileIdFromRow(row, out fileId))
            return true;

        _workbench.SetStatus("Could not read file id from the grid row.", Severity.Warning);
        return false;
    }

    private static IReadOnlyList<Guid> GetActiveFileIds(IReadOnlyList<object?> rows)
    {
        var ids = new List<Guid>();
        foreach (var row in rows) {
            if (FileStorageGridRowHelper.IsRowDeleted(row))
                continue;

            if (FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var id))
                ids.Add(id);
        }

        return ids;
    }

    private Task NotifyMutatedAsync() => _onMutated();
}
