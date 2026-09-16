namespace Lyo.Law.Web.Components;

/// <summary>Reads one statute node and saves heading, body, and notes through the source-key upsert.</summary>
public partial class LawNodeView
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Typed client for detail, children, and upsert.</summary>
    [Parameter]
    [EditorRequired]
    public ILawClient LawClient { get; set; } = null!;

    /// <summary>Node to load.</summary>
    [Parameter]
    [EditorRequired]
    public Guid NodeId { get; set; }

    private LawNodeDetailRes? _detail;
    private List<LawNodeChildRes> _children = [];
    private List<LawAnnotationReq> _annotations = [];
    private string _heading = string.Empty;
    private string _label = string.Empty;
    private string? _citation;
    private string? _body;
    private bool _repealed;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _detail = await LawClient.GetNodeAsync(NodeId);
        if (_detail is null)
            return;

        _heading = _detail.Heading;
        _label = _detail.Label;
        _citation = _detail.Citation;
        _body = _detail.Body;
        _repealed = _detail.IsRepealed;
        _annotations = _detail.Annotations.Select(a => new LawAnnotationReq {
            Kind = a.Kind,
            Ordinal = a.Ordinal,
            Body = a.Body,
            RelatedCitation = a.RelatedCitation,
            RelatedNodeId = a.RelatedNodeId
        }).ToList();
        _children = (await LawClient.GetChildrenAsync(NodeId)).ToList();
    }

    private void AddNote() => _annotations.Add(new LawAnnotationReq { Kind = LawAnnotationKind.History, Ordinal = _annotations.Count, Body = string.Empty });

    private async Task Save()
    {
        if (_detail is null)
            return;

        for (var i = 0; i < _annotations.Count; i++)
            _annotations[i].Ordinal = i;

        var result = await LawClient.UpsertBySourceAsync(new LawNodeUpsertReq {
            JurisdictionKey = _detail.JurisdictionKey,
            CodeKey = _detail.CodeKey,
            SourceKey = _detail.SourceKey,
            ParentSourceKey = _detail.ParentSourceKey,
            Kind = _detail.Kind,
            SortOrder = _detail.SortOrder,
            Label = _label,
            Heading = _heading,
            Body = _body,
            Citation = _citation,
            IsRepealed = _repealed,
            SourceUrl = _detail.SourceUrl,
            Annotations = _annotations.Where(a => !string.IsNullOrWhiteSpace(a.Body)).ToList()
        });
        Snackbar.Add(result.Unchanged ? "No changes" : "Saved", Severity.Success);
        if (!result.Unchanged)
            MudDialog.Close(DialogResult.Ok(true));
    }

    private void Close() => MudDialog.Close(DialogResult.Cancel());
}
