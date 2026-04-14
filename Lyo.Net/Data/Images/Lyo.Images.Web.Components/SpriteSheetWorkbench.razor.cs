using Lyo.Common.Records;
using Lyo.Images.Models;
using Lyo.Images.Sprite;
using Lyo.Images.Sprite.Models;
using Lyo.Web.Components;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Images.Web.Components;

public partial class SpriteSheetWorkbench : IAsyncDisposable
{
    private const string ModuleUrl = "/_content/Lyo.Images.Web.Components/scripts/spriteSheetAnimator.js";
    private readonly List<AnimatorSheetEntry> _animatorSheets = [];
    private readonly HashSet<int> _excludedFrames = [];
    private SpriteSheetAnimateUploader? _animatorUploader;
    private int _bottomTrim;
    private bool _busy;

    private SpriteSheetCalculation _calculation = CreateEmptyCalculation();
    private bool _extractBusy;
    private int _extractFramesPerRow = 8;
    private SpriteGridPadMode _extractGridPadMode = SpriteGridPadMode.StretchedUniform;
    private double _extractLoopDurationMs;
    private ImageMetadata? _extractMetadata;
    private int _extractOffsetX;
    private int _extractOffsetY;
    private int _extractPadBottom;
    private int _extractPadLeft;
    private int _extractPadRight;
    private int _extractPadTop;
    private string? _extractPreviewDataUrl;
    private byte[]? _extractResultBytes;
    private int _extractRowCount = 1;
    private int _extractSampleFps = 60;
    private int _extractSourceFrameCount = 1;
    private string _extractStatusMessage = string.Empty;
    private Severity _extractStatusSeverity = Severity.Info;
    private int _extractStatusVersion;
    private AnimatedExtractTiming? _extractTiming;

    private LocalBrowserFile? _extractUploadedFile;
    private int _framesPerRow = 1;
    private int _framesPerSecond = 60;
    private string? _imageSource;
    private int _leftTrim;
    private ImageMetadata? _metadata;
    private IJSObjectReference? _module;
    private int _offsetX;
    private int _offsetY;
    private bool _pendingResumePlayback;
    private bool _playing;
    private ElementReference _previewCanvas;
    private bool _previewDirty;
    private bool _previewReady;
    private int _rightTrim;
    private int _rowCount = 1;
    private Guid? _selectedAnimatorSheetId;
    private int _selectedPreviewIndex;
    private int _topTrim;
    private LocalBrowserFile? _uploadedFile;

