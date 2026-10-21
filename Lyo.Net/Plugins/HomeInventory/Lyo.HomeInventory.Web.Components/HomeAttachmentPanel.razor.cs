using System.Text.Json;
using Lyo.Api.Models.Common.Response;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.Web.Components.Models;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>FileStorage attachments for one inventory subject.</summary>
public partial class HomeAttachmentPanel
{
    private List<HomeAttachmentRes> _rows = [];

    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Attachment CRUD route.</summary>
    [Parameter]
    [EditorRequired]
    public string AttachmentsRoute { get; set; } = "";

    /// <summary>FileStorage MapGroup prefix.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>Origin used to build download URLs when <see cref="AttachmentsRoute" /> is absolute.</summary>
    [Parameter]
    public string ApiOrigin { get; set; } = "";

    /// <summary>Subject type.</summary>
    [Parameter]
    public string EntityType { get; set; } = HomeInventoryEntityTypes.Item;

    /// <summary>Subject id.</summary>
    [Parameter]
    public Guid EntityId { get; set; }

    protected override Task OnParametersSetAsync() => Reload();

    private string DownloadUrl(Guid fileId, bool inline)
    {
        var prefix = FileStorageApiRoutePrefix.Trim().Trim('/');
        var origin = string.IsNullOrWhiteSpace(ApiOrigin) ? "" : ApiOrigin.TrimEnd('/');
        var path = $"{prefix}/files/{fileId:D}/download";
        if (inline)
            path += "?inline=true";

        return string.IsNullOrWhiteSpace(origin) ? path : $"{origin}/{path}";
    }

    private async Task Reload()
    {
        var res = await ApiClient.QueryConcreteAsync<QueryRes<HomeAttachmentRes>>(AttachmentsRoute, new() { Amount = 200 });
        _rows = (res?.Items ?? []).Where(a => a.EntityId == EntityId && a.EntityType == EntityType).ToList();
    }

    private async Task OnClientFileReadyAsync(LocalBrowserFile file)
    {
        if (EntityId == Guid.Empty)
            return;

        var prefix = FileStorageApiRoutePrefix.Trim().Trim('/');
        var origin = string.IsNullOrWhiteSpace(ApiOrigin) ? "" : ApiOrigin.TrimEnd('/');
        var pathPrefix = Uri.EscapeDataString(HomeInventoryFilePaths.ForEntity(EntityType, EntityId));
        var uri =
            $"{(string.IsNullOrWhiteSpace(origin) ? "" : origin + "/")}{prefix}/files/save-stream?originalFileName={Uri.EscapeDataString(file.FileName)}&pathPrefix={pathPrefix}";

        await using var stream = new MemoryStream(file.Content, false);
        var stored = await ApiClient.PostFileAsAsync<JsonElement>(uri, stream, file.FileName);
        var fileId = stored.TryGetProperty("id", out var idEl) ? idEl.GetGuid() : stored.GetProperty("Id").GetGuid();
        await ApiClient.PostAsAsync<HomeAttachmentReq, CreateResult<HomeAttachmentRes>>(
            $"{AttachmentsRoute}", new() {
                EntityType = EntityType,
                EntityId = EntityId,
                FileId = fileId,
                Role = HomeAttachmentRole.Photo,
                Caption = file.FileName
            });

        await Reload();
    }

    private async Task Delete(Guid id)
    {
        await ApiClient.DeleteAsAsync<object>($"{AttachmentsRoute}/{id}");
        await Reload();
    }
}