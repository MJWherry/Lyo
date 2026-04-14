namespace Lyo.Endato.Client.Models.Enrichment.Response;

public sealed record Phone(string FirstReportedDate, string LastReportedDate, string Type, bool IsConnected, string Number);