using System.Net.Http.Json;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Images.Models;
using Lyo.Result;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using ResizeMode = Lyo.Images.Models.ResizeMode;

namespace Lyo.Images.Web.Components;

public partial class ImageWorkbench
{
    private static readonly ImageFormat[] SupportedOutputFormats = [ImageFormat.Png, ImageFormat.Jpeg];

    private LocalBrowserFile? _uploadedFile;
    private ImageMetadata? _metadata;
    private byte[]? _originalBytes;
    private byte[]? _currentBytes;
    private string? _previewDataUrl;
    private bool _busy;

    private int _paletteColorCount = 8;
    private readonly List<string> _paletteColors = [];

    private int _resizeWidth = 256;
    private int _resizeHeight = 256;
    private ResizeMode _resizeMode = ResizeMode.Max;
    private ImageFormat _resizeFormat = ImageFormat.Png;
    private int _resizeQuality = 90;
    private int _cropX;
    private int _cropY;
    private int _cropWidth = 64;
    private int _cropHeight = 64;
    private ImageFormat _cropFormat = ImageFormat.Png;
    private int _cropQuality = 90;
    private float _rotateDegrees;
    private ImageFormat _rotateFormat = ImageFormat.Png;
    private int _rotateQuality = 90;
    private bool _liveRotate = true;
    private ImageFormat _compressFormat = ImageFormat.Jpeg;
    private int _compressQuality = 80;
    private ImageFormat _convertFormat = ImageFormat.Png;
    private int _convertQuality = 90;


    private bool HasImage => _currentBytes is { Length: > 0 };

    private bool CanReset => _originalBytes != null && _currentBytes != null && !_originalBytes.AsSpan().SequenceEqual(_currentBytes);

