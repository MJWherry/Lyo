using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileMetadata;

public partial class FileTabularPreviewDialog
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<FileTabularPreviewSheet> Sheets { get; set; } = [];
}
