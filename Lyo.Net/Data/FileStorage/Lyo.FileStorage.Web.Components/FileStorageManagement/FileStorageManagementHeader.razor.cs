using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStorageManagementHeader
{
    [Parameter]
    public string Title { get; set; } = "File Storage";

    [Parameter]
    public string Description { get; set; } = "";
}
