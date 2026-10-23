using System.Text.Json;
using Lyo.Json;

namespace Lyo.Story.Web.Components;

/// <summary>Library of story graphs for one tenant and game. Opens <see cref="StoryBuilderWorkbench" />.</summary>
public partial class StoryManagement
{
    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();
    private StoryDraft? _open;

    private List<StoryGraphRes> _stories = [];

    /// <summary>Client for graph list, save, and delete.</summary>
    [Parameter]
    [EditorRequired]
    public IStoryClient StoryClient { get; set; } = null!;

    /// <summary>Route prefix the host mounted the Story API on.</summary>
    [Parameter]
    public string BaseRoute { get; set; } = "Story";

    /// <summary>Tenant whose stories are listed. Null lists stories with no tenant.</summary>
    [Parameter]
    public Guid? TenantId { get; set; }

    /// <summary>Game filter. Null lists every game in the tenant.</summary>
    [Parameter]
    public string? GameKey { get; set; }

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try {
            _stories = [.. await StoryClient.ListGraphsAsync(new() { TenantId = TenantId, GameKey = GameKey })];
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private void Create()
        => _open = new() {
            Title = "Untitled",
            Status = StoryStatus.Draft,
            TenantId = TenantId,
            GameKey = GameKey,
            StartNodeKey = "start",
            Nodes = [new() { Key = "start" }]
        };

    private async Task OpenAsync(Guid id)
    {
        try {
            var graph = await StoryClient.GetGraphAsync(id);
            if (graph is null) {
                Snackbar.Add("That story was not found.", Severity.Warning);
                return;
            }

            _open = StoryDraftMap.FromGraph(graph);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task DuplicateAsync(Guid id)
    {
        try {
            var graph = await StoryClient.GetGraphAsync(id);
            if (graph is null)
                return;

            var draft = StoryDraftMap.FromGraph(graph);
            draft.Id = Guid.Empty;
            draft.Title += " copy";
            draft.Slug += "-copy";
            draft.Status = StoryStatus.Draft;
            var saved = await StoryClient.SaveGraphAsync(StoryDraftMap.ToRequest(draft));
            Snackbar.Add("Duplicated " + saved.Title + ".", Severity.Success);
            await ReloadAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task DeleteAsync(Guid id)
    {
        try {
            await StoryClient.DeleteAsync(id);
            await ReloadAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private void Close() => _open = null;

    private async Task OnSaved(StoryGraphRes saved)
    {
        if (_open is not null)
            _open.Id = saved.Id;

        await ClientStore.SetStoryBuilderStateAsync(JsonSerializer.Serialize(StoryDraftMap.ToRequest(_open!), JsonOptions));
        await ReloadAsync();
    }
}