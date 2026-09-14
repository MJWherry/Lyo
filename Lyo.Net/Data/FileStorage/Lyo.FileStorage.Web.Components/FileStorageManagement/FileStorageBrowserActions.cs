using Lyo.Api.FileStorage.Models;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Web.Components.FileAccessLink;
using Lyo.Web.Components.Dialog;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Shared Browser-grid and Tree-inspector handlers (view, download, zip archive, move, copy, rename, rotate DEK, delete).</summary>
public sealed class FileStorageBrowserActions
{
    private readonly Func<Task> _onMutated;
    private readonly FileStorageManagement _host;

    /// <summary>Binds actions to the parent. <paramref name="onMutated" /> runs after a successful mutation so grids and the tree can refresh.</summary>
    public FileStorageBrowserActions(FileStorageManagement host, Func<Task> onMutated)
    {
        _host = ArgumentHelpers.ThrowIfNullReturn(host);
        _onMutated = ArgumentHelpers.ThrowIfNullReturn(onMutated);
    }

    /// <summary>Loads metadata (tombstones included) and opens the read-only metadata dialog.</summary>
    public async Task ViewFromRowAsync(object? row)
    {
        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var fileId)) {
            _host.SetStatus("Could not read file id from the grid row.", Severity.Warning);
            return;
        }

        await ViewAsync(fileId, FileStorageGridRowHelper.IsRowDeleted(row)).ConfigureAwait(false);
    }

    /// <summary>Fetches metadata and opens the read-only metadata dialog.</summary>
    public async Task ViewAsync(Guid fileId, bool includeDeleted = false)
    {
        try {
            var result = await GetMetadataAsync(fileId, includeDeleted).ConfigureAwait(false) ??
                throw new InvalidOperationException($"Metadata for file {fileId} was not returned.");

            var parameters = new DialogParameters<FileStoreMetadataDialog> { { d => d.Metadata, result } };
            await _host.DialogService.ShowAsync<FileStoreMetadataDialog>("File metadata", parameters, LyoDialogPresets.Medium);
            _host.SetStatus($"Loaded metadata for {fileId}.", Severity.Success);
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary><c>GET files/{id}/metadata</c> (includes tombstones when <paramref name="includeDeleted" /> is set).</summary>
    public async Task<FileStoreResult?> GetMetadataAsync(Guid fileId, bool includeDeleted = false)
    {
        var uri = includeDeleted
            ? _host.FilesApi($"files/{fileId:D}/metadata?includeDeleted=true")
            : _host.FilesApi($"files/{fileId:D}/metadata");
        return await _host.ApiClient.GetAsAsync<FileStoreResult>(uri).ConfigureAwait(false);
    }

    /// <summary>Shows the access-link dialog for an active file.</summary>
    public Task AccessLinkFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "Access links cannot be created for deleted files.", out var fileId)
            ? Task.CompletedTask
            : AccessLinkAsync(fileId, FileStorageGridRowHelper.GetOriginalFileNameFromRow(row));

    /// <summary>Shows the access-link dialog for an active file.</summary>
    public async Task AccessLinkAsync(Guid fileId, string? fileName = null)
    {
        var parameters = new DialogParameters<FileAccessLinkDialog> {
            { d => d.FileId, fileId },
            { d => d.FileName, fileName },
            { d => d.ApiRoutePrefix, _host.FileStorageApiRoutePrefix },
            { d => d.PublicBaseUrl, _host.ResolvePublicBaseUrl() }
        };
        await _host.DialogService.ShowAsync<FileAccessLinkDialog>("Create access link", parameters, LyoDialogPresets.Medium);
    }

    /// <summary>Opens the API download endpoint, which streams bytes through the host (decrypt/decompress). Direct-to-bucket URLs are access-link or presigned-read only.</summary>
    public Task DownloadFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "Deleted files cannot be downloaded; the backing object was removed.", out var fileId)
            ? Task.CompletedTask
            : DownloadAsync(fileId);

    /// <summary>Opens the API download endpoint for one active file.</summary>
    public async Task DownloadAsync(Guid fileId)
    {
        var url = _host.GetApiAbsoluteUrl(_host.FilesApi($"files/{fileId:D}/download"));
        if (url == null) {
            _host.SetStatus("ApiClient:BaseUrl is not configured; cannot download.", Severity.Warning);
            return;
        }

        try {
            await _host.JsRuntime.InvokeVoidAsync("open", url, "_blank");
            _host.SetStatus($"Started download for {fileId}.", Severity.Success);
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Opens the raw physical download for an object that exists on disk but is not in the catalog.</summary>
    public async Task DownloadPhysicalAsync(string physicalKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(physicalKey);
        var url = _host.GetApiAbsoluteUrl(_host.FilesApi("files/physical/download?physicalKey=" + Uri.EscapeDataString(physicalKey)));
        if (url == null) {
            _host.SetStatus("ApiClient:BaseUrl is not configured; cannot download.", Severity.Warning);
            return;
        }

        try {
            await _host.JsRuntime.InvokeVoidAsync("open", url, "_blank");
            _host.SetStatus($"Started physical download for {physicalKey}.", Severity.Success);
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Zips active files from <c>GET files/archive</c>.</summary>
    public async Task DownloadArchiveAsync(IReadOnlyList<Guid> fileIds)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        if (fileIds.Count == 0) {
            _host.SetStatus("Select at least one active file to download.", Severity.Warning);
            return;
        }

        try {
            var qs = string.Join("&", fileIds.Select(id => $"id={id:D}"));
            var (stream, fileName, _) = await _host.ApiClient.GetFileStreamAsync(_host.FilesApi($"files/archive?{qs}")).ConfigureAwait(false);
            await using (stream)
                await _host.Js.DownloadFileFromStream(stream, fileName ?? "files.zip", FileTypeInfo.Zip.MimeType);

            _host.SetStatus($"Downloaded {fileIds.Count} file(s).", Severity.Success);
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Confirms, then tombstone-deletes one file.</summary>
    public Task DeleteFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "This file is already deleted.", out var fileId) ? Task.CompletedTask : DeleteAsync(fileId);

    /// <summary>Confirms, then tombstone-deletes one file.</summary>
    public Task DeleteAsync(Guid fileId) => DeleteFilesAsync([fileId]);

    /// <summary>Confirms, then tombstone-deletes each file id. Tree bulk selection expands a checked folder to its descendant files.</summary>
    public Task DeleteFilesAsync(IReadOnlyList<Guid> fileIds)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        if (fileIds.Count == 0) {
            _host.SetStatus("Select at least one active file to delete.", Severity.Warning);
            return Task.CompletedTask;
        }

        return DeleteFilesCoreAsync(fileIds);
    }

    /// <summary>Prompts for a path prefix, then moves one file.</summary>
    public Task MoveFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "Deleted files cannot be moved.", out var fileId)
            ? Task.CompletedTask
            : MoveAsync(fileId, FileStorageGridRowHelper.GetPathPrefixFromRow(row));

    /// <summary>Prompts for a path prefix, then moves one file.</summary>
    public async Task MoveAsync(Guid fileId, string? currentPathPrefix = null)
    {
        var parameters = new DialogParameters<FileStoreMoveDialog> { { d => d.InitialPathPrefix, currentPathPrefix } };
        var dialog = await _host.DialogService.ShowAsync<FileStoreMoveDialog>("Move file", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        await MoveFilesAsync([fileId], prefix.PathPrefix);
    }

    /// <summary>Prompts for an optional path prefix, then copies one file.</summary>
    public Task CopyFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "Deleted files cannot be copied.", out var fileId)
            ? Task.CompletedTask
            : CopyAsync(fileId, FileStorageGridRowHelper.GetPathPrefixFromRow(row));

    /// <summary>Prompts for an optional path prefix, then copies one file.</summary>
    public async Task CopyAsync(Guid fileId, string? currentPathPrefix = null)
    {
        var parameters = new DialogParameters<FileStoreCopyDialog> { { d => d.InitialPathPrefix, currentPathPrefix } };
        var dialog = await _host.DialogService.ShowAsync<FileStoreCopyDialog>("Copy file", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        try {
            var copied = await _host.ApiClient
                .PostAsAsync<CopyFileRequest, FileStoreResult>(
                    _host.FilesApi("files/copy"), new(fileId, prefix.PathPrefix))
                .ConfigureAwait(false);
            _host.SetStatus($"Copied to {copied.Id}.", Severity.Success);
            await NotifyMutatedAsync();
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Renames the metadata display name for one file.</summary>
    public Task RenameFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "Deleted files cannot be renamed.", out var fileId)
            ? Task.CompletedTask
            : RenameAsync(fileId, FileStorageGridRowHelper.GetOriginalFileNameFromRow(row));

    /// <summary>Renames the metadata display name for one file.</summary>
    public async Task RenameAsync(Guid fileId, string? currentOriginalFileName = null)
    {
        var parameters = new DialogParameters<FileStoreRenameDialog> { { d => d.InitialOriginalFileName, currentOriginalFileName } };
        var dialog = await _host.DialogService.ShowAsync<FileStoreRenameDialog>("Rename file", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStoreRenameDialogResult rename })
            return;

        try {
            await _host.ApiClient
                .PostAsAsync<RenameFileRequest, FileStoreResult>(
                    _host.FilesApi("files/rename"), new(fileId, rename.OriginalFileName))
                .ConfigureAwait(false);
            _host.SetStatus($"Renamed file {fileId}.", Severity.Success);
            await NotifyMutatedAsync();
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    /// <summary>Prompts for target key options, then rotates the DEK for one file.</summary>
    public Task RotateDekFromRowAsync(object? row)
        => !TryGetActiveFileId(row, "Deleted files cannot have DEKs rotated.", out var fileId) ? Task.CompletedTask : RotateDekAsync(fileId);

    /// <summary>Prompts for target key options, then rotates the DEK for one file.</summary>
    public Task RotateDekAsync(Guid fileId) => RotateDeksAsync([fileId]);

    /// <summary>Prompts for target key options, then rotates DEKs for each file id. Used by tree bulk selection.</summary>
    public Task RotateDeksAsync(IReadOnlyList<Guid> fileIds)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        if (fileIds.Count == 0) {
            _host.SetStatus("Select at least one active file to rotate.", Severity.Warning);
            return Task.CompletedTask;
        }

        return RotateDeksCoreAsync(fileIds);
    }

    /// <summary>Copies the row's expected storage key to the clipboard.</summary>
    public async Task CopyExpectedKeyFromRowAsync(object? row)
    {
        var key = FileStorageGridRowHelper.GetExpectedStorageKey(row);
        if (string.IsNullOrWhiteSpace(key)) {
            _host.SetStatus("Could not build an expected storage key from the grid row.", Severity.Warning);
            return;
        }

        try {
            await _host.Js.SendToClipboard(key);
            _host.SetStatus("Copied expected storage key.", Severity.Success);
        }
        catch (Exception ex) {
            _host.SetStatus($"Copy failed: {ex.Message}", Severity.Warning);
        }
    }

    /// <summary>Moves every selected active file to one path prefix.</summary>
    public async Task BulkMoveAsync(IReadOnlyList<object?> rows)
    {
        var ids = GetActiveFileIds(rows);
        if (ids.Count == 0) {
            _host.SetStatus("Select at least one active file to move.", Severity.Warning);
            return;
        }

        var dialog = await _host.DialogService.ShowAsync<FileStoreMoveDialog>("Move files", new DialogParameters(), LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        await MoveFilesAsync(ids, prefix.PathPrefix);
    }

    /// <summary>Asks for a path prefix, then moves every file id there.</summary>
    public async Task BulkMoveAsync(IReadOnlyList<Guid> fileIds)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        if (fileIds.Count == 0) {
            _host.SetStatus("Select at least one active file to move.", Severity.Warning);
            return;
        }

        var dialog = await _host.DialogService.ShowAsync<FileStoreMoveDialog>("Move files", new DialogParameters(), LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStorePathPrefixDialogResult prefix })
            return;

        await MoveFilesAsync(fileIds, prefix.PathPrefix);
    }

    /// <summary>Moves each file to its destination path prefix with no dialog. Used by tree drag-and-drop.</summary>
    public async Task MoveFilesToPrefixesAsync(IReadOnlyList<(Guid FileId, string? PathPrefix)> moves)
    {
        ArgumentHelpers.ThrowIfNull(moves);
        if (moves.Count == 0) {
            _host.SetStatus("Nothing to move.", Severity.Warning);
            return;
        }

        var failed = 0;
        foreach (var (fileId, pathPrefix) in moves) {
            try {
                await _host.ApiClient
                    .PostAsAsync<MoveFileRequest, FileStoreResult>(_host.FilesApi("files/move"), new(fileId, pathPrefix))
                    .ConfigureAwait(false);
            }
            catch (Exception ex) {
                failed++;
                _host.SetStatus($"Move failed for {fileId}: {ex.Message}", Severity.Error);
            }
        }

        var moved = moves.Count - failed;
        if (moved > 0)
            _host.SetStatus(failed == 0 ? $"Moved {moved} file(s)." : $"Moved {moved} file(s); {failed} failed.", failed == 0 ? Severity.Success : Severity.Warning);

        if (moved > 0)
            await NotifyMutatedAsync();
    }

    /// <summary>After confirmation, deletes every selected active file.</summary>
    public async Task BulkDeleteAsync(IReadOnlyList<object?> rows)
    {
        var ids = GetActiveFileIds(rows);
        if (ids.Count == 0) {
            _host.SetStatus("Select at least one active file to delete.", Severity.Warning);
            return;
        }

        await DeleteFilesCoreAsync(ids);
    }

    /// <summary>Rotates DEKs on every selected active file.</summary>
    public async Task BulkRotateDeksAsync(IReadOnlyList<object?> rows)
    {
        var ids = GetActiveFileIds(rows);
        if (ids.Count == 0) {
            _host.SetStatus("Select at least one active file to rotate.", Severity.Warning);
            return;
        }

        await RotateDeksCoreAsync(ids);
    }

    private async Task MoveFilesAsync(IReadOnlyList<Guid> fileIds, string? pathPrefix)
    {
        var failed = 0;
        foreach (var fileId in fileIds) {
            try {
                await _host.ApiClient
                    .PostAsAsync<MoveFileRequest, FileStoreResult>(_host.FilesApi("files/move"), new(fileId, pathPrefix))
                    .ConfigureAwait(false);
            }
            catch (Exception ex) {
                failed++;
                _host.SetStatus($"Move failed for {fileId}: {ex.Message}", Severity.Error);
            }
        }

        var moved = fileIds.Count - failed;
        if (moved > 0)
            _host.SetStatus(failed == 0 ? $"Moved {moved} file(s)." : $"Moved {moved} file(s); {failed} failed.", failed == 0 ? Severity.Success : Severity.Warning);

        if (moved > 0)
            await NotifyMutatedAsync();
    }

    private async Task DeleteFilesCoreAsync(IReadOnlyList<Guid> fileIds)
    {
        var subject = fileIds.Count == 1 ? $"file {fileIds[0]}" : $"{fileIds.Count} files";
        if (!await _host.DialogService.ConfirmDeleteAsync(subject, title: "Delete file"))
            return;

        var failed = 0;
        foreach (var fileId in fileIds) {
            try {
                var deleted = await _host.ApiClient.DeleteAsAsync<bool>(_host.FilesApi($"files/{fileId:D}")).ConfigureAwait(false);
                if (!deleted)
                    failed++;
            }
            catch (Exception ex) {
                failed++;
                _host.SetStatus($"Delete failed for {fileId}: {ex.Message}", Severity.Error);
            }
        }

        var deletedCount = fileIds.Count - failed;
        if (deletedCount > 0)
            _host.SetStatus(
                failed == 0 ? $"Deleted {deletedCount} file(s)." : $"Deleted {deletedCount} file(s); {failed} failed.", failed == 0 ? Severity.Success : Severity.Warning);
        else
            _host.SetStatus("No files were deleted.", Severity.Warning);

        if (deletedCount > 0)
            await NotifyMutatedAsync();
    }

    private async Task RotateDeksCoreAsync(IReadOnlyList<Guid> fileIds)
    {
        var parameters = new DialogParameters<FileStoreRotateDekDialog> {
            { d => d.FileCount, fileIds.Count },
            { d => d.KeyIds, _host.EncryptionKeyIds }
        };
        var dialog = await _host.DialogService.ShowAsync<FileStoreRotateDekDialog>("Rotate DEKs", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: FileStoreRotateDekDialogResult rotate })
            return;

        try {
            var rotation = await _host.ApiClient
                .PostAsAsync<RotateDeksRequest, DekMigrationResult>(
                    _host.FilesApi("files/rotate-deks"), new(fileIds, rotate.TargetKeyId, rotate.TargetKeyVersion, rotate.BatchSize))
                .ConfigureAwait(false);
            _host.SetStatus(
                rotation.AllSucceeded ? "DEK rotation completed." : "DEK rotation completed with failures.", rotation.AllSucceeded ? Severity.Success : Severity.Warning);
            await NotifyMutatedAsync();
        }
        catch (Exception ex) {
            _host.SetStatus(ex.Message, Severity.Error);
        }
    }

    private bool TryGetActiveFileId(object? row, string deletedMessage, out Guid fileId)
    {
        fileId = default;
        if (FileStorageGridRowHelper.IsRowDeleted(row)) {
            _host.SetStatus(deletedMessage, Severity.Info);
            return false;
        }

        if (FileStorageGridRowHelper.TryGetFileIdFromRow(row, out fileId))
            return true;

        _host.SetStatus("Could not read file id from the grid row.", Severity.Warning);
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
