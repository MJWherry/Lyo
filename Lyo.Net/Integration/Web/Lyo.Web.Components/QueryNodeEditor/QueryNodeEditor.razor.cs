using System.Text.RegularExpressions;
using Lyo.Common;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Validation;
using Lyo.Web.Components.Models;
using Lyo.Web.Components.Validation;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryNodeEditor;

public partial class QueryNodeEditor
{
    private static readonly Regex FieldPattern = new("^[a-zA-Z.]+$", RegexOptions.Compiled);

    private static readonly Validator<string> FieldValidator = ValidatorBuilder<string>.Create()
        .Must(v => !string.IsNullOrWhiteSpace(v), ValidationErrorCodes.EmptyString, "Field name is required.")
        .Must(v => string.IsNullOrWhiteSpace(v) || FieldPattern.IsMatch(v), ValidationErrorCodes.InvalidFormat, "Only letters (a-z, A-Z) and dots are allowed.")
        .Build();

    private LyoValidationWrapper<string>? _fieldValidationRef;

    [Parameter]
    public WhereClause Node { get; set; } = null!;

    [Parameter]
    public IEnumerable<FilterPropertyDefinition> PropertyDefinitions { get; set; } = [];

    [Parameter]
    public EventCallback<WhereClause> OnNodeChanged { get; set; }

    [Parameter]
    public EventCallback<WhereClause> OnChildSelected { get; set; }

    public bool ValidateCurrentNode()
    {
        if (Node is not ConditionClause)
            return true;

        _fieldValidationRef?.Validate();
        return _fieldValidationRef?.IsValid ?? true;
    }

    private string GetStringValue(ConditionClause node)
    {
        if (node.Value is IEnumerable<string> multiValues)
            return string.Join(", ", multiValues);

        return node.Value?.ToString() ?? "";
    }

    private string GetCsvValue(ConditionClause node)
    {
        if (node.Value is IEnumerable<string> multiValues)
            return string.Join(", ", multiValues);

        return "";
    }

    private string GetNodeDisplayText(WhereClause node)
        => node switch {
            GroupClause groupClause =>
                $"{groupClause.Operator} ({groupClause.Children?.Count ?? 0} condition{((groupClause.Children?.Count ?? 0) != 1 ? "s" : "")}){GetDescriptionSuffix(groupClause.Description)}",
            ConditionClause conditionClause =>
                $"{conditionClause.Field} {conditionClause.Comparison}{GetConditionValueSuffix(conditionClause.Value)}{GetDescriptionSuffix(conditionClause.Description)}",
            _ => node.ToString() ?? "Unknown"
        };

    private static string GetConditionValueSuffix(object? value) => value != null ? $" {value}" : " null";

    private static string GetDescriptionSuffix(string? description) => !string.IsNullOrWhiteSpace(description) ? $" - {description}" : "";

    private void OnGroupOperatorChanged(GroupOperatorEnum value)
    {
        if (Node is GroupClause logicalNode) {
            logicalNode.Operator = value;
            OnNodeChanged.InvokeAsync(Node);
        }
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

    private void OnFieldChanged(string? value)
    {
        if (Node is ConditionClause conditionNode) {
            conditionNode.Field = value ?? "";
            OnNodeChanged.InvokeAsync(Node);
        }
    }

    private void OnComparisonChanged(ComparisonOperatorEnum value)
    {
        if (Node is ConditionClause conditionNode) {
            conditionNode.Comparison = value;
            OnNodeChanged.InvokeAsync(Node);
        }
    }

    private async Task OnStringValueChanged(string? value)
    {
        if (Node is ConditionClause conditionNode) {
            conditionNode.Value = value;
            await OnNodeChanged.InvokeAsync(Node);
        }
    }

    private async Task OnBoolValueChanged(bool? value)
    {
        if (Node is ConditionClause conditionNode) {
            conditionNode.Value = value;
            await OnNodeChanged.InvokeAsync(Node);
        }
    }

    private async Task OnNumberValueChanged(decimal? value)
    {
        if (Node is ConditionClause conditionNode) {
            conditionNode.Value = value;
            await OnNodeChanged.InvokeAsync(Node);
        }
    }

    private async Task OnDateValueChanged(DateTime? value)
    {
        if (Node is ConditionClause conditionNode) {
            conditionNode.Value = value;
            await OnNodeChanged.InvokeAsync(Node);
        }
    }

    private async Task OnCsvValueChanged(string? value)
    {
        if (Node is not ConditionClause conditionNode)
            return;

        if (string.IsNullOrWhiteSpace(value))
            conditionNode.Value = null;
        else {
            var values = value.Split(',').Select(item => item.Trim()).Where(item => !string.IsNullOrEmpty(item)).ToList();
            conditionNode.Value = values.Any() ? values : null;
        }

        await OnNodeChanged.InvokeAsync(Node);
    }

    private void AddChildLogical()
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Children ??= [];
        var placeholder = WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null);
        var newNode = WhereClauseBuilder.And(and => and.Add(placeholder));
        logicalNode.Children.Add(newNode);
        OnNodeChanged.InvokeAsync(Node);
    }

    private void AddChildCondition()
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Children ??= [];
        logicalNode.Children.Add(WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null));
        OnNodeChanged.InvokeAsync(Node);
    }

    private void SelectChild(WhereClause child) => OnChildSelected.InvokeAsync(child);

    private void SelectSubClause(WhereClause subQuery) => OnChildSelected.InvokeAsync(subQuery);

    private void AddSubClause(ConditionClause conditionNode)
    {
        conditionNode.SubClause = WhereClauseBuilder.And(and => and.Add(WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null)));
        OnNodeChanged.InvokeAsync(Node);
    }

    private void AddSubClause(GroupClause logicalNode)
    {
        logicalNode.SubClause = WhereClauseBuilder.And(and => and.Add(WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null)));
        OnNodeChanged.InvokeAsync(Node);
    }

    private void RemoveSubClause(ConditionClause conditionNode)
    {
        conditionNode.SubClause = null;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void RemoveSubClause(GroupClause logicalNode)
    {
        logicalNode.SubClause = null;
        OnNodeChanged.InvokeAsync(Node);
    }

    private void RemoveChild(WhereClause child)
    {
        if (Node is not GroupClause logicalNode)
            return;

        logicalNode.Children.Remove(child);
        if (!logicalNode.Children.Any())
            logicalNode.Children = [];

        OnNodeChanged.InvokeAsync(Node);
    }
}