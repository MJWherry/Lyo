using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileMetadata;

public partial class FileHtmlPreviewDialog
{
    [Parameter]
    public string? Html { get; set; }
}
