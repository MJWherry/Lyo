namespace Lyo.Reporting.Models;

/// <summary>
/// Raised when a report request fails input validation (bad parameters, malformed JSON, inactive/missing definition, size limits). API hosts map this to HTTP 400; genuine
/// faults keep surfacing as 500.
/// </summary>
public sealed class ReportValidationException : Exception
{
    public ReportValidationException(string message)
        : base(message) { }

    public ReportValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}