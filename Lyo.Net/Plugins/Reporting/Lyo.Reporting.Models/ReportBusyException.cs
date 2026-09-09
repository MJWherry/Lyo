namespace Lyo.Reporting.Models;

/// <summary>Raised when generation is rejected because the host <c>MaxConcurrentGenerations</c> limit is saturated. API hosts map this to HTTP 503.</summary>
public sealed class ReportBusyException(string message)
    : Exception(message);