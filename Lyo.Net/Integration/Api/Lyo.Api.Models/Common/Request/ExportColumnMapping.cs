namespace Lyo.Api.Models.Common.Request;

/// <summary>One export column: display header and property path or SmartFormat template.</summary>
public sealed class ExportColumnMapping
{
    public string Header { get; set; } = "";

    public string Value { get; set; } = "";
}