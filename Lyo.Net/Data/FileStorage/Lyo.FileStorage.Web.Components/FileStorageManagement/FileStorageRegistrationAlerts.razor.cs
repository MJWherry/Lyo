using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStorageRegistrationAlerts
{
    [Parameter]
    public bool ApiHealthy { get; set; }

    [Parameter]
    public bool StorageHealthy { get; set; }

    [Parameter]
    public string ApiHealthDescription { get; set; } = "";

    private Severity Severity => !ApiHealthy ? Severity.Warning : StorageHealthy ? Severity.Success : Severity.Info;
}
