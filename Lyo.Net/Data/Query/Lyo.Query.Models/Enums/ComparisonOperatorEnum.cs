using System.ComponentModel;

namespace Lyo.Query.Models.Enums;

/// <summary>Predicate operator on a <see cref="Lyo.Query.Models.Common.ConditionClause" />. Applicable CLR types vary per member.</summary>
/// <remarks>GUID fields accept the same string-style operators as <see cref="string" /> by comparing canonical string forms.</remarks>
public enum ComparisonOperatorEnum
{
    [Description("Unknown")]
    Unknown,

    /// <summary>Numeric, String, DateTime, DateOnly, TimeOnly, Guid</summary>
    [Description("=")]
    Equals,

    /// <summary>Numeric, String, DateTime, DateOnly, TimeOnly, Guid</summary>
    [Description("≠")]
    NotEquals,

    /// <summary>String fields</summary>
    [Description("Contains")]
    Contains,

    /// <summary>String fields</summary>
    [Description("Not Contains")]
    NotContains,

    /// <summary>String fields</summary>
    [Description("Starts With")]
    StartsWith,

    /// <summary>String fields</summary>
    [Description("Ends With")]
    EndsWith,

    /// <summary>String fields</summary>
    [Description("Not Starts With")]
    NotStartsWith,

    /// <summary>String fields</summary>
    [Description("Not Ends With")]
    NotEndsWith,

    /// <summary>Numeric, DateTime, DateOnly, TimeOnly, enumerable refs. On enumerables uses item count; can explode queries.</summary>
    [Description(">")]
    GreaterThan,

    /// <summary>Numeric, DateTime, DateOnly, TimeOnly, enumerable refs. On enumerables uses item count; can explode queries.</summary>
    [Description("≥")]
    GreaterThanOrEqual,

    /// <summary>Numeric, DateTime, DateOnly, TimeOnly, enumerable refs. On enumerables uses item count; can explode queries.</summary>
    [Description("<")]
    LessThan,

    /// <summary>Numeric, DateTime, DateOnly, TimeOnly, enumerable refs. On enumerables uses item count; can explode queries.</summary>
    [Description("≤")]
    LessThanOrEqual,

    /// <summary>String, numeric, DateTime, DateOnly, TimeOnly. Value = [1, 2, 3], ['1', '2', '3'], or CSV "1,2,3"</summary>
    [Description("In")]
    In,

    /// <summary>String, numeric, DateTime, DateOnly, TimeOnly. Value = [1, 2, 3], ['1/1/2001', '2', '3'], or CSV "1,2,3"</summary>
    [Description("Not In")]
    NotIn,

    /// <summary>String fields</summary>
    [Description("Regex")]
    Regex,

    /// <summary>String fields</summary>
    [Description("Not Regex")]
    NotRegex
}