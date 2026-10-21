using System.Text;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Lyo.Web.WebRenderer.Models;
using MudBlazor;

namespace Lyo.Pdf.Web.Components;

public partial class HtmlToPdfWorkbench
{
    private bool _busy;
    private CancellationTokenSource? _convertCts;
    private WebRenderFormat _format = WebRenderFormat.Pdf;

    private string _inputSource = "Editor";
    private string _orientation = "Portrait";
    private byte[]? _output;
    private string _paper = "Letter";
    private string? _previewUrl;
    private string _uploadStatus = "No HTML file uploaded.";

    private string OutputFileName
        => _format switch {
            WebRenderFormat.Png => "html-capture.png",
            WebRenderFormat.Jpeg => "html-capture.jpg",
            WebRenderFormat.WebP => "html-capture.webp",
            var _ => "html-to-pdf.pdf"
        };

    private string OutputContentType
        => _format switch {
            WebRenderFormat.Png => FileTypeInfo.Png.MimeType,
            WebRenderFormat.Jpeg => FileTypeInfo.Jpeg.MimeType,
            WebRenderFormat.WebP => FileTypeInfo.Webp.MimeType,
            var _ => FileTypeInfo.Pdf.MimeType
        };

    public void Dispose()
    {
        _convertCts?.Cancel();
        _convertCts?.Dispose();
    }

    private Task OnHtmlFileReadyAsync(LocalBrowserFile file)
    {
        _htmlContent = Encoding.UTF8.GetString(file.Content);
        _inputSource = file.FileName;
        _output = null;
        _previewUrl = null;
        SetStatus($"{file.FileName} loaded.", Severity.Success);
        return Task.CompletedTask;
    }

    private Task OnHtmlFileRemovedAsync(LocalBrowserFile file)
    {
        _inputSource = "Editor";
        SetStatus($"{file.FileName} removed.", Severity.Info);
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

    private async Task ConvertAsync()
    {
        if (string.IsNullOrWhiteSpace(_htmlContent)) {
            SetStatus("Enter HTML or upload an HTML file.", Severity.Warning);
            return;
        }

        _convertCts?.Cancel();
        _convertCts?.Dispose();
        _convertCts = new();
        _busy = true;
        try {
            var request = new WebRenderReq {
                Format = _format,
                Paper = _paper,
                Landscape = string.Equals(_orientation, "Landscape", StringComparison.OrdinalIgnoreCase),
                PreferCssPageSize = string.Equals(_paper, "CSS", StringComparison.OrdinalIgnoreCase),
                Quality = _format is WebRenderFormat.Jpeg or WebRenderFormat.WebP ? 80 : null
            };

            using var buffer = new MemoryStream();
            await WebRendererService.CopyCaptureToAsync(_htmlContent, buffer, request, _convertCts.Token);
            _output = buffer.ToArray();
            _previewUrl = "data:" + OutputContentType + ";base64," + Convert.ToBase64String(_output);
            SetStatus("Capture generated.", Severity.Success);
        }
        catch (Exception ex) {
            _output = null;
            _previewUrl = null;
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DownloadAsync()
    {
        if (_output == null)
            return;

        await Js.DownloadFile(_output, OutputFileName, OutputContentType);
    }
}