namespace Lyo.Query.Models.Enums;

/// <summary>SQL join kind for root <c>/Query</c> <see cref="Common.Request.JoinClause" />.</summary>
public enum JoinType
{
    Inner = 0,
    Left = 1,
    Right = 2,
    FullOuter = 3
}
