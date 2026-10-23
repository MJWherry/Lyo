using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Recipe.Models.Enums;
using Lyo.Web.Components.Models;

namespace Lyo.Recipe.Web.Components;

/// <summary>FileStorage attachments for one recipe subject. Upload accepts any content type.</summary>
public partial class RecipeAttachmentPanel
{
    private RecipeAttachmentRole _role = RecipeAttachmentRole.Document;

    private List<RecipeAttachmentRes> _rows = [];

    /// <summary>HTTP client for list and delete.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for multipart upload.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

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
    public string EntityType { get; set; } = "";

    /// <summary>Subject id.</summary>
    [Parameter]
    public Guid EntityId { get; set; }

    protected override Task OnParametersSetAsync() => Reload();

    private string DownloadUrl(Guid fileId, bool inline) => RecipeFileLinks.DownloadUrl(FileStorageApiRoutePrefix, ApiOrigin, fileId, inline);

    private async Task Reload()
    {
        var res = await ApiClient.QueryConcreteAsync<QueryRes<RecipeAttachmentRes>>(AttachmentsRoute, new() { Amount = 200 });
        _rows = (res?.Items ?? []).Where(a => a.EntityId == EntityId && a.EntityType == EntityType).ToList();
    }

    private async Task OnClientFileReadyAsync(LocalBrowserFile file)
    {
        if (EntityId == Guid.Empty)
            return;

        await using var stream = new MemoryStream(file.Content, false);
        await RecipeClient.UploadAttachmentAsync(EntityType, EntityId, stream, file.FileName, _role, file.FileName);
        await Reload();
    }

    private async Task SaveCaption(RecipeAttachmentRes row, string? caption)
    {
        row.Caption = caption;
        await Update(row);
    }

    private async Task SavePrimary(RecipeAttachmentRes row, bool isPrimary)
    {
        row.IsPrimary = isPrimary;
        await Update(row);
    }

    private async Task Update(RecipeAttachmentRes row)
    {
        var req = new RecipeAttachmentReq {
            EntityType = row.EntityType,
            EntityId = row.EntityId,
            FileId = row.FileId,
            Role = row.Role,
            Caption = row.Caption,
            IsPrimary = row.IsPrimary,
            SortOrder = row.SortOrder
        };

        await ApiClient.PostAsAsync<UpdateRequest<RecipeAttachmentReq>, UpdateResult<RecipeAttachmentRes>>($"{AttachmentsRoute}/Update", new(req, row.Id));
        await Reload();
    }

    private async Task Delete(Guid id)
    {
        await ApiClient.DeleteAsAsync<object>($"{AttachmentsRoute}/{id}");
        await Reload();
    }
}