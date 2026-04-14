using System.Text.Json;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

// ReSharper disable ConvertClosureToMethodGroup

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class QueryReqForm
{
    [Parameter]
    public QueryReq Request { get; set; } = new();

    [Parameter]
    public EventCallback<QueryReq> RequestChanged { get; set; }

    [Parameter]
    public IEnumerable<FilterPropertyDefinition> PropertyDefinitions { get; set; } = [];

    [Parameter]
    public IEnumerable<string>? IncludeAll { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<string>> IncludeAllChanged { get; set; }

    [Parameter]
    public IEnumerable<string>? KeysAll { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<string>> KeysAllChanged { get; set; }

    [Parameter]
    public EventCallback<WhereClause?> SelectedWhereClauseChanged { get; set; }

    [Parameter]
    public bool AllowFilterDragDrop { get; set; } = true;

    [Parameter]
    public bool AutoSelectNewFilterNode { get; set; } = true;

    /// <summary>When true, shows a /Query vs /QueryProject toggle on the query score row (Query Builder workbench).</summary>
    [Parameter]
    public bool ShowEndpointToggle { get; set; }

    [Parameter]
    public bool UseQueryProject { get; set; }

    [Parameter]
    public EventCallback<bool> UseQueryProjectChanged { get; set; }

    protected override void OnParametersSet() => Request.Options ??= new QueryRequestOptions();

    private async Task OnEndpointToggleClicked(bool useProject)
    {
        if (UseQueryProject == useProject)
            return;

        if (UseQueryProjectChanged.HasDelegate)
            await UseQueryProjectChanged.InvokeAsync(useProject);
    }

    private static string FormatKeyPart(object? value)
    {
        if (value == null)
            return "null";

        if (value is string stringValue)
            return $"\"{stringValue}\"";

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return $"\"{jsonElement.GetString() ?? ""}\"";

        return value.ToString() ?? "null";
    }

    private static string FormatKeySet(object[] keySet) => string.Join(", ", keySet.Select(FormatKeyPart));

    private async Task OnStartAmountChanged((int? Start, int? Amount) value)
    {
        Request.Start = value.Start;
        Request.Amount = value.Amount;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnTotalCountModeChanged(QueryTotalCountMode mode)
    {
        Request.Options.TotalCountMode = mode;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnIncludeFilterModeChanged(QueryIncludeFilterMode mode)
    {
        Request.Options.IncludeFilterMode = mode;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnIncludeChanged(List<string> include)
    {
        Request.Include = include;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnIncludeAllChanged(List<string> includeAll)
    {
        Request.Include = includeAll;
        await RequestChanged.InvokeAsync(Request);
        if (IncludeAllChanged.HasDelegate)
            await IncludeAllChanged.InvokeAsync(includeAll);
    }

    private async Task OnKeysChanged(List<object[]> keys)
    {
        Request.Keys = keys;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnKeysAllChanged(IEnumerable<string> keysAll)
    {
        if (KeysAllChanged.HasDelegate)
            await KeysAllChanged.InvokeAsync(keysAll.ToList());
    }

    private async Task OnSortByChanged(List<SortBy> sortBy)
    {
        Request.SortBy = sortBy;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnWhereClauseChanged(WhereClause? queryNode)
    {
        Request.WhereClause = queryNode;
        await RequestChanged.InvokeAsync(Request);
    }

    private async Task OnSelectedWhereClauseChanged(WhereClause? queryNode)
    {
        if (SelectedWhereClauseChanged.HasDelegate)
            await SelectedWhereClauseChanged.InvokeAsync(queryNode);
    }
}
