namespace Lyo.Reporting.Models.Enums;

/// <summary>Lifecycle status of one report generation.</summary>
public enum ReportGenerationStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}