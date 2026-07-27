using System.ComponentModel.DataAnnotations;
using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>A single generated report output. Optional <see cref="OutputFileId"/> is set by consumer hooks.</summary>
public sealed class ReportGeneration
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ReportDefinitionId { get; set; }

    public ReportDefinition? ReportDefinition { get; set; }

    /// <summary>Snapshot of composition JSON used for this generation.</summary>
    [Required]
    public string ReportDataJson { get; set; } = null!;

    [Required]
    [MaxLength(32)]
    public string Format { get; set; } = nameof(ReportFormat.Html);

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = nameof(ReportGenerationStatus.Pending);

    /// <summary>Optional opaque id of a persisted output (consumer-defined, e.g. FileStorage).</summary>
    public Guid? OutputFileId { get; set; }

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    [MaxLength(200)]
    public string? ContentType { get; set; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(500)]
    public string? PathPrefix { get; set; }

    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = "Unknown";

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public List<ReportGenerationParameter> Parameters { get; set; } = [];
}
