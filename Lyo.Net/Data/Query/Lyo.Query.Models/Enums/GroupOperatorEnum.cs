namespace Lyo.Query.Models.Enums;

/// <summary>Boolean combiner for child nodes in a <see cref="Lyo.Query.Models.Common.GroupClause" />.</summary>
public enum GroupOperatorEnum
{
    /// <summary>All children must match (logical AND).</summary>
    And,

    /// <summary>At least one child must match (logical OR).</summary>
    Or
}