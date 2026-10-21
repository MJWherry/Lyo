namespace Lyo.Api.Models.Common.Request;

/// <summary>Renames a source column or JSON property onto a target property. Unmapped sources are ignored.</summary>
public sealed class ImportFieldMap
{
    /// <summary>Header or JSON property name as it appears in the file.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Property name on the destination type.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>When true, a missing source value omits the row. A missing column fails the document.</summary>
    public bool Required { get; set; }
}