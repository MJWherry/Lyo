using Lyo.Note;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Nested notes for one inventory subject.</summary>
public partial class HomeNotesPanel
{
    private string _draft = "";

    private List<NoteRecord> _notes = [];

    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Full notes collection route, e.g. <c>HomeInventory/Items/{id}/Notes</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string NotesRoute { get; set; } = "";

    protected override Task OnParametersSetAsync() => Reload();

    private async Task Reload()
    {
        var rows = await ApiClient.GetAsAsync<List<NoteRecord>>(NotesRoute);
        _notes = rows ?? [];
    }

    private async Task Add()
    {
        await ApiClient.PostAsAsync<HomeNoteCreateReq, NoteRecord>(NotesRoute, new() { Content = _draft });
        _draft = "";
        await Reload();
    }

    private async Task Delete(Guid noteId)
    {
        await ApiClient.DeleteAsAsync<object>($"{NotesRoute}/{noteId}");
        await Reload();
    }
}