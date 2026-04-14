using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Web.Components.QueryNodeEditor;

public partial class QueryNodeEditorItem
{
    private bool? _boolValue;
    private string? _csvValue;
    private DateTime? _dateValue;
    private decimal? _numberValue;

    private FilterPropertyDefinition? _selectedProperty;
    private string? _stringValue;

    [Parameter]
    public WhereClause Node { get; set; } = null!;

    [Parameter]
    public IEnumerable<FilterPropertyDefinition> PropertyDefinitions { get; set; } = [];

    [Parameter]
    public EventCallback<WhereClause> OnNodeChanged { get; set; }

    [Parameter]
    public EventCallback<WhereClause> OnRemoveChild { get; set; }

    [Parameter]
    public int Level { get; set; }

    [Parameter]
    public bool CanRemove { get; set; } = true;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Node is not ConditionClause conditionNode)
            return;

        _selectedProperty = PropertyDefinitions.FirstOrDefault(property => property.PropertyName == conditionNode.Field);
        if (conditionNode.Value != null) {
            if (conditionNode.Comparison.IsMultiValueComparisonOperator() && conditionNode.Value is IEnumerable<string> multiValues)
                _csvValue = string.Join(", ", multiValues);
            else if (conditionNode.Value is bool boolValue)
                _boolValue = boolValue;
            else if (conditionNode.Value is decimal || conditionNode.Value is int || conditionNode.Value is long)
                _numberValue = Convert.ToDecimal(conditionNode.Value);
            else if (conditionNode.Value is DateTime dateValue)
                _dateValue = dateValue;
            else
                _stringValue = conditionNode.Value.ToString();
        }
        else {
            _stringValue = null;
            _csvValue = null;
            _boolValue = null;
            _numberValue = null;
            _dateValue = null;
        }
    }

    private string GetBorderStyle()
    {
        var color = Node is GroupClause logicalNode ? logicalNode.Operator == GroupOperatorEnum.And ? "#1976d2" : "#9c27b0" : "#4caf50";
        return $"border-left: 4px solid {color};";
    }

    private Color GetOperatorColor(GroupClause logicalNode) => logicalNode.Operator == GroupOperatorEnum.And ? Color.Primary : Color.Secondary;

    private void OnGroupOperatorChanged(GroupOperatorEnum value)
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Operator = value;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void OnDescriptionChanged(string? value)
    {
        if (Node is GroupClause logicalNode) {
            logicalNode.Description = value;
            OnNodeChanged.InvokeAsync(Node);
        }
        else if (Node is ConditionClause conditionNode) {
            conditionNode.Description = value;
            OnNodeChanged.InvokeAsync(Node);
        }
    }

    private void OnComparisonChanged(ComparisonOperatorEnum value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        conditionNode.Comparison = value;
        OnNodeChanged.InvokeAsync(Node);
    }

    private Task<IEnumerable<string>> SearchFieldNames(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(PropertyDefinitions.Select(property => property.PropertyName).Where(propertyName => !string.IsNullOrEmpty(propertyName)));

        var results = PropertyDefinitions
            .Where(property => (property.PropertyName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (property.DisplayName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(property => property.PropertyName)
            .Where(propertyName => !string.IsNullOrEmpty(propertyName));

        return Task.FromResult(results);
    }

    private void OnFieldChanged(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || Node is not ConditionClause conditionNode)
            return;

        conditionNode.Field = fieldName;
        _selectedProperty = PropertyDefinitions.FirstOrDefault(property => property.PropertyName == fieldName);
        var availableComparisonOperators = Extensions.GetAvailableComparisonOperators(_selectedProperty?.Type ?? FilterPropertyType.String);
        if (availableComparisonOperators.Any())
            conditionNode.Comparison = availableComparisonOperators.First();

        OnNodeChanged.InvokeAsync(Node);
    }

    private void OnStringValueChanged(string? value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        _stringValue = value;
        conditionNode.Value = value;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void OnBoolValueChanged(bool? value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        _boolValue = value;
        conditionNode.Value = value;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void OnNumberValueChanged(decimal? value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        _numberValue = value;
        conditionNode.Value = value;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void OnDateValueChanged(DateTime? value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        _dateValue = value;
        conditionNode.Value = value;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void OnCsvValueChanged(string? value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        _csvValue = value;
        conditionNode.Value = string.IsNullOrWhiteSpace(value) ? new() : value.Split(',').Select(item => item.Trim()).Where(item => !string.IsNullOrEmpty(item)).ToList();
        OnNodeChanged.InvokeAsync(Node);
    }

    private void AddChildLogical()
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Children ??= [];
        var placeholder = WhereClauseBuilder.Condition(PropertyDefinitions.FirstOrDefault()?.PropertyName ?? "", ComparisonOperatorEnum.Equals, null);
        logicalNode.Children.Add(WhereClauseBuilder.And(and => and.Add(placeholder)));
        OnNodeChanged.InvokeAsync(Node);
    }

    private void AddChildCondition()
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Children ??= [];
        logicalNode.Children.Add(WhereClauseBuilder.Condition(PropertyDefinitions.FirstOrDefault()?.PropertyName ?? "", ComparisonOperatorEnum.Equals, null));
        OnNodeChanged.InvokeAsync(Node);
    }

    private void RemoveNode() => OnRemoveChild.InvokeAsync(Node);

    private void HandleChildChanged(WhereClause child) => OnNodeChanged.InvokeAsync(Node);

    private void RemoveChild(WhereClause child)
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Children.Remove(child);
        if (!logicalNode.Children.Any())
            logicalNode.Children = [];

        OnNodeChanged.InvokeAsync(Node);
    }

    private void RemoveSubClauseChild(WhereClause node)
    {
        if (Node is not ConditionClause conditionNode || conditionNode.SubClause != node)
            return;

        conditionNode.SubClause = null;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void AddSubClause(ConditionClause conditionNode)
    {
        conditionNode.SubClause = WhereClauseBuilder.And(and
            => and.Add(WhereClauseBuilder.Condition(PropertyDefinitions.FirstOrDefault()?.PropertyName ?? "", ComparisonOperatorEnum.Equals, null)));

        OnNodeChanged.InvokeAsync(Node);
    }
}