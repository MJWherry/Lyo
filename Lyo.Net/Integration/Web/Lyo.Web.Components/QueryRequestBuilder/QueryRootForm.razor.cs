using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class QueryRootForm
{
    [Parameter]
    public string? ElementId { get; set; }

    [Parameter]
    public QueryReq Request { get; set; } = new();

    [Parameter]
    public EventCallback<QueryReq> RequestChanged { get; set; }

    [Parameter]
    public IEnumerable<FilterPropertyDefinition> PropertyDefinitions { get; set; } = [];

    [Parameter]
    public IEnumerable<string>? SelectAll { get; set; }

    [Parameter]
    public EventCallback<IEnumerable<string>> SelectAllChanged { get; set; }

    [Parameter]
    public EventCallback<WhereClause?> SelectedWhereClauseChanged { get; set; }

    [Parameter]
    public bool AllowFilterDragDrop { get; set; } = true;

    [Parameter]
    public bool AutoSelectNewFilterNode { get; set; } = true;

    protected override void OnParametersSet()
    {
        Request.Options ??= new();
        Request.From ??= new FromClause();
        Request.Joins ??= [];
        Request.Select ??= [];
        Request.ComputedFields ??= [];
    }

    private async Task NotifyAsync() => await RequestChanged.InvokeAsync(Request);

    private async Task OnStartAmountChanged((int? Start, int? Amount) value)
    {
        Request.Start = value.Start;
        Request.Amount = value.Amount;
        await NotifyAsync();
    }

    private async Task OnTotalCountModeChanged(QueryTotalCountMode mode)
    {
        Request.Options.TotalCountMode = mode;
        await NotifyAsync();
    }

    private async Task OnFromAliasChanged(string alias)
    {
        Request.From.Alias = alias?.Trim() ?? "";
        await NotifyAsync();
    }

    private async Task OnFromEntityTypeChanged(string entityType)
    {
        Request.From.EntityType = entityType?.Trim() ?? "";
        await NotifyAsync();
    }

    private async Task AddJoin()
    {
        Request.Joins.Add(
            new JoinClause {
                Alias = "j" + (Request.Joins.Count + 1),
                EntityType = "",
                Type = JoinType.Left,
                On = [new JoinOn { From = $"{Request.From.Alias}.Id", To = "j.Id" }]
            });
        await NotifyAsync();
    }

    private async Task RemoveJoin(JoinClause join)
    {
        Request.Joins.Remove(join);
        await NotifyAsync();
    }

    private async Task OnJoinAliasChanged(JoinClause join, string alias)
    {
        join.Alias = alias?.Trim() ?? "";
        await NotifyAsync();
    }

    private async Task OnJoinEntityTypeChanged(JoinClause join, string entityType)
    {
        join.EntityType = entityType?.Trim() ?? "";
        await NotifyAsync();
    }

    private async Task OnJoinAsChanged(JoinClause join, string? asName)
    {
        join.As = string.IsNullOrWhiteSpace(asName) ? null : asName.Trim();
        await NotifyAsync();
    }

    private async Task OnJoinTypeChanged(JoinClause join, JoinType type)
    {
        join.Type = type;
        await NotifyAsync();
    }

    private async Task AddOn(JoinClause join)
    {
        join.On.Add(new JoinOn { From = $"{Request.From.Alias}.", To = $"{join.Alias}." });
        await NotifyAsync();
    }

    private async Task RemoveOn(JoinClause join, JoinOn on)
    {
        join.On.Remove(on);
        await NotifyAsync();
    }

    private async Task OnOnFromChanged(JoinOn on, string value)
    {
        on.From = value?.Trim() ?? "";
        await NotifyAsync();
    }

    private async Task OnOnToChanged(JoinOn on, string value)
    {
        on.To = value?.Trim() ?? "";
        await NotifyAsync();
    }

    private async Task OnSelectChanged(List<string> select)
    {
        Request.Select = select;
        await NotifyAsync();
    }

    private async Task OnSelectAllChanged(List<string> selectAll)
    {
        Request.Select = selectAll;
        await NotifyAsync();
        if (SelectAllChanged.HasDelegate)
            await SelectAllChanged.InvokeAsync(selectAll);
    }

    private async Task OnComputedFieldsChanged(List<ComputedField> fields)
    {
        Request.ComputedFields = fields;
        await NotifyAsync();
    }

    private async Task OnSortByChanged(List<SortBy> sortBy)
    {
        Request.SortBy = sortBy;
        await NotifyAsync();
    }

    private async Task OnWhereClauseChanged(WhereClause? queryNode)
    {
        Request.WhereClause = queryNode;
        await NotifyAsync();
    }

    private async Task OnSelectedWhereClauseChanged(WhereClause? queryNode)
    {
        if (SelectedWhereClauseChanged.HasDelegate)
            await SelectedWhereClauseChanged.InvokeAsync(queryNode);
    }
}
