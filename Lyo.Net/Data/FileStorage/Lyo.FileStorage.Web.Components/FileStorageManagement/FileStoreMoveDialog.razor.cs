using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreMoveDialog
{
    private string _pathPrefix = string.Empty;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string? InitialPathPrefix { get; set; }

    protected override void OnParametersSet() => _pathPrefix = InitialPathPrefix ?? string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private void Confirm() => MudDialog.Close(DialogResult.Ok(new FileStorePathPrefixDialogResult(string.IsNullOrWhiteSpace(_pathPrefix) ? null : _pathPrefix.Trim())));
}