using System.IO;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Variant = MudBlazor.Variant;

namespace Lyo.Web.Components.FileUpload;

public partial class LyoFileUpload : IDisposable
{
    public enum UploadProgressViewType
    {
        None,
        Percentage,
        ProgressBar,
        Both
    }

    private readonly List<LyoFileUploadState> _fileStates = [];
    private bool _isDragOver;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Parameter]
    public FileTypeFlags ValidFileTypes { get; set; } = FileTypeFlags.All;

    [Parameter]
    public int MaxFiles { get; set; } = 5;

    [Parameter]
    public string? ButtonText { get; set; }

    [Parameter]
    public string ButtonIcon { get; set; } = Icons.Material.Filled.CloudUpload;

    [Parameter]
    public Color ButtonColor { get; set; } = Color.Primary;

    [Parameter]
    public Variant ButtonVariant { get; set; } = Variant.Filled;

    [Parameter]
    public bool ButtonFullWidth { get; set; }

    [Parameter]
    public bool InlineLayout { get; set; }

    [Parameter]
    public UploadProgressViewType ProgressViewType { get; set; } = UploadProgressViewType.Percentage;

    [Parameter]
    public TimeSpan UploadTimeout { get; init; } = TimeSpan.FromMinutes(2);

    [Parameter]
    public long MaxFileSize { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(3);

    [Parameter]
    public bool ShowRemoveButton { get; set; } = true;

    [Parameter]
    public bool ShowFiles { get; set; } = true;

    [Parameter]
    public bool ChipsToggleable { get; set; }

    /// <summary>Maximum characters for the file name label inside each chip; longer names show as <c>prefix...ext</c> with the full name in a tooltip.</summary>
    [Parameter]
    public int ChipFileNameMaxLength { get; set; } = 32;

    /// <summary>
    /// <see cref="ClientFileDisplayMode.Chips" /> shows per-file chips (subject to <see cref="ShowFiles" />). <see cref="ClientFileDisplayMode.ExternalList" /> hides all chip
    /// UI; use <see cref="OnClientFileReady" /> in a parent listbox/list.
    /// </summary>
    [Parameter]
    public ClientFileDisplayMode ClientFileDisplay { get; set; } = ClientFileDisplayMode.Chips;

    [Parameter]
    public bool AllowDragDrop { get; set; }

    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public EventCallback<FileUploadRes> OnFileUploaded { get; set; }

    [Parameter]
    public EventCallback<FileUploadRes> OnFileRemoved { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<FileUploadRes>> OnAllFilesUploaded { get; set; }

    [Parameter]
    public EventCallback<LocalBrowserFile> OnClientFileReady { get; set; }

    [Parameter]
    public EventCallback<LocalBrowserFile> OnClientFileRemoved { get; set; }

    /// <summary>
    /// When set, the component streams browser files to temp disk instead of buffering in memory.
    /// The <see cref="OnClientFilePathReady" /> / <see cref="OnClientFilePathRemoved" /> callbacks fire instead of
    /// <see cref="OnClientFileReady" /> / <see cref="OnClientFileRemoved" />.
    /// </summary>
    [Parameter]
    public IIOTempService? TempService { get; set; }

    /// <summary>
    /// When set (takes precedence over <see cref="TempService" />), staged bytes are written under this IO temp session directory.
    /// </summary>
    [Parameter]
    public IIOTempSession? TempSession { get; set; }

    [Parameter]
    public EventCallback<LocalBrowserFilePath> OnClientFilePathReady { get; set; }

    [Parameter]
    public EventCallback<LocalBrowserFilePath> OnClientFilePathRemoved { get; set; }

    [Parameter]
    public EventCallback<LyoFileUploadEventArgs> OnUploadStarted { get; set; }

    [Parameter]
    public EventCallback<LyoFileUploadEventArgs> OnUploadProgress { get; set; }

    [Parameter]
    public EventCallback<LyoFileUploadEventArgs> OnUploadCompleted { get; set; }

    [Parameter]
    public EventCallback<LyoFileUploadEventArgs> OnUploadCancelled { get; set; }

    [Parameter]
    public EventCallback<LyoFileUploadEventArgs> OnUploadFailed { get; set; }

    public void Dispose()
    {
        foreach (var fileState in _fileStates)
            fileState.CancellationTokenSource?.Dispose();
    }

    private void OnDragEnter() => _isDragOver = true;

    private void OnDragLeave() => _isDragOver = false;

    private Task OnFilesChanged(IBrowserFile file) => OnFilesChanged([file]);

    private async Task OnFilesChanged(IReadOnlyList<IBrowserFile> files)
    {
        var remainingSlots = GetRemainingFileSlots();
        if (remainingSlots <= 0) {
            Snackbar.Add($"Maximum of {GetMaxFiles()} files reached.", Severity.Warning);
            return;
        }

        if (files.Count > remainingSlots) {
            Snackbar.Add($"Only {remainingSlots} file(s) can be added. Extra files were ignored.", Severity.Info);
            files = files.Take(remainingSlots).ToList();
        }

        var batch = new List<(IBrowserFile File, LyoFileUploadState State)>();
        foreach (var file in files) {
            if (file.Size > MaxFileSize) {
                Snackbar.Add($"File {file.Name} exceeds maximum size of {FileSizeUnitInfo.FormatBestFitAbbreviation(MaxFileSize)}", Severity.Error);
                continue;
            }

            var fileState = new LyoFileUploadState {
                Id = Guid.NewGuid(),
                FileName = file.Name,
                FileSize = file.Size,
                Status = LyoFileUploadStatus.Pending,
                CancellationTokenSource = new(UploadTimeout)
            };

            _fileStates.Add(fileState);
            batch.Add((file, fileState));
        }

        foreach (var (file, fileState) in batch) {
            try {
                fileState.Status = LyoFileUploadStatus.Uploading;
                await NotifyUploadEventAsync(OnUploadStarted, fileState);

                if (TempService != null || TempSession != null)
                    await StreamToTempFileAsync(file, fileState);
                else
                    await BufferToMemoryAsync(file, fileState);

                Snackbar.Add($"{file.Name} ready", Severity.Success);
            }
            catch (OperationCanceledException) {
                fileState.Status = LyoFileUploadStatus.Cancelled;
                await NotifyUploadEventAsync(OnUploadCancelled, fileState);
                Snackbar.Add($"{file.Name} upload cancelled", Severity.Warning);
            }
            catch (Exception ex) {
                fileState.Status = LyoFileUploadStatus.Failed;
                fileState.ErrorMessage = ex.Message;
                await NotifyUploadEventAsync(OnUploadFailed, fileState);
                Snackbar.Add($"{file.Name} failed: {ex.Message}", Severity.Error);
            }
            finally {
                _ = InvokeAsync(StateHasChanged);
            }
        }

        await OnAllFilesUploaded.InvokeAsync([]);
    }

    private async Task BufferToMemoryAsync(IBrowserFile file, LyoFileUploadState fileState)
    {
        await using var stream = file.OpenReadStream(MaxFileSize, fileState.CancellationTokenSource.Token);
        using var memoryStream = new MemoryStream((int)Math.Min(file.Size, int.MaxValue));
        var buffer = new byte[65536];
        long readTotal = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, fileState.CancellationTokenSource.Token)) > 0) {
            await memoryStream.WriteAsync(buffer.AsMemory(0, read), fileState.CancellationTokenSource.Token);
            readTotal += read;
            if (file.Size > 0)
                fileState.Progress = readTotal * 100.0 / file.Size;

            await NotifyUploadEventAsync(OnUploadProgress, fileState);
            await InvokeAsync(StateHasChanged);
        }

        var bytes = memoryStream.ToArray();
        fileState.Progress = 100;
        fileState.Status = LyoFileUploadStatus.Completed;
        fileState.ClientFile = new(file.Name, bytes);
        await NotifyUploadEventAsync(OnUploadCompleted, fileState);
        if (OnClientFileReady.HasDelegate)
            await OnClientFileReady.InvokeAsync(fileState.ClientFile);
    }

    private async Task StreamToTempFileAsync(IBrowserFile file, LyoFileUploadState fileState)
    {
        var tempPath = TempSession != null ? TempSession.TouchFile(file.Name) : TempService!.CreateFile(file.Name);
        try {
            await using var stream = file.OpenReadStream(MaxFileSize, fileState.CancellationTokenSource.Token);
            // Close the temp FileStream before OnClientFilePathReady — consumers open the same path for read on explicit upload/save.
            {
                await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer, fileState.CancellationTokenSource.Token)) > 0) {
                    await fs.WriteAsync(buffer.AsMemory(0, read), fileState.CancellationTokenSource.Token);
                    readTotal += read;
                    if (file.Size > 0)
                        fileState.Progress = readTotal * 100.0 / file.Size;

                    await NotifyUploadEventAsync(OnUploadProgress, fileState);
                    await InvokeAsync(StateHasChanged);
                }
            }

            fileState.Progress = 100;
            fileState.Status = LyoFileUploadStatus.Completed;
            fileState.ClientFilePath = new(file.Name, tempPath, file.Size);
            await NotifyUploadEventAsync(OnUploadCompleted, fileState);
            if (OnClientFilePathReady.HasDelegate)
                await OnClientFilePathReady.InvokeAsync(fileState.ClientFilePath);
        }
        catch {
            try { File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }

    private void CancelUpload(LyoFileUploadState fileState) => fileState.CancellationTokenSource?.Cancel();

    private void CancelAllUploads()
    {
        foreach (var fileState in _fileStates.Where(file => file.Status == LyoFileUploadStatus.Uploading))
            fileState.CancellationTokenSource?.Cancel();

        _ = InvokeAsync(StateHasChanged);
    }

    private bool HasActiveUploads() => _fileStates.Any(file => file.Status == LyoFileUploadStatus.Uploading);

    /// <summary>Removes a completed client-side file from the list (same reference as passed to <see cref="OnClientFileReady" />).</summary>
    public async Task RemoveClientFileAsync(LocalBrowserFile? clientFile)
    {
        if (clientFile == null)
            return;

        var fileState = _fileStates.FirstOrDefault(s => ReferenceEquals(s.ClientFile, clientFile));
        if (fileState != null)
            await RemoveFile(fileState);
    }

    private async Task RemoveFile(LyoFileUploadState fileState)
    {
        _fileStates.Remove(fileState);
        if (fileState.UploadResult != null)
            await OnFileRemoved.InvokeAsync(fileState.UploadResult);

        if (fileState.ClientFile != null)
            await OnClientFileRemoved.InvokeAsync(fileState.ClientFile);

        if (fileState.ClientFilePath != null) {
            try { File.Delete(fileState.ClientFilePath.FilePath); } catch { /* best effort */ }
            if (OnClientFilePathRemoved.HasDelegate)
                await OnClientFilePathRemoved.InvokeAsync(fileState.ClientFilePath);
        }

        fileState.CancellationTokenSource?.Dispose();
        await InvokeAsync(StateHasChanged);
    }

    private string GetButtonText() => !string.IsNullOrEmpty(ButtonText) ? ButtonText : $"Upload {GetFileTypeDescription()}";

    private int GetMaxFiles() => Math.Max(1, MaxFiles);

    private int GetRemainingFileSlots() => Math.Max(0, GetMaxFiles() - _fileStates.Count);

    private string GetChipFileNameDisplay(string? fileName) => FormatChipFileNameDisplay(fileName, ChipFileNameMaxLength);

    private bool ShouldShowChipFileNameTooltip(string? fileName)
        => !string.IsNullOrEmpty(fileName) && fileName.Length > ChipFileNameMaxLength;

    /// <summary>Shortens long file names while keeping the extension visible (e.g. <c>my_long_name...jpg</c>).</summary>
    private static string FormatChipFileNameDisplay(string? fileName, int maxLength)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        if (fileName.Length <= maxLength)
            return fileName;

        const string Ellipsis = "...";
        if (maxLength <= Ellipsis.Length)
            return Ellipsis[..maxLength];

        var ext = Path.GetExtension(fileName);
        var dotIdx = fileName.LastIndexOf('.');
        var hasExt = dotIdx > 0 && ext.Length > 0 && dotIdx == fileName.Length - ext.Length;

        if (!hasExt || ext.Length >= maxLength - Ellipsis.Length)
            return fileName[..(maxLength - Ellipsis.Length)] + Ellipsis;

        var headBudget = maxLength - Ellipsis.Length - ext.Length;
        if (headBudget < 1)
            return fileName[..(maxLength - Ellipsis.Length)] + Ellipsis;

        var root = fileName[..dotIdx];
        var head = root.Length <= headBudget ? root : root[..headBudget];
        return $"{head}{Ellipsis}{ext}";
    }

    private string GetUploadingSummaryText()
    {
        var uploadingFiles = _fileStates.Where(file => file.Status == LyoFileUploadStatus.Uploading).ToList();
        var currentIndex = _fileStates.Count(file
            => file.Status == LyoFileUploadStatus.Completed || file.Status == LyoFileUploadStatus.Failed || file.Status == LyoFileUploadStatus.Cancelled) + 1;

        var totalFiles = _fileStates.Count;
        var averageProgress = uploadingFiles.Any() ? uploadingFiles.Average(file => file.Progress) : 0;
        return $"Uploading {currentIndex}/{totalFiles} ({averageProgress:F0}%)";
    }

    private string GetFileTypeDescription()
    {
        if (ValidFileTypes == FileTypeFlags.All)
            return "Files";

        var types = Enum.GetValues<FileTypeFlags>()
            .Where(flag => flag != FileTypeFlags.Unknown && flag != FileTypeFlags.All && ValidFileTypes.HasFlag(flag))
            .Select(flag => flag.ToString())
            .ToList();

        if (types.Count == 0)
            return "Files";

        return types.Count == 1 ? types[0] : types.Count <= 3 ? string.Join("/", types) : "Files";
    }

    private string GetAcceptString()
    {
        if (ValidFileTypes == FileTypeFlags.All)
            return "*/*";

        var extensions = new List<string>();
        foreach (var flag in Enum.GetValues<FileTypeFlags>()) {
            if (flag is FileTypeFlags.Unknown or FileTypeFlags.All)
                continue;

            if (ValidFileTypes.HasFlag(flag))
                extensions.Add($".{flag.ToString().ToLower()}");
        }

        return extensions.Count > 0 ? string.Join(",", extensions) : "*/*";
    }

    private static LyoFileUploadEventArgs CreateEventArgs(LyoFileUploadState fileState)
        => new(fileState.FileName, fileState.FileSize, fileState.Status, fileState.Progress, fileState.ErrorMessage);

    private static Task NotifyUploadEventAsync(EventCallback<LyoFileUploadEventArgs> callback, LyoFileUploadState fileState)
        => callback.HasDelegate ? callback.InvokeAsync(CreateEventArgs(fileState)) : Task.CompletedTask;
}