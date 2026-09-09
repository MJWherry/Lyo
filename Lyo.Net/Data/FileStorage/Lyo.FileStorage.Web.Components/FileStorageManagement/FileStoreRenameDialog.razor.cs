using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreRenameDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string? InitialOriginalFileName { get; set; }

    private string _originalFileName = string.Empty;

    protected override void OnParametersSet() => _originalFileName = InitialOriginalFileName ?? string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(_originalFileName))
            return;

        MudDialog.Close(DialogResult.Ok(new FileStoreRenameDialogResult(_originalFileName.Trim())));
    }
}
