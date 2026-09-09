namespace Lyo.Query.Models.Enums;

/// <summary>How navigation <c>Include</c> graphs are populated relative to the where clause.</summary>
public enum QueryIncludeFilterMode
{
    /// <summary>Load every related row reachable from matched roots (typical EF eager-load).</summary>
    Full,

    /// <summary>Trim included collections and nested graphs to elements that satisfy the filter (API-layer, implementation-defined).</summary>
    MatchedOnly
}