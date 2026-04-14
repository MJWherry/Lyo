using System.Linq;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;
using Lyo.Web.Components;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components;

public partial class FileStoreFilesTab : ComponentBase
{
    [CascadingParameter]
    public FileStorageWorkbench Workbench { get; set; } = default!;

    private bool _fileBusy;
    private IIOTempSession? _stagingSession;
    private LocalBrowserFilePath? _uploadedFile;
    private string _uploadStatus = "No file selected.";
    private string? _saveOriginalFileName;
    private string _savePathPrefix = string.Empty;
    private bool _saveCompress;
    private bool _saveEncrypt;
    private string _saveKeyId = string.Empty;
    private int? _saveChunkSize;

    private int _cryptoOpsTab;
    private LyoDataGridProjected? _fileMetadataGrid;

    private string CryptoOpsMigrationTitle => _cryptoOpsTab == 0 ? "DEK migration" : "KEK migration";

    private string CryptoOpsRotationTitle => _cryptoOpsTab == 0 ? "DEK rotation" : "KEK rotation";

    private string CryptoOpsKind => _cryptoOpsTab == 0 ? "DEK" : "KEK";

    private string _migrationSourceKeyId = string.Empty;
    private string _migrationSourceKeyVersion = string.Empty;
    private string _migrationTargetKeyId = string.Empty;
    private string _migrationTargetKeyVersion = string.Empty;
    private int _migrationBatchSize = 100;
    private DekMigrationResult? _migrationResult;
    private const string _guidValidationPattern = "^[{(]?[0-9A-Fa-f]{8}(?:-?[0-9A-Fa-f]{4}){3}-?[0-9A-Fa-f]{12}[)}]?$";
    private List<string> _rotationFileIds = [];
    private string _rotationTargetKeyId = string.Empty;
    private string _rotationTargetKeyVersion = string.Empty;
    private int _rotationBatchSize = 100;
    private DekMigrationResult? _rotationResult;

    private IReadOnlyList<string> GetKnownVersions(string? keyId) => Workbench.GetKnownVersionsForKey(keyId);

    private async Task OnClientFilePathReadyAsync(LocalBrowserFilePath file)
    {
        _uploadedFile = file;
        _saveOriginalFileName = file.FileName;

        if (Workbench.FileStorage == null)
            _uploadStatus = $"{file.FileName} staged in IO temp session (no storage service).";
        else if (_saveEncrypt && string.IsNullOrWhiteSpace(_saveKeyId))
            _uploadStatus = $"{file.FileName} staged in IO temp session — select a key id, then click Upload.";
        else
            _uploadStatus = $"{file.FileName} staged in IO temp session — click Upload to send to the API.";

        await InvokeAsync(StateHasChanged);
    }

    private Task OnClientFilePathRemovedAsync(LocalBrowserFilePath file)
    {
        if (ReferenceEquals(_uploadedFile, file))
            _uploadedFile = null;

        _saveOriginalFileName = null;
        _uploadStatus = $"{file.FileName} removed.";
        return Task.CompletedTask;
    }

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

    private async Task SaveFileAsync()
    {
        var storage = Workbench.FileStorage;
        if (storage == null) {
            Workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
            _uploadStatus = "No file storage service.";
            return;
        }

        if (_uploadedFile == null) {
            Workbench.SetStatus("Choose a file first.", Severity.Warning);
            _uploadStatus = "No file selected.";
            return;
        }

        if (_saveEncrypt && string.IsNullOrWhiteSpace(_saveKeyId)) {
            Workbench.SetStatus("Key id is required when encryption is enabled.", Severity.Warning);
            _uploadStatus = "Select a key id for encrypted uploads.";
            return;
        }

        _fileBusy = true;
        _uploadStatus = "Uploading to storage…";
        try {
            await InvokeAsync(StateHasChanged);
            var result = await storage.SaveFileAsync(_uploadedFile.FilePath, string.IsNullOrWhiteSpace(_saveOriginalFileName) ? _uploadedFile.FileName : _saveOriginalFileName, _saveCompress, _saveEncrypt, _saveEncrypt ? _saveKeyId : null, string.IsNullOrWhiteSpace(_savePathPrefix) ? null : _savePathPrefix, _saveChunkSize);
            if (_fileMetadataGrid != null)
                await _fileMetadataGrid.RefreshData();

            Workbench.SetStatus($"Uploaded file {result.Id}.", Severity.Success);
            _uploadStatus = $"Uploaded file {result.Id}.";
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
            _uploadStatus = $"Upload failed: {ex.Message}";
        }
        finally {
            _fileBusy = false;
        }
    }

