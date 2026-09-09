namespace Lyo.Query.Models.Enums;

/// <summary>Boolean combiner for children in a <see cref="Lyo.Query.Models.Common.GroupClause" />.</summary>
public enum GroupOperatorEnum
{
    /// <summary>Every child must match (logical AND).</summary>
    And,

    /// <summary>One or more children must match (logical OR).</summary>
    Or
}