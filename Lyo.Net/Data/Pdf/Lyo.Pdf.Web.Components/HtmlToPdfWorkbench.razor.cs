using System.Net.Http.Json;
using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Lyo.Web.WebRenderer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Pdf.Web.Components;

public partial class HtmlToPdfWorkbench
{
    private bool _busy;

    private string _inputSource = "Editor";
    private byte[]? _pdfBytes;
    private readonly string _pdfFileName = "html-to-pdf.pdf";
    private string _uploadStatus = "No HTML file uploaded.";

    private Task OnHtmlFileReadyAsync(LocalBrowserFile file)
    {
        _htmlContent = Encoding.UTF8.GetString(file.Content);
        _inputSource = file.FileName;
        _pdfBytes = null;
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

        _busy = true;
        try {
            _pdfBytes = await WebRendererService.ConvertHtmlToPdfAsync(_htmlContent);
            SetStatus("PDF generated.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DownloadAsync()
    {
        if (_pdfBytes == null)
            return;

        await Js.DownloadFile(_pdfBytes, _pdfFileName, FileTypeInfo.Pdf.MimeType);
    }
}
