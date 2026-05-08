namespace Lyo.Query.Models.Enums;

/// <summary>How the API exposes total row counts alongside a paged result set.</summary>
public enum QueryTotalCountMode
{
    /// <summary>Return the exact total number of rows matching the filter (may require an extra COUNT query).</summary>
    Exact,

    /// <summary>Do not compute or return a total count.</summary>
    None,

    /// <summary>Return whether more rows exist beyond the current page without necessarily returning the full total.</summary>
    HasMore
}