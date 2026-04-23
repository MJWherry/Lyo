namespace Lyo.Common.Enums;

/// <summary>Defines the severity level of an error.</summary>
public enum ErrorSeverity
{
    /// <summary>Informational message - not an error.</summary>
    Info = 0,

    /// <summary>Warning - operation completed but with concerns.</summary>
    Warning = 1,

    /// <summary>Error - operation failed but may be recoverable.</summary>
    Error = 2,

    /// <summary>Critical error - operation failed and requires immediate attention.</summary>
    Critical = 3
}