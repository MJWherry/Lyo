using Lyo.Endato.Client.Models.Person.Request;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Endato.Web.Components;

public partial class EndatoPersonSearchForm
{
    [Parameter]
    [EditorRequired]
    public PersonQuery Query { get; set; } = new();

    [Parameter]
    public EventCallback<PersonQuery> QueryChanged { get; set; }

    private readonly List<PersonQueryAddress> _addressRows = [];
    private string _tahoeIdsText = string.Empty;
    private string _includesText = string.Empty;
    private string _filterOptionsText = string.Empty;

    private string AddressPanelTitle => $"Addresses ({_addressRows.Count})";

    protected override void OnParametersSet()
    {
        if (_addressRows.Count == 0 && Query.Addresses is { Count: > 0 })
            _addressRows.AddRange(Query.Addresses.Select(a => new PersonQueryAddress { AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, County = a.County }));

        _tahoeIdsText = Query.TahoeIds == null ? string.Empty : string.Join(", ", Query.TahoeIds);
        _includesText = Query.Includes == null ? string.Empty : string.Join(", ", Query.Includes);
        _filterOptionsText = Query.FilterOptions == null ? string.Empty : string.Join(", ", Query.FilterOptions);
    }

    public PersonQuery ApplyToQuery()
    {
        Query.Addresses = _addressRows.Count == 0 ? null : _addressRows.ToList();
        Query.TahoeIds = SplitCsv(_tahoeIdsText);
        Query.Includes = SplitCsv(_includesText);
        Query.FilterOptions = SplitCsv(_filterOptionsText);
        return Query;
    }

    private void AddAddress() => _addressRows.Add(new());

    private void RemoveAddress(int index)
    {
        if (index >= 0 && index < _addressRows.Count)
            _addressRows.RemoveAt(index);
    }

    private static IReadOnlyList<string>? SplitCsv(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var items = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return items.Length == 0 ? null : items;
    }
}
