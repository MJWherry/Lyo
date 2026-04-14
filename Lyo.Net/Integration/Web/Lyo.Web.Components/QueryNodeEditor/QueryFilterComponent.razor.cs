using Lyo.Api.Client;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Lyo.Web.Components.UniqueValueSelector;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Web.Components.QueryNodeEditor;

public partial class QueryFilterComponent
{
    private MudMenu? _uniqueValuesMenu;
    private bool? _currentBoolValue;
    private ComparisonOperatorEnum _currentComparison = ComparisonOperatorEnum.Contains;
    private string? _currentCsvValue;
    private DateTime? _currentDateValue;
    private IReadOnlyList<string> _currentMultiSelectValues = new List<string>();
    private decimal? _currentNumberValue;
    private FilterPropertyDefinition? _currentProperty;
    private IReadOnlyList<SpUniqueValueCount>? _currentPropertyUniqueValues;
    private string? _currentStringValue;

    private List<ConditionClause> _filters = [];
    private bool _isLoadingUniqueValues;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = default!;

    [Parameter]
    public EventCallback<List<ConditionClause>> FiltersChanged { get; set; }

    [Parameter]
    public IEnumerable<FilterPropertyDefinition> PropertyDefinitions { get; set; } = [];

    [Parameter]
    public IEnumerable<ConditionClause> CurrentFilters { get; set; } = [];

    protected override void OnParametersSet()
    {
        if (!_filters.SequenceEqual(CurrentFilters))
            _filters = new(CurrentFilters);

        if (_currentProperty == null)
            return;

        var availableComparisonOperators = Extensions.GetAvailableComparisonOperators(_currentProperty.Type);
        if (!availableComparisonOperators.Contains(_currentComparison))
            _currentComparison = availableComparisonOperators.First();
    }

    private static bool ShouldUseTextInput(FilterPropertyDefinition property, ComparisonOperatorEnum comparator)
        => property.Type is FilterPropertyType.String && comparator is ComparisonOperatorEnum.Contains or ComparisonOperatorEnum.NotContains or ComparisonOperatorEnum.Equals or ComparisonOperatorEnum.NotEquals
            or ComparisonOperatorEnum.StartsWith or ComparisonOperatorEnum.NotStartsWith or ComparisonOperatorEnum.EndsWith or ComparisonOperatorEnum.NotEndsWith;

    private Task OnUniqueValuesChanged(IEnumerable<string?> values)
    {
        _currentMultiSelectValues = values.Select(value => value ?? string.Empty).ToList();
        return Task.CompletedTask;
    }

    private Task OnMudSelectValuesChanged(IReadOnlyCollection<string>? values)
    {
        _currentMultiSelectValues = values?.ToList() ?? [];
        return Task.CompletedTask;
    }

    private async Task OnUniqueValueSelectorTextChanged(string searchText)
    {
        if (_currentProperty == null)
            return;

        _isLoadingUniqueValues = true;
        try {
            var route = $"info/{_currentProperty.Schema}/{_currentProperty.Table}/{_currentProperty.Column}/GetUniqueCounts";
            var queryParams = "?start=0&amount=100";
            if (!string.IsNullOrWhiteSpace(searchText))
                queryParams += $"&containsFilter={Uri.EscapeDataString(searchText)}";

            _currentPropertyUniqueValues = await ApiClient.GetAsAsync<List<SpUniqueValueCount>>(route + queryParams);
        }
        finally {
            _isLoadingUniqueValues = false;
        }
    }

    private async Task AddFilter()
    {
        if (_currentProperty == null || !IsValidInput())
            return;

        object? value;
        if (_currentComparison.IsMultiValueComparisonOperator()) {
            value = _currentProperty.UniqueValues != null || _currentProperty.EnumValues != null || _currentProperty.HasDynamicUniqueValues ? _currentMultiSelectValues.ToList() :
                string.IsNullOrWhiteSpace(_currentCsvValue) ? new() : _currentCsvValue.Split(',').Select(item => item.Trim()).Where(item => !string.IsNullOrEmpty(item)).ToList();
        }
        else {
            value = _currentProperty.Type switch {
                FilterPropertyType.String => _currentStringValue,
                FilterPropertyType.Number => _currentNumberValue,
                FilterPropertyType.DateTime => _currentDateValue,
                FilterPropertyType.Enum => _currentStringValue,
                FilterPropertyType.Bool => _currentBoolValue,
                var _ => _currentStringValue
            };
        }

        _filters.Add(new(_currentProperty.PropertyName, _currentComparison, value));
        await FiltersChanged.InvokeAsync(_filters);
        ClearInputs();
    }

    private bool IsValidInput()
    {
        if (_currentProperty == null)
            return false;

        if (_currentComparison.IsMultiValueComparisonOperator()) {
            if (_currentProperty.UniqueValues != null || _currentProperty.EnumValues != null || _currentProperty.HasDynamicUniqueValues)
                return _currentMultiSelectValues.Any();

            return !string.IsNullOrWhiteSpace(_currentCsvValue);
        }

        if (_currentProperty.Type == FilterPropertyType.String && _currentComparison is ComparisonOperatorEnum.Contains or ComparisonOperatorEnum.NotContains)
            return true;

        return _currentProperty.Type switch {
            FilterPropertyType.String => !string.IsNullOrWhiteSpace(_currentStringValue),
            FilterPropertyType.Number => _currentNumberValue.HasValue,
            FilterPropertyType.DateTime => _currentDateValue.HasValue,
            FilterPropertyType.Enum => !string.IsNullOrWhiteSpace(_currentStringValue),
            FilterPropertyType.Bool => _currentBoolValue.HasValue,
            var _ => !string.IsNullOrWhiteSpace(_currentStringValue)
        };
    }

    private void ClearInputs()
    {
        _currentStringValue = null;
        _currentNumberValue = null;
        _currentDateValue = null;
        _currentCsvValue = null;
        _currentMultiSelectValues = [];
        _currentPropertyUniqueValues = null;
    }

    private string FormatValue(object? value, Func<object?, string>? formatter)
    {
        if (value == null)
            return "(null)";

        return formatter?.Invoke(value) ?? value.ToString() ?? "(empty)";
    }

    private async Task CloseUniqueValuesMenuAsync()
    {
        if (_uniqueValuesMenu is not null)
            await _uniqueValuesMenu.CloseMenuAsync();
    }

    /// <summary>MudSelect multi-selection default text joins every label on one line; keep the closed control compact.</summary>
    private static string SummarizeMultiSelectSummary(IReadOnlyList<string?>? selected)
    {
        if (selected is null || selected.Count == 0)
            return "Select values...";

        return $"{selected.Count} selected";
    }
}