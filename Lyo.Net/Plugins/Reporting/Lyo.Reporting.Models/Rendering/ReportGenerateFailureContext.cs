namespace Lyo.Reporting.Models.Rendering;

/// <summary>Context used when generation fails.</summary>
public sealed class ReportGenerateFailureContext : ReportGenerateContext
{
    public Exception Exception { get; init; } = null!;
}