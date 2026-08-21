using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.IO.Temp.Models;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

public partial class FileStoreFilesTab : ComponentBase
{
    private const string GuidValidationPattern = "^[{(]?[0-9A-Fa-f]{8}(?:-?[0-9A-Fa-f]{4}){3}-?[0-9A-Fa-f]{12}[)}]?$";

    private int _cryptoOpsTab;

    private bool _fileBusy;
    private int _migrationBatchSize = 100;
    private DekMigrationResult? _migrationResult;

    private string _migrationSourceKeyId = string.Empty;
    private string _migrationSourceKeyVersion = string.Empty;
    private string _migrationTargetKeyId = string.Empty;
    private string _migrationTargetKeyVersion = string.Empty;
    private int _rotationBatchSize = 100;
    private List<string> _rotationFileIds = [];
    private DekMigrationResult? _rotationResult;
    private string _rotationTargetKeyId = string.Empty;
    private string _rotationTargetKeyVersion = string.Empty;
    private int? _saveChunkSize;
    private bool _saveCompress;
    private bool _saveEncrypt;
    private string _saveKeyId = string.Empty;
    private string? _saveOriginalFileName;
    private string _savePathPrefix = string.Empty;
    private IIOTempSession? _stagingSession;
    private string _uploadStatus = "No file selected.";
    private LocalBrowserFilePath? _uploadedFile;

    [CascadingParameter]
    public FileStorageWorkbench Workbench { get; set; } = default!;

    private string CryptoOpsMigrationTitle => _cryptoOpsTab == 0 ? "DEK migration" : "KEK migration";

    private string CryptoOpsRotationTitle => _cryptoOpsTab == 0 ? "DEK rotation" : "KEK rotation";

    private string CryptoOpsKind => _cryptoOpsTab == 0 ? "DEK" : "KEK";

    public async ValueTask DisposeAsync()
    {
        if (_stagingSession is null)
            return;

        await _stagingSession.DisposeAsync();
        _stagingSession = null;
    }

    private async Task OnClientFilePathReadyAsync(LocalBrowserFilePath file)
    {
        _uploadedFile = file;
        _saveOriginalFileName = file.FileName;
        if (_saveEncrypt && string.IsNullOrWhiteSpace(_saveKeyId))
            _uploadStatus = $"{file.FileName} staged in IO temp session — enter a key id, then click Upload.";
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
        if (_uploadedFile == null) {
            Workbench.SetStatus("Choose a file first.", Severity.Warning);
            _uploadStatus = "No file selected.";
            return;
        }

        if (_saveEncrypt && string.IsNullOrWhiteSpace(_saveKeyId)) {
            Workbench.SetStatus("Key id is required when encryption is enabled.", Severity.Warning);
            _uploadStatus = "Enter a key id for encrypted uploads.";
            return;
        }

        _fileBusy = true;
        _uploadStatus = "Uploading to storage…";
        try {
            await InvokeAsync(StateHasChanged);
            var uri = Workbench.BuildSaveStreamUri(
                string.IsNullOrWhiteSpace(_saveOriginalFileName) ? _uploadedFile.FileName : _saveOriginalFileName, _saveCompress, _saveEncrypt,
                _saveEncrypt ? NullIfWhiteSpace(_saveKeyId) : null, string.IsNullOrWhiteSpace(_savePathPrefix) ? null : _savePathPrefix, _saveChunkSize);
            var result = await Workbench.ApiClient.PostFileAsAsync<FileStoreResult>(uri, _uploadedFile.FilePath).ConfigureAwait(false);

            await Workbench.NotifyFilesChangedAsync();

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
        if (string.IsNullOrWhiteSpace(_migrationSourceKeyId)) {
            Workbench.SetStatus($"Source key id is required for {CryptoOpsKind} migration.", Severity.Warning);
            return;
        }

        _fileBusy = true;
        try {
            _migrationResult = await Workbench.ApiClient
                .PostAsAsync<MigrateDeksRequest, DekMigrationResult>(
                    Workbench.FilesApi("files/migrate-deks"),
                    new(
                        _migrationSourceKeyId, NullIfWhiteSpace(_migrationSourceKeyVersion), NullIfWhiteSpace(_migrationTargetKeyId),
                        NullIfWhiteSpace(_migrationTargetKeyVersion), _migrationBatchSize))
                .ConfigureAwait(false);

            Workbench.SetStatus(
                _migrationResult.AllSucceeded ? $"{CryptoOpsKind} migration completed." : $"{CryptoOpsKind} migration completed with failures.",
                _migrationResult.AllSucceeded ? Severity.Success : Severity.Warning);
            await Workbench.NotifyFilesChangedAsync();
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
        if (!TryParseRotationFileIds(out var fileIds))
            return;

        _fileBusy = true;
        try {
            _rotationResult = await Workbench.ApiClient
                .PostAsAsync<RotateDeksRequest, DekMigrationResult>(
                    Workbench.FilesApi("files/rotate-deks"),
                    new(fileIds, NullIfWhiteSpace(_rotationTargetKeyId), NullIfWhiteSpace(_rotationTargetKeyVersion), _rotationBatchSize))
                .ConfigureAwait(false);
            Workbench.SetStatus(
                _rotationResult.AllSucceeded ? $"{CryptoOpsKind} rotation completed." : $"{CryptoOpsKind} rotation completed with failures.",
                _rotationResult.AllSucceeded ? Severity.Success : Severity.Warning);
            await Workbench.NotifyFilesChangedAsync();
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

    protected override void OnParametersSet() => EnsureStagingSession();

    private void EnsureStagingSession()
    {
        if (_stagingSession != null)
            return;

        _stagingSession = Workbench.TempService.CreateSession();
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
}
