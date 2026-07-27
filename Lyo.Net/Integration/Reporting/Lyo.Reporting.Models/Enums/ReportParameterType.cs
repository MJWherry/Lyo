namespace Lyo.Reporting.Models.Enums;

/// <summary>Parameter value types for report definition / generation parameters (mirrors Job parameter types).</summary>
public enum ReportParameterType
{
    Unknown,
    String,
    Bool,
    Enum,
    DateTime,
    DateOnly,
    TimeOnly,
    Int,
    Long,
    Decimal,
    Guid,
    Regex,
    Json,
    Xml
}
