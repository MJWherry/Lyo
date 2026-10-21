using Lyo.Endato.Client.Models.Enrichment.Request;
using Microsoft.AspNetCore.Components;

namespace Lyo.Endato.Web.Components;

public partial class EndatoEnrichmentSearchForm
{
    private string? _addressLine1;
    private string? _addressLine2;

    [Parameter]
    [EditorRequired]
    public EnrichmentQuery Query { get; set; } = new();

    [Parameter]
    public EventCallback<EnrichmentQuery> QueryChanged { get; set; }

    private int IdentifierCount {
        get {
            var count = 0;
            if (HasName())
                count++;

            if (!string.IsNullOrWhiteSpace(Query.Phone))
                count++;

            if (!string.IsNullOrWhiteSpace(Query.Email))
                count++;

            if (HasAddress())
                count++;

            return count;
        }
    }

    protected override void OnParametersSet()
    {
        _addressLine1 = Query.Address?.AddressLine1;
        _addressLine2 = Query.Address?.AddressLine2;
    }

    public EnrichmentQuery ApplyToQuery()
    {
        if (HasAddress())
            Query.Address = new() { AddressLine1 = _addressLine1, AddressLine2 = _addressLine2 };
        else
            Query.Address = null;

        return Query;
    }

    private bool HasName() => !string.IsNullOrWhiteSpace(Query.FirstName) || !string.IsNullOrWhiteSpace(Query.LastName) || !string.IsNullOrWhiteSpace(Query.MiddleName);

    private bool HasAddress() => !string.IsNullOrWhiteSpace(_addressLine1) || !string.IsNullOrWhiteSpace(_addressLine2);
}