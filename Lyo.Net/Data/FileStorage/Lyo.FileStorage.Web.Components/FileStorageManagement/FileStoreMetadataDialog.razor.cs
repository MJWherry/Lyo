using Lyo.FileMetadataStore.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreMetadataDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public FileStoreResult? Metadata { get; set; }

    private void Close() => MudDialog.Close();
}