    private async Task MigrateDeksAsync()
    {
        var storage = Workbench.FileStorage;
        if (storage == null) {
            Workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_migrationSourceKeyId)) {
            Workbench.SetStatus($"Source key id is required for {CryptoOpsKind} migration.", Severity.Warning);
            return;
        }

        _fileBusy = true;
        try {
            _migrationResult = await storage.MigrateDeksAsync(_migrationSourceKeyId, NullIfWhiteSpace(_migrationSourceKeyVersion), NullIfWhiteSpace(_migrationTargetKeyId), NullIfWhiteSpace(_migrationTargetKeyVersion), _migrationBatchSize);
            Workbench.SetStatus(_migrationResult.AllSucceeded ? $"{CryptoOpsKind} migration completed." : $"{CryptoOpsKind} migration completed with failures.", _migrationResult.AllSucceeded ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _fileBusy = false;
        }
    }

    private async Task RotateDeksAsync()
    {
        var storage = Workbench.FileStorage;
        if (storage == null) {
            Workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (!TryParseRotationFileIds(out var fileIds))
            return;

        _fileBusy = true;
        try {
            _rotationResult = await storage.RotateDeksAsync(fileIds, NullIfWhiteSpace(_rotationTargetKeyId), NullIfWhiteSpace(_rotationTargetKeyVersion), _rotationBatchSize);
            Workbench.SetStatus(_rotationResult.AllSucceeded ? $"{CryptoOpsKind} rotation completed." : $"{CryptoOpsKind} rotation completed with failures.", _rotationResult.AllSucceeded ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _fileBusy = false;
        }
    }

    private Task OnRotationFileIdsChanged(IEnumerable<string> values)
    {
        _rotationFileIds = values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return Task.CompletedTask;
    }

    private async Task ViewFileMetadataFromRowAsync(object? row)
    {
        var storage = Workbench.FileStorage;
        if (storage == null) {
            Workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var fileId)) {
            Workbench.SetStatus("Could not read file id from the grid row.", Severity.Warning);
            return;
        }

        _fileBusy = true;
        try {
            var result = await storage.GetMetadataAsync(fileId);
            await ShowFileMetadataDialogAsync(result);
            Workbench.SetStatus($"Loaded metadata for {fileId}.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _fileBusy = false;
        }
    }

    private async Task DownloadFileFromRowAsync(object? row)
    {
        var storage = Workbench.FileStorage;
        if (storage == null) {
            Workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var fileId)) {
            Workbench.SetStatus("Could not read file id from the grid row.", Severity.Warning);
            return;
        }

        _fileBusy = true;
        try {
            var meta = await storage.GetMetadataAsync(fileId);
            if (!meta.IsEncrypted && !meta.IsCompressed) {
                var url = await storage.GetPreSignedReadUrlAsync(fileId);
                await Workbench.JsRuntime.InvokeVoidAsync("open", url, "_blank");
                Workbench.SetStatus($"Opened presigned download for {fileId}.", Severity.Success);
            }
            else {
                var url = Workbench.NavigationManager.ToAbsoluteUri($"/{Workbench.ProxyDownloadPath}/{fileId:D}").AbsoluteUri;
                await Workbench.JsRuntime.InvokeVoidAsync("open", url, "_blank");
                Workbench.SetStatus($"Started download for {fileId}.", Severity.Success);
            }
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _fileBusy = false;
        }
    }

    private async Task DeleteFileFromRowAsync(object? row)
    {
        var storage = Workbench.FileStorage;
        if (storage == null) {
            Workbench.SetStatus("No file storage service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var fileId)) {
            Workbench.SetStatus("Could not read file id from the grid row.", Severity.Warning);
            return;
        }

        var confirm = await Workbench.DialogService.ShowMessageBoxAsync("Delete file", $"Delete file {fileId}?", "Delete", cancelText: "Cancel");
        if (confirm != true)
            return;

        _fileBusy = true;
        try {
            var deleted = await storage.DeleteFileAsync(fileId);

            if (deleted && _fileMetadataGrid != null)
                await _fileMetadataGrid.RefreshData();

            Workbench.SetStatus(deleted ? $"Deleted file {fileId}." : $"File {fileId} was not deleted.", deleted ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _fileBusy = false;
        }
    }

    private Task OnSaveKeyIdChanged(string? value)
    {
        _saveKeyId = value ?? string.Empty;
        if (_uploadedFile != null && Workbench.FileStorage != null && _saveEncrypt) {
            _uploadStatus = string.IsNullOrWhiteSpace(_saveKeyId)
                ? $"{_uploadedFile.FileName} staged in IO temp session — select a key id, then click Upload."
                : $"{_uploadedFile.FileName} staged in IO temp session — click Upload to send to the API.";
        }

        return Task.CompletedTask;
    }

    private Task OnMigrationSourceKeyIdChanged(string? value)
    {
        _migrationSourceKeyId = value ?? string.Empty;
        NormalizeVersionSelection(_migrationSourceKeyId, ref _migrationSourceKeyVersion);
        return Task.CompletedTask;
    }

    private Task OnMigrationSourceKeyVersionChanged(string? value)
    {
        _migrationSourceKeyVersion = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnMigrationTargetKeyIdChanged(string? value)
    {
        _migrationTargetKeyId = value ?? string.Empty;
        NormalizeVersionSelection(_migrationTargetKeyId, ref _migrationTargetKeyVersion);
        return Task.CompletedTask;
    }

    private Task OnMigrationTargetKeyVersionChanged(string? value)
    {
        _migrationTargetKeyVersion = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnRotationTargetKeyIdChanged(string? value)
    {
        _rotationTargetKeyId = value ?? string.Empty;
        NormalizeVersionSelection(_rotationTargetKeyId, ref _rotationTargetKeyVersion);
        return Task.CompletedTask;
    }

    private Task OnRotationTargetKeyVersionChanged(string? value)
    {
        _rotationTargetKeyVersion = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private void NormalizeVersionSelection(string? keyId, ref string version)
    {
        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(keyId))
            return;

        if (!GetKnownVersions(keyId).Contains(version, StringComparer.Ordinal))
            version = string.Empty;
    }

    protected override void OnParametersSet()
    {
        EnsureStagingSession();
        NormalizeFileTabSelections();
    }

    private void EnsureStagingSession()
    {
        if (_stagingSession != null || Workbench == null)
            return;

        _stagingSession = Workbench.TempService.CreateSession();
    }

    public async ValueTask DisposeAsync()
    {
        if (_stagingSession is null)
            return;

        await _stagingSession.DisposeAsync();
        _stagingSession = null;
    }

    private void NormalizeFileTabSelections()
    {
        var ids = Workbench.AvailableKeyIds;
        if (!string.IsNullOrWhiteSpace(_saveKeyId) && !ids.Contains(_saveKeyId, StringComparer.Ordinal))
            _saveKeyId = string.Empty;

        if (!string.IsNullOrWhiteSpace(_migrationSourceKeyId) && !ids.Contains(_migrationSourceKeyId, StringComparer.Ordinal))
            _migrationSourceKeyId = string.Empty;

        if (!string.IsNullOrWhiteSpace(_migrationTargetKeyId) && !ids.Contains(_migrationTargetKeyId, StringComparer.Ordinal))
            _migrationTargetKeyId = string.Empty;

        if (!string.IsNullOrWhiteSpace(_rotationTargetKeyId) && !ids.Contains(_rotationTargetKeyId, StringComparer.Ordinal))
            _rotationTargetKeyId = string.Empty;

        NormalizeVersionSelection(_migrationSourceKeyId, ref _migrationSourceKeyVersion);
        NormalizeVersionSelection(_migrationTargetKeyId, ref _migrationTargetKeyVersion);
        NormalizeVersionSelection(_rotationTargetKeyId, ref _rotationTargetKeyVersion);
    }

    private bool TryParseRotationFileIds(out IReadOnlyCollection<Guid> fileIds)
    {
        if (_rotationFileIds.Count == 0) {
            fileIds = [];
            Workbench.SetStatus("Enter at least one file id to rotate.", Severity.Warning);
            return false;
        }

        var parsedIds = new List<Guid>();
        var invalidTokens = new List<string>();
        foreach (var value in _rotationFileIds) {
            if (Guid.TryParse(value, out var fileId))
                parsedIds.Add(fileId);
            else
                invalidTokens.Add(value);
        }

        if (invalidTokens.Count > 0) {
            fileIds = [];
            Workbench.SetStatus($"One or more file ids are invalid: {string.Join(", ", invalidTokens.Take(5))}", Severity.Warning);
            return false;
        }

        fileIds = parsedIds.Distinct().ToList();
        return true;
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task ShowFileMetadataDialogAsync(FileStoreResult metadata)
    {
        var parameters = new DialogParameters<FileStoreMetadataDialog> { { d => d.Metadata, metadata } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        await Workbench.DialogService.ShowAsync<FileStoreMetadataDialog>("File metadata", parameters, options);
    }
}
