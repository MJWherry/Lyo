using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreRotateDekDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public int FileCount { get; set; }

    [Parameter]
    public IReadOnlyList<string>? KeyIds { get; set; }

    private string _targetKeyId = string.Empty;
    private string _targetKeyVersion = string.Empty;
    private int _batchSize = 100;

    private void Cancel() => MudDialog.Cancel();

    private void Confirm()
        => MudDialog.Close(
            DialogResult.Ok(
                new FileStoreRotateDekDialogResult(
                    string.IsNullOrWhiteSpace(_targetKeyId) ? null : _targetKeyId, string.IsNullOrWhiteSpace(_targetKeyVersion) ? null : _targetKeyVersion, _batchSize)));
}
