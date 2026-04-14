namespace Lyo.Job.Models.Enums;

public enum JobParameterType
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

    /// <summary>The string value should be interpreted as a regex expression</summary>
    Regex, Json,
    Xml

    //todo allow deserialization to complex type via typename + assemblyname?
}