namespace Lyo.Diagnostic.Classification;

/// <summary>How severe the classified exception is likely to be in production.</summary>
public enum ExceptionSeverity
{
    /// <summary>Informational; may be expected during normal operation.</summary>
    Low,

    /// <summary>Degraded behaviour; warrants investigation.</summary>
    Medium,

    /// <summary>Service impact; should raise an alert.</summary>
    High,

    /// <summary>Process-threatening; needs immediate action.</summary>
    Critical
}