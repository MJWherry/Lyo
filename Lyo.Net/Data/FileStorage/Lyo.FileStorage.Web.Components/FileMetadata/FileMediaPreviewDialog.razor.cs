using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileMetadata;

/// <summary>Plays or shows one stored image, audio, or video file from its inline download URL.</summary>
public partial class FileMediaPreviewDialog
{
    /// <summary>Which viewer to render.</summary>
    [Parameter]
    public FileMediaPreviewKind Kind { get; set; }

    /// <summary>Absolute inline download URL.</summary>
    [Parameter]
    public string? Src { get; set; }

    /// <summary>Original file name, used as the image alt text.</summary>
    [Parameter]
    public string? FileName { get; set; }
}