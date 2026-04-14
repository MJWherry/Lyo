using System.ComponentModel;

namespace Lyo.Query.Models.Enums;

//todo Add Only
//todo add starts/ends with, etc work for guid values
public enum ComparisonOperatorEnum
{
    [Description("Unknown")]
    Unknown,

    /// <summary>Numerical, String, DateTime, DateOnly, TimeOnly, Guid</summary>
    [Description("=")]
    Equals,

    /// <summary>Numerical, String, DateTime, DateOnly, TimeOnly, Guid</summary>
    [Description("≠")]
    NotEquals,

    /// <summary>String</summary>
    [Description("Contains")]
    Contains,

    /// <summary>String</summary>
    [Description("Not Contains")]
    NotContains,

    /// <summary>String</summary>
    [Description("Starts With")]
    StartsWith,

    /// <summary>String</summary>
    [Description("Ends With")]
    EndsWith,

    /// <summary>String</summary>
    [Description("Not Starts With")]
    NotStartsWith,

    /// <summary>String</summary>
    [Description("Not Ends With")]
    NotEndsWith,

    /// <summary>Numerical, DateTime, DateOnly, TimeOnly, Enumerable References If Enumerable, will take count of referenced items, can be dangerous due to explosive queries</summary>
    [Description(">")]
    GreaterThan,

    /// <summary>Numerical, DateTime, DateOnly, TimeOnly, Enumerable References If Enumerable, will take count of referenced items, can be dangerous due to explosive queries</summary>
    [Description("≥")]
    GreaterThanOrEqual,

    /// <summary>Numerical, DateTime, DateOnly, TimeOnly, Enumerable References If Enumerable, will take count of referenced items, can be dangerous due to explosive queries</summary>
    [Description("<")]
    LessThan,

    /// <summary>Numerical, DateTime, DateOnly, TimeOnly, Enumerable References If Enumerable, will take count of referenced items, can be dangerous due to explosive queries</summary>
    [Description("≤")]
    LessThanOrEqual,

    /// <summary>String, Numerical, DateTime, DateOnly, TimeOnly<br /> IE Value = [1, 2, 3], ['1', '2', '3'], or CSV "1,2,3"</summary>
    [Description("In")]
    In,

    /// <summary>String, Numerical, DateTime, DateOnly, TimeOnly<br /> IE Value = [1, 2, 3], ['1/1/2001', '2', '3'], or CSV "1,2,3"</summary>
    [Description("Not In")]
    NotIn,

    /// <summary>String</summary>
    [Description("Regex")]
    Regex,

    /// <summary>String</summary>
    [Description("Not Regex")]
    NotRegex
}
