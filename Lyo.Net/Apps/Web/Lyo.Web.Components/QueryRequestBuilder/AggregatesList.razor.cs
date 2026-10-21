using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class AggregatesList
{
    private static readonly AggregateFunction[] Functions = [
        AggregateFunction.Count, AggregateFunction.Sum, AggregateFunction.Average, AggregateFunction.Min, AggregateFunction.Max, AggregateFunction.CountDistinct
    ];

    private string _field = "";

    private AggregateFunction _function = AggregateFunction.Count;
    private string _name = "Count";

    [Parameter]
    public string? ElementId { get; set; }

    [Parameter]
    public List<AggregateSelect> Items { get; set; } = [];

    [Parameter]
    public EventCallback<List<AggregateSelect>> ItemsChanged { get; set; }

    private bool CanAdd => !string.IsNullOrWhiteSpace(_name) && (_function == AggregateFunction.Count || !string.IsNullOrWhiteSpace(_field));

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            AddAggregate();
    }

    private void AddAggregate()
    {
        if (!CanAdd)
            return;

        var field = string.IsNullOrWhiteSpace(_field) ? null : _field.Trim();
        var list = new List<AggregateSelect>(Items) { new(_function, _name.Trim(), field) };
        _name = "";
        _field = "";
        ItemsChanged.InvokeAsync(list);
    }

    private void RemoveAggregate(int index)
    {
        var list = Items.Where((_, i) => i != index).ToList();
        ItemsChanged.InvokeAsync(list);
    }

    private static string ChipLabel(AggregateSelect item) => string.IsNullOrWhiteSpace(item.Field) ? item.Name : $"{item.Function}({item.Field}) as {item.Name}";
}