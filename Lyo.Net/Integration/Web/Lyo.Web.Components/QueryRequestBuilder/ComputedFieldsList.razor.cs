using Lyo.Query.Models.Common.Request;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class ComputedFieldsList
{
    private string _newName = "";
    private string _newTemplate = "";

    [Parameter]
    public List<ComputedField> Items { get; set; } = [];

    [Parameter]
    public EventCallback<List<ComputedField>> ItemsChanged { get; set; }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            AddField();
    }

    private void AddField()
    {
        if (string.IsNullOrWhiteSpace(_newName) || string.IsNullOrWhiteSpace(_newTemplate))
            return;

        var list = new List<ComputedField>(Items) { new(_newName.Trim(), _newTemplate.Trim()) };
        _newName = "";
        _newTemplate = "";
        ItemsChanged.InvokeAsync(list);
    }

    private void RemoveField(int index)
    {
        var list = Items.Where((_, i) => i != index).ToList();
        ItemsChanged.InvokeAsync(list);
    }
}