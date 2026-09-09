namespace Lyo.Query.Models.Enums;

/// <summary>How the API exposes total row counts next to a paged result set.</summary>
public enum QueryTotalCountMode
{
    /// <summary>Exact total of rows matching the filter (may need an extra COUNT query).</summary>
    Exact,

    /// <summary>Skip computing or returning a total count.</summary>
    None,

    /// <summary>Whether more rows exist beyond the current page, without necessarily returning the full total.</summary>
    HasMore
}