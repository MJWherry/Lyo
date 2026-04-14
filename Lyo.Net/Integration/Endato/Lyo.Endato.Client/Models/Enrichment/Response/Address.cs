namespace Lyo.Endato.Client.Models.Enrichment.Response;

public sealed record Address(string FirstReportedDate, string LastReportedDate, string Street, string? Unit, string City, string State, string Zip);