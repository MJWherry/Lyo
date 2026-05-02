namespace Lyo.Diagnostic.Classification;

/// <summary>How severe the classified exception is likely to be in production.</summary>
public enum ExceptionSeverity
{
    /// <summary>Informational; may be expected in normal operation.</summary>
    Low,

    /// <summary>Degraded behaviour; warrants investigation.</summary>
    Medium,

    /// <summary>Service impact; should be alerted on.</summary>
    High,

    /// <summary>Process-threatening; requires immediate action.</summary>
    Critical
}