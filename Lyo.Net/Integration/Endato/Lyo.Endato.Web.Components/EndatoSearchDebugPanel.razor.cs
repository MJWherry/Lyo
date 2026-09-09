using System.Text.Json;
using Lyo.Endato.Client.Models.Enrichment.Request;
using Lyo.Endato.Client.Models.Enrichment.Response;
using Lyo.Endato.Client.Models.Person.Request;
using Lyo.Endato.Client.Models.Person.Response;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Endato.Web.Components;

public partial class EndatoSearchDebugPanel
{
    [Parameter]
    public EndatoSearchMode SearchMode { get; set; }

    [Parameter]
    [EditorRequired]
    public PersonQuery PersonQuery { get; set; } = new();

    [Parameter]
    public EventCallback<PersonQuery> PersonQueryChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public EnrichmentQuery EnrichmentQuery { get; set; } = new();

    [Parameter]
    public EventCallback<EnrichmentQuery> EnrichmentQueryChanged { get; set; }

    [Parameter]
    public PersonQueryResponse? PersonResponse { get; set; }

    [Parameter]
    public EnrichmentResponse? EnrichmentResponse { get; set; }

    [Parameter]
    public EventCallback OnRunRequested { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public long? LastElapsedMs { get; set; }

    private bool _busy;
    private string? _requestParseError;
    private int _responseEditorKey;
    private JsonElement _responseJson;

    protected override void OnParametersSet()
    {
        _busy = Busy;
        if (SearchMode == EndatoSearchMode.Person && PersonResponse != null)
            _responseJson = JsonSerializer.SerializeToElement(PersonResponse, JsonOptions);
        else if (SearchMode == EndatoSearchMode.Enrichment && EnrichmentResponse != null)
            _responseJson = JsonSerializer.SerializeToElement(EnrichmentResponse, JsonOptions);
    }

    private Task OnPersonQueryChanged(PersonQuery query) => PersonQueryChanged.HasDelegate ? PersonQueryChanged.InvokeAsync(query) : Task.CompletedTask;

    private Task OnEnrichmentQueryChanged(EnrichmentQuery query) => EnrichmentQueryChanged.HasDelegate ? EnrichmentQueryChanged.InvokeAsync(query) : Task.CompletedTask;

    private Task OnRequestParseErrorChanged(string? error)
    {
        _requestParseError = error;
        return Task.CompletedTask;
    }

    private async Task RunAsync()
    {
        if (!string.IsNullOrWhiteSpace(_requestParseError))
            return;

        await OnRunRequested.InvokeAsync();
        _responseEditorKey++;
    }
}
