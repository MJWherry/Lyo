namespace Lyo.Endato.Client.Models.Enrichment.Request;

public class Address
{
    public string? AddressLine1 { get; set; }

    public string AddressLine2 { get; set; } = null!;
}