namespace Lyo.Endato.Client.Models.Enrichment.Request;

public class EnrichmentQuery
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public Address Address { get; set; } = null!;

    public string? DoB { get; set; }
}