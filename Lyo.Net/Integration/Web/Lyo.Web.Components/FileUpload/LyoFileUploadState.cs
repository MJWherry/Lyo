using Lyo.Api.Models.Common.Response;
using Lyo.Web.Components.Models;

namespace Lyo.Web.Components.FileUpload;

internal sealed class LyoFileUploadState
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public LyoFileUploadStatus Status { get; set; }

    public double Progress { get; set; }

    public string? ErrorMessage { get; set; }

    public FileUploadRes? UploadResult { get; set; }

    public LocalBrowserFile? ClientFile { get; set; }

    public LocalBrowserFilePath? ClientFilePath { get; set; }

    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
}

public enum LyoFileUploadStatus
{
    Pending,
    Uploading,
    Completed,
    Failed,
    Cancelled
}

public sealed record LyoFileUploadEventArgs(string FileName, long FileSize, LyoFileUploadStatus Status, double Progress, string? ErrorMessage = null);