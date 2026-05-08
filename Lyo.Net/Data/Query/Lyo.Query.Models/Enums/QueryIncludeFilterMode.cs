namespace Lyo.Query.Models.Enums;

/// <summary>Controls how navigation <c>Include</c> graphs are populated relative to the where clause.</summary>
public enum QueryIncludeFilterMode
{
    /// <summary>Include all related rows reachable from matched roots (typical EF eager-load).</summary>
    Full,

    /// <summary>Trim included collections and nested graphs to elements that satisfy the filter (implementation-defined in the API layer).</summary>
    MatchedOnly
}