using System.Text.RegularExpressions;
using Lyo.Query.Models.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SortDirectionEnum = Lyo.Common.Enums.SortDirection;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class SortByList
{
    private static readonly Regex PropertyPattern = new("^[a-zA-Z.]+$", RegexOptions.Compiled);

    private SortDirectionEnum _newDirection = SortDirectionEnum.Desc;
    private int? _newPriority;

    private string _newProperty = "";

    [Parameter]
    public List<SortBy> Items { get; set; } = [];

    [Parameter]
    public EventCallback<List<SortBy>> ItemsChanged { get; set; }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            AddSort();
    }

    private void AddSort()
    {
        if (string.IsNullOrWhiteSpace(_newProperty))
            return;

        if (!PropertyPattern.IsMatch(_newProperty.Trim()))
            return;

        var list = new List<SortBy>(Items) { new(_newProperty.Trim(), _newDirection, _newPriority ?? Items.Count) };
        _newProperty = "";
        _newDirection = SortDirectionEnum.Desc;
        _newPriority = null;
        ItemsChanged.InvokeAsync(list);
    }

    private void RemoveSort(int index)
    {
        var list = Items.Where((_, i) => i != index).ToList();
        ItemsChanged.InvokeAsync(list);
    }

    private static string FormatChipText(SortBy item)
    {
        var dir = item.Direction == SortDirectionEnum.Asc ? "Asc" : "Desc";
        var pri = item.Priority.HasValue ? $" #{item.Priority}" : "";
        return $"{item.PropertyName} {dir}{pri}";
    }
}