    [Inject]
    private IJsInterop Js { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private ISpriteSheetExportService SpriteSheetExport { get; set; } = null!;

    private bool CanDownloadAllFrames => _uploadedFile != null && _calculation.IncludedFrames.Count > 0 && !_busy;

    private bool CanDownloadCurrentFrame => _uploadedFile != null && SelectedFrame != null && !_busy;

    private bool CanDownloadGif => _uploadedFile != null && _calculation.IncludedFrames.Count > 0 && !_busy;

    private bool CanDownloadExtract => _extractResultBytes is { Length: > 0 } && !_extractBusy;

    private bool CanGenerateExtract => _extractUploadedFile != null && _extractMetadata != null && !_extractBusy;

    private int ExtractGridCellCount => _extractRowCount * _extractFramesPerRow;

    private int ExtractSampleBudget => SpriteSheetExportService.ComputeExtractSampleBudget(_extractSourceFrameCount, _extractLoopDurationMs, _extractSampleFps);

    private string? ExtractGridCellsHelperText {
        get {
            if (_extractTiming == null || _extractSourceFrameCount <= 1)
                return null;

            var parts = new List<string>();
            if (ExtractSampleBudget >= _extractSourceFrameCount)
                parts.Add("Using every source frame");
            else
                parts.Add("Fewer samples than frames; time-subsampled");

            if (ExtractGridCellCount > ExtractSampleBudget)
                parts.Add($"{ExtractGridCellCount - ExtractSampleBudget} duplicate(s) — {_extractGridPadMode}");

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }
    }

    private bool HasIncludedFrames => _calculation.IncludedFrames.Count > 0;

    private bool HasSpriteSheet => _uploadedFile != null && _metadata != null;

    private int MaxPreviewFrameIndex => Math.Max(0, _calculation.IncludedFrames.Count - 1);

    private SpriteFrameRect? SelectedFrame => !HasIncludedFrames ? null : _calculation.IncludedFrames[Math.Clamp(_selectedPreviewIndex, 0, MaxPreviewFrameIndex)];

    public async ValueTask DisposeAsync()
    {
        if (_module == null)
            return;

        try {
            await _module.InvokeVoidAsync("dispose", _previewCanvas);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) {
            // Ignore disposal failures when the circuit is already gone.
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleUrl);

        if (_module != null && HasSpriteSheet && !_previewReady) {
            await _module.InvokeVoidAsync("initialize", _previewCanvas);
            _previewReady = true;
            _previewDirty = true;
        }

        if (_previewReady && _previewDirty)
            await SyncPreviewAsync();

        if (_pendingResumePlayback && _module != null && _previewReady && HasIncludedFrames) {
            _pendingResumePlayback = false;
            await PlayAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task ApplySettingsAsync()
    {
        if (!HasSpriteSheet)
            return;

        await PauseIfPlayingAsync();
        RecalculateFrames();
        await SyncPreviewAsync();
    }

    private Task OnAnimatorSliceRowCountChangedAsync(int value)
    {
        _rowCount = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceFramesPerRowChangedAsync(int value)
    {
        _framesPerRow = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceOffsetXChangedAsync(int value)
    {
        _offsetX = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceOffsetYChangedAsync(int value)
    {
        _offsetY = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceLeftTrimChangedAsync(int value)
    {
        _leftTrim = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceRightTrimChangedAsync(int value)
    {
        _rightTrim = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceTopTrimChangedAsync(int value)
    {
        _topTrim = value;
        return Task.CompletedTask;
    }

    private Task OnAnimatorSliceBottomTrimChangedAsync(int value)
    {
        _bottomTrim = value;
        return Task.CompletedTask;
    }

    private async Task DownloadCurrentFrameAsync()
    {
        if (_uploadedFile == null || SelectedFrame == null)
            return;

        _busy = true;
        try {
            var frameBytes = await SpriteSheetExport.ExportFrameAsync(_uploadedFile.Content, SelectedFrame.Value);
            await Js.DownloadFile(frameBytes, $"frame-{SelectedFrame.Value.Index:D3}.png", FileTypeInfo.Png.MimeType);
        }
        catch (Exception ex) {
            ReportAnimatorError(ex.Message);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DownloadIncludedFramesAsync()
    {
        if (_uploadedFile == null || _calculation.IncludedFrames.Count == 0)
            return;

        _busy = true;
        try {
            var zipBytes = await SpriteSheetExport.ExportFramesZipAsync(_uploadedFile.Content, _calculation.Frames);
            await Js.DownloadFile(zipBytes, "spritesheet-frames.zip", FileTypeInfo.Zip.MimeType);
        }
        catch (Exception ex) {
            ReportAnimatorError(ex.Message);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DownloadGifAsync()
    {
        if (_uploadedFile == null || _calculation.IncludedFrames.Count == 0)
            return;

        _busy = true;
        try {
            var gifBytes = await SpriteSheetExport.ExportAnimatedGifAsync(_uploadedFile.Content, _calculation.Frames, _framesPerSecond);
            await Js.DownloadFile(gifBytes, "spritesheet-preview.gif", FileTypeInfo.Gif.MimeType);
        }
        catch (Exception ex) {
            ReportAnimatorError(ex.Message);
        }
        finally {
            _busy = false;
        }
    }

    private async Task OnAnimatorClientFileReadyAsync(LocalBrowserFile file)
    {
        AnimatorSheetEntry? entry = null;
        _busy = true;
        try {
            var existing = _animatorSheets.FirstOrDefault(e => FilesMatch(e.File, file));
            if (existing != null) {
                _selectedAnimatorSheetId = existing.Id;
                if (_animatorUploader != null && !ReferenceEquals(existing.File, file))
                    await _animatorUploader.RemoveClientFileAsync(file);

                if (!ReferenceEquals(_uploadedFile, existing.File))
                    await LoadAnimatorSheetCoreAsync(existing.File);

                return;
            }

            entry = new(Guid.NewGuid(), file);
            _animatorSheets.Add(entry);
            _selectedAnimatorSheetId = entry.Id;
            await LoadAnimatorSheetCoreAsync(file);
        }
        catch (Exception ex) {
            if (entry != null) {
                _animatorSheets.Remove(entry);
                if (_animatorUploader != null)
                    await _animatorUploader.RemoveClientFileAsync(file);
            }

            _selectedAnimatorSheetId = _animatorSheets.FirstOrDefault()?.Id;
            if (_animatorSheets.Count == 0)
                ResetState();
            else
                await LoadAnimatorSheetCoreAsync(_animatorSheets[0].File);

            ReportAnimatorError(ex.Message);
        }
        finally {
            _busy = false;
        }
    }

    private async Task OnAnimatorClientFileRemovedAsync(LocalBrowserFile removed)
    {
        // Match by reference only: duplicate uploads (same bytes) are removed from the uploader without a matching
        // list entry, and content-based matching would incorrectly delete an older sheet with the same file bytes.
        var entry = _animatorSheets.FirstOrDefault(e => ReferenceEquals(e.File, removed));
        if (entry == null)
            return;

        var wasSelected = _selectedAnimatorSheetId == entry.Id;
        _animatorSheets.Remove(entry);
        if (!wasSelected) {
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (_animatorSheets.Count == 0) {
            ResetState();
            await SyncPreviewAsync();
            return;
        }

        _selectedAnimatorSheetId = _animatorSheets[0].Id;
        _busy = true;
        try {
            await LoadAnimatorSheetCoreAsync(_animatorSheets[0].File);
        }
        catch (Exception ex) {
            ReportAnimatorError(ex.Message);
        }
        finally {
            _busy = false;
        }
    }

    private async Task OnAnimatorSheetSelectedAsync(Guid id)
    {
        var entry = _animatorSheets.FirstOrDefault(e => e.Id == id);
        if (entry == null)
            return;

        if (_selectedAnimatorSheetId == id && ReferenceEquals(_uploadedFile, entry.File))
            return;

        _busy = true;
        try {
            _selectedAnimatorSheetId = id;
            await LoadAnimatorSheetCoreAsync(entry.File);
        }
        catch (Exception ex) {
            ReportAnimatorError(ex.Message);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RemoveAnimatorSheetAsync(AnimatorSheetEntry entry)
    {
        if (_animatorUploader != null)
            await _animatorUploader.RemoveClientFileAsync(entry.File);
    }

    private string GetAnimatorSheetLabel(AnimatorSheetEntry entry)
    {
        var sameName = _animatorSheets.Count(e => e.File.FileName == entry.File.FileName);
        if (sameName <= 1)
            return entry.File.FileName;

        var ordinal = _animatorSheets.TakeWhile(e => e != entry).Count(e => e.File.FileName == entry.File.FileName) + 1;
        return $"{entry.File.FileName} ({ordinal})";
    }

    private async Task LoadAnimatorSheetCoreAsync(LocalBrowserFile file)
    {
        var resumePlayback = _playing;
        var savedFps = _framesPerSecond;
        _uploadedFile = file;
        _metadata = await SpriteSheetExport.GetMetadataAsync(file.Content);
        _imageSource = BuildDataUrl(file);
        _excludedFrames.Clear();
        _playing = false;
        _previewReady = false;
        ApplyDefaultSliceSettings(file.Content);
        if (resumePlayback)
            _framesPerSecond = Math.Clamp(savedFps, 1, 60);
        else {
            var (animFrames, loopMs) = await SpriteSheetExport.GetAnimatedSourceStatsAsync(file.Content);
            _framesPerSecond = SpriteSheetExportService.ComputePlaybackFpsFromAnimatedStats(animFrames, loopMs);
        }

        RecalculateFrames();
        _pendingResumePlayback = resumePlayback && HasIncludedFrames;
        await SyncPreviewAsync();
    }

    private async Task OnExtractFileReadyAsync(LocalBrowserFile file)
    {
        _extractBusy = true;
        try {
            _extractUploadedFile = file;
            _extractMetadata = await SpriteSheetExport.GetMetadataAsync(file.Content);
            _extractResultBytes = null;
            _extractPreviewDataUrl = null;
            ApplyDefaultExtractSettings();
            var (sourceFrames, loopMs) = await SpriteSheetExport.GetAnimatedSourceStatsAsync(file.Content);
            _extractSourceFrameCount = sourceFrames;
            _extractLoopDurationMs = loopMs;
            _extractSampleFps = 60;
            _extractRowCount = 1;
            RecalculateExtractGrid();
            await RefreshExtractTimingAsync();
            SetExtractStatus($"Loaded {file.FileName}.", Severity.Success);
        }
        catch (Exception ex) {
            ResetExtractState();
            SetExtractStatus(ex.Message, Severity.Error);
        }
        finally {
            _extractBusy = false;
        }
    }

    private Task OnExtractFileRemovedAsync(LocalBrowserFile _)
    {
        ResetExtractState();
        SetExtractStatus("Source file removed.", Severity.Info);
        return Task.CompletedTask;
    }

    private async Task GenerateExtractSpriteSheetAsync()
    {
        if (_extractUploadedFile == null || _extractMetadata == null)
            return;

        _extractBusy = true;
        try {
            RecalculateExtractGrid();
            var png = await SpriteSheetExport.ExportAnimatedImageToSpriteSheetPngAsync(
                _extractUploadedFile.Content, ExtractSampleBudget, _extractRowCount, _extractFramesPerRow, _extractOffsetX, _extractOffsetY, _extractPadLeft, _extractPadRight,
                _extractPadTop, _extractPadBottom, _extractGridPadMode);

            _extractResultBytes = png;
            _extractPreviewDataUrl = $"data:{FileTypeInfo.Png.MimeType};base64,{Convert.ToBase64String(png)}";
            SetExtractStatus("Spritesheet generated.", Severity.Success);
        }
        catch (Exception ex) {
            _extractResultBytes = null;
            _extractPreviewDataUrl = null;
            SetExtractStatus(ex.Message, Severity.Error);
        }
        finally {
            _extractBusy = false;
        }
    }

    private async Task DownloadExtractSpriteSheetAsync()
    {
        if (_extractResultBytes == null || _extractResultBytes.Length == 0)
            return;

        _extractBusy = true;
        try {
            await Js.DownloadFile(_extractResultBytes, "spritesheet.png", FileTypeInfo.Png.MimeType);
            SetExtractStatus("Download started.", Severity.Success);
        }
        catch (Exception ex) {
            SetExtractStatus(ex.Message, Severity.Error);
        }
        finally {
            _extractBusy = false;
        }
    }

    private void ApplyDefaultExtractSettings()
    {
        _extractSourceFrameCount = 1;
        _extractLoopDurationMs = 0;
        _extractSampleFps = 60;
        _extractRowCount = 1;
        _extractFramesPerRow = 8;
        _extractOffsetX = 0;
        _extractOffsetY = 0;
        _extractPadLeft = 0;
        _extractPadRight = 0;
        _extractPadTop = 0;
        _extractPadBottom = 0;
        _extractGridPadMode = SpriteGridPadMode.StretchedUniform;
    }

    private void ResetExtractState()
    {
        _extractUploadedFile = null;
        _extractMetadata = null;
        _extractTiming = null;
        _extractResultBytes = null;
        _extractPreviewDataUrl = null;
        ApplyDefaultExtractSettings();
    }

    private async Task RefreshExtractTimingAsync()
    {
        if (_extractUploadedFile == null) {
            _extractTiming = null;
            return;
        }

        try {
            _extractTiming = await SpriteSheetExport.GetAnimatedExtractTimingAsync(_extractUploadedFile.Content, _extractRowCount, _extractFramesPerRow);
        }
        catch {
            _extractTiming = null;
        }
    }

    private void RecalculateExtractGrid()
    {
        var budget = ExtractSampleBudget;
        var (rows, cols) = SpriteSheetExportService.FitGridToRowCount(budget, _extractRowCount);
        _extractRowCount = rows;
        _extractFramesPerRow = cols;
    }

    private async Task OnExtractRowCountChangedAsync(int value)
    {
        _extractRowCount = value;
        RecalculateExtractGrid();
        await RefreshExtractTimingAsync();
    }

    private async Task OnExtractSampleFpsChangedAsync(int value)
    {
        _extractSampleFps = Math.Clamp(value, 1, 240);
        RecalculateExtractGrid();
        await RefreshExtractTimingAsync();
    }

    private void SetExtractStatus(string message, Severity severity)
    {
        _extractStatusMessage = message;
        _extractStatusSeverity = severity;
        var version = ++_extractStatusVersion;
        Snackbar.Add(message, severity);
        _ = ClearExtractStatusLaterAsync(version);
    }

    private async Task ClearExtractStatusLaterAsync(int version)
    {
        await Task.Delay(2500);
        if (version != _extractStatusVersion)
            return;

        _extractStatusMessage = string.Empty;
        await InvokeAsync(StateHasChanged);
    }

    private async Task PauseIfPlayingAsync()
    {
        if (!_playing || _module == null)
            return;

        await _module.InvokeVoidAsync("pause", _previewCanvas);
        var idx = await _module.InvokeAsync<int>("getCurrentFrame", _previewCanvas);
        _selectedPreviewIndex = Math.Clamp(idx, 0, MaxPreviewFrameIndex);
        _playing = false;
    }

    private async Task PauseAsync()
    {
        if (_module == null || !HasIncludedFrames || !_playing)
            return;

        await PauseIfPlayingAsync();
    }

    private async Task OnFramesPerSecondChangedAsync(int value)
    {
        _framesPerSecond = Math.Clamp(value, 1, 60);
        if (_module != null && _previewReady)
            await _module.InvokeVoidAsync("setFps", _previewCanvas, _framesPerSecond);
    }

    private async Task OnPreviewFrameSliderChangedAsync(int value)
    {
        if (_playing || !HasIncludedFrames || _busy)
            return;

        _selectedPreviewIndex = Math.Clamp(value, 0, MaxPreviewFrameIndex);
        await ShowSelectedFrameAsync();
    }

    private async Task TogglePlayPauseAsync()
    {
        if (!HasIncludedFrames || _busy)
            return;

        if (_playing)
            await PauseAsync();
        else
            await PlayAsync();
    }

    private async Task PlayAsync()
    {
        if (_module == null || !HasIncludedFrames)
            return;

        await SyncPreviewAsync();
        await _module.InvokeVoidAsync("play", _previewCanvas);
        _playing = true;
    }

    private async Task PreviewFrameBySourceIndexAsync(int frameIndex)
    {
        var includedIndex = _calculation.IncludedFrames.Select((frame, index) => new { frame.Index, IncludedIndex = index })
            .FirstOrDefault(entry => entry.Index == frameIndex)
            ?.IncludedIndex;

        if (includedIndex == null)
            return;

        await PauseIfPlayingAsync();
        _selectedPreviewIndex = includedIndex.Value;
        await ShowSelectedFrameAsync();
    }

    private async Task ShowSelectedFrameAsync()
    {
        if (_module == null || !HasIncludedFrames)
            return;

        _selectedPreviewIndex = Math.Clamp(_selectedPreviewIndex, 0, MaxPreviewFrameIndex);
        await _module.InvokeVoidAsync("setFrame", _previewCanvas, _selectedPreviewIndex);
        _playing = false;
    }

    private async Task ToggleFrameAsync(int frameIndex)
    {
        await PauseIfPlayingAsync();
        if (_excludedFrames.Contains(frameIndex))
            _excludedFrames.Remove(frameIndex);
        else
            _excludedFrames.Add(frameIndex);

        RecalculateFrames();
        await SyncPreviewAsync();
    }

    /// <returns><see langword="true" /> if <c>LyoGrid</c> metadata was found in a PNG.</returns>
    private bool ApplyDefaultSliceSettings(byte[] imageBytes)
    {
        if (_metadata == null)
            return false;

        _offsetX = 0;
        _offsetY = 0;
        _leftTrim = 0;
        _topTrim = 0;
        _rightTrim = 0;
        _bottomTrim = 0;
        _rowCount = 1;
        _selectedPreviewIndex = 0;
        if (SpriteSheetExportService.TryReadSpriteSheetGridFromImageBytes(imageBytes, out var gr, out var gc)) {
            _rowCount = gr;
            _framesPerRow = gc;
            return true;
        }

        var w = _metadata.Width;
        var h = Math.Max(1, _metadata.Height);
        _framesPerRow = w > 0 && w % h == 0 ? w / h : 1;
        return false;
    }

    private static string BuildDataUrl(LocalBrowserFile file) => $"data:{GetMimeType(file.FileName)};base64,{Convert.ToBase64String(file.Content)}";

    private static SpriteSheetCalculation CreateEmptyCalculation()
        => new() {
            Frames = [],
            IncludedFrames = [],
            SourceWidth = 0,
            SourceHeight = 0,
            FrameWidth = 1,
            FrameHeight = 0,
            RowCount = 1,
            ColumnCount = 0,
            FramesPerSecond = 60,
            OffsetX = 0,
            OffsetY = 0,
            LeftTrim = 0,
            TopTrim = 0,
            RightTrim = 0,
            BottomTrim = 0,
            MaxFrameCount = 0
        };

    private static bool FilesMatch(LocalBrowserFile a, LocalBrowserFile b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a.FileName != b.FileName)
            return false;

        return a.Content.AsSpan().SequenceEqual(b.Content);
    }

    private static string GetMimeType(string fileName) => FileTypeInfo.FromFilePath(fileName).MimeType;

    private void RecalculateFrames()
    {
        if (_metadata == null) {
            _calculation = CreateEmptyCalculation();
            _selectedPreviewIndex = 0;
            _previewDirty = true;
            return;
        }

        _calculation = SpriteSheetCalculator.Calculate(
            new() {
                SourceWidth = _metadata.Width,
                SourceHeight = _metadata.Height,
                OffsetX = _offsetX,
                OffsetY = _offsetY,
                LeftTrim = _leftTrim,
                TopTrim = _topTrim,
                RightTrim = _rightTrim,
                BottomTrim = _bottomTrim,
                RowCount = _rowCount,
                FramesPerRow = _framesPerRow,
                FramesPerSecond = _framesPerSecond,
                ExcludedFrames = _excludedFrames
            });

        _rowCount = Math.Max(1, _calculation.RowCount);
        _framesPerRow = Math.Max(1, _calculation.ColumnCount);
        if (_calculation.IncludedFrames.Count == 0) {
            _selectedPreviewIndex = 0;
            _playing = false;
        }
        else
            _selectedPreviewIndex = Math.Clamp(_selectedPreviewIndex, 0, _calculation.IncludedFrames.Count - 1);

        _previewDirty = true;
    }

    private void ResetState()
    {
        _selectedAnimatorSheetId = null;
        _uploadedFile = null;
        _metadata = null;
        _imageSource = null;
        _excludedFrames.Clear();
        _offsetX = 0;
        _offsetY = 0;
        _leftTrim = 0;
        _topTrim = 0;
        _rightTrim = 0;
        _bottomTrim = 0;
        _rowCount = 1;
        _framesPerRow = 1;
        _framesPerSecond = 60;
        _selectedPreviewIndex = 0;
        _playing = false;
        _previewReady = false;
        _calculation = CreateEmptyCalculation();
        _previewDirty = true;
        _pendingResumePlayback = false;
    }

    private void ReportAnimatorError(string message) => Snackbar.Add(message, Severity.Error);

    private async Task SyncPreviewAsync()
    {
        if (_module == null || !_previewReady)
            return;

        await _module.InvokeVoidAsync(
            "setSpriteSheet", _previewCanvas, new {
                source = _imageSource,
                frames = _calculation.IncludedFrames.Select(frame => new {
                        index = frame.Index,
                        x = frame.X,
                        y = frame.Y,
                        width = frame.Width,
                        height = frame.Height
                    })
                    .ToArray(),
                fps = Math.Clamp(_framesPerSecond, 1, 60),
                currentFrame = _selectedPreviewIndex
            });

        if (!_playing)
            await _module.InvokeVoidAsync("pause", _previewCanvas);

        _previewDirty = false;
    }
}