using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreRenameDialog
{
    private string _originalFileName = string.Empty;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string? InitialOriginalFileName { get; set; }

    protected override void OnParametersSet() => _originalFileName = InitialOriginalFileName ?? string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(_originalFileName))
            return;

        MudDialog.Close(DialogResult.Ok(new FileStoreRenameDialogResult(_originalFileName.Trim())));
    }
}