    private async Task OnClientFileReadyAsync(LocalBrowserFile file)
    {
        _busy = true;
        try {
            _uploadedFile = file;
            _originalBytes = file.Content.ToArray();
            _currentBytes = file.Content.ToArray();
            _paletteColors.Clear();
            await RefreshPreviewAndMetadataAsync();
            ApplyDefaultEditValues();
            SetStatus($"Loaded {file.FileName}.", Severity.Success);
        }
        catch (Exception ex) {
            ResetState();
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task OnClientFileRemovedAsync(LocalBrowserFile _)
    {
        ResetState();
        SetStatus("Image removed.", Severity.Info);
        return Task.CompletedTask;
    }

    private async Task ExtractPaletteAsync()
    {
        if (!HasImage)
            return;

        _busy = true;
        try {
            await using var input = new MemoryStream(_currentBytes!, false);
            var result = await ImageService.GetPaletteAsync(input, Math.Clamp(_paletteColorCount, 1, 256));
            ThrowIfFailed(result, "Failed to extract palette.");
            _paletteColors.Clear();
            _paletteColors.AddRange(result.Data!.Colors);
            SetStatus($"Extracted {_paletteColors.Count} colors.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task ApplyResizeAsync()
        => ApplyOperationAsync(
            async (input, output) => {
                var result = await ImageService.ResizeAsync(input, output, Math.Max(1, _resizeWidth), Math.Max(1, _resizeHeight), _resizeMode, _resizeFormat, Math.Clamp(_resizeQuality, 1, 100));
                ThrowIfFailed(result, "Resize failed.");
            }, "Resize applied.");

    private Task ApplyCropAsync()
        => ApplyOperationAsync(
            async (input, output) => {
                var result = await ImageService.CropAsync(input, output, Math.Max(0, _cropX), Math.Max(0, _cropY), Math.Max(1, _cropWidth), Math.Max(1, _cropHeight), _cropFormat, Math.Clamp(_cropQuality, 1, 100));
                ThrowIfFailed(result, "Crop failed.");
            }, "Crop applied.");

    private Task ApplyRotateAsync() => ApplyRotateByDegreesAsync(_rotateDegrees, "Rotation applied.");

    private Task ApplyCompressAsync()
        => ApplyOperationAsync(
            async (input, output) => {
                var result = await ImageService.CompressAsync(input, output, Math.Clamp(_compressQuality, 1, 100), _compressFormat);
                ThrowIfFailed(result, "Compress failed.");
            }, "Compression applied.");

    private Task ApplyConvertFormatAsync()
        => ApplyOperationAsync(
            async (input, output) => {
                var result = await ImageService.ConvertFormatAsync(input, output, _convertFormat, Math.Clamp(_convertQuality, 1, 100));
                ThrowIfFailed(result, "Format conversion failed.");
            }, "Format conversion applied.");

    private async Task OnRotateDegreesChangedAsync(float value)
    {
        var previous = _rotateDegrees;
        _rotateDegrees = value;
        if (!_liveRotate || !HasImage || _busy)
            return;

        var delta = value - previous;
        if (Math.Abs(delta) < 0.001f)
            return;

        await ApplyRotateByDegreesAsync(delta, $"Live rotation: {value:0.##} deg");
    }

    private Task ApplyRotateByDegreesAsync(float degrees, string successMessage)
        => ApplyOperationAsync(
            async (input, output) => {
                var result = await ImageService.RotateAsync(input, output, degrees, _rotateFormat, Math.Clamp(_rotateQuality, 1, 100));
                ThrowIfFailed(result, "Rotate failed.");
            }, successMessage, false);

    private async Task ApplyOperationAsync(Func<Stream, Stream, Task> operation, string successMessage, bool clearPalette = true)
    {
        if (!HasImage)
            return;

        _busy = true;
        try {
            await using var input = new MemoryStream(_currentBytes!, false);
            await using var output = new MemoryStream();
            await operation(input, output);
            _currentBytes = output.ToArray();
            if (clearPalette)
                _paletteColors.Clear();

            await RefreshPreviewAndMetadataAsync();
            SetStatus(successMessage, Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task ResetToOriginalAsync()
    {
        if (_originalBytes == null)
            return;

        _currentBytes = _originalBytes.ToArray();
        _rotateDegrees = 0;
        _paletteColors.Clear();
        await RefreshPreviewAndMetadataAsync();
        SetStatus("Reset to original image.", Severity.Success);
    }

    private async Task DownloadCurrentAsync()
    {
        if (!HasImage)
            return;

        var fileName = BuildDownloadFileName();
        await Js.DownloadFile(_currentBytes!, fileName, GetMimeType(_metadata?.Format ?? ImageFormat.Png));
    }

    private async Task RefreshPreviewAndMetadataAsync()
    {
        if (!HasImage) {
            _metadata = null;
            _previewDataUrl = null;
            return;
        }

        await using var stream = new MemoryStream(_currentBytes!, false);
        var metadataResult = await ImageService.GetMetadataAsync(stream);
        ThrowIfFailed(metadataResult, "Failed to read metadata.");
        _metadata = metadataResult.Data!;
        _previewDataUrl = $"data:{GetMimeType(_metadata.Format)};base64,{Convert.ToBase64String(_currentBytes!)}";
    }

    private static void ThrowIfFailed<T>(Result<T> result, string fallbackMessage)
    {
        if (result.IsSuccess && result.Data != null)
            return;

        var message = result.Errors == null || result.Errors.Count == 0 ? fallbackMessage : string.Join(Environment.NewLine, result.Errors.Where(error => !string.IsNullOrWhiteSpace(error.Message)).Select(error => error.Message));
        throw new InvalidOperationException(message);
    }

    private string BuildDownloadFileName()
    {
        var originalName = _uploadedFile?.FileName ?? "image";
        var baseName = Path.GetFileNameWithoutExtension(originalName);
        var extension = GetExtension(_metadata?.Format ?? ImageFormat.Png);
        return $"{baseName}-edited.{extension}";
    }

    private static string GetExtension(ImageFormat format)
        => format switch {
            ImageFormat.Png => "png",
            ImageFormat.Jpeg => "jpg",
            ImageFormat.Gif => "gif",
            ImageFormat.Bmp => "bmp",
            ImageFormat.WebP => "webp",
            ImageFormat.Tiff => "tiff",
            ImageFormat.Ico => "ico",
            var _ => "png"
        };

    private static string GetMimeType(ImageFormat format)
        => format switch {
            ImageFormat.Png => FileTypeInfo.Png.MimeType,
            ImageFormat.Jpeg => FileTypeInfo.Jpeg.MimeType,
            ImageFormat.Gif => FileTypeInfo.Gif.MimeType,
            ImageFormat.Bmp => FileTypeInfo.Bmp.MimeType,
            ImageFormat.WebP => FileTypeInfo.Webp.MimeType,
            ImageFormat.Tiff => FileTypeInfo.Tiff.MimeType,
            ImageFormat.Ico => FileTypeInfo.Ico.MimeType,
            var _ => FileTypeInfo.Unknown.MimeType
        };

    private void ApplyDefaultEditValues()
    {
        if (_metadata == null)
            return;

        _resizeWidth = Math.Max(1, _metadata.Width);
        _resizeHeight = Math.Max(1, _metadata.Height);
        _cropX = 0;
        _cropY = 0;
        _cropWidth = Math.Max(1, _metadata.Width / 2);
        _cropHeight = Math.Max(1, _metadata.Height / 2);
        _rotateDegrees = 0;
    }

    private void ResetState()
    {
        _uploadedFile = null;
        _metadata = null;
        _originalBytes = null;
        _currentBytes = null;
        _previewDataUrl = null;
        _paletteColors.Clear();
    }
}
