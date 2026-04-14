using Lyo.Query.Models.Common;
using Microsoft.AspNetCore.Components;

namespace Lyo.Query.Web.Components;

public partial class QueryBuilderSettings
{
    [Parameter]
    public bool AllowFilterDragDrop { get; set; } = true;

    [Parameter]
    public EventCallback<bool> AllowFilterDragDropChanged { get; set; }

    [Parameter]
    public bool AutoSelectNewFilterNode { get; set; } = true;

    [Parameter]
    public EventCallback<bool> AutoSelectNewFilterNodeChanged { get; set; }

    [Parameter]
    public WhereClause? SelectedWhereClause { get; set; }

    private string GetSelectedWhereClauseLabel()
        => SelectedWhereClause switch {
            null => "None",
            GroupClause groupClause => $"{groupClause.Operator} group clause ({groupClause.Children?.Count ?? 0} children)",
            ConditionClause condition => $"{condition.Field} {condition.Comparison}",
            var other => other.ToString() ?? "Unknown"
        };
}