using System.ComponentModel.DataAnnotations;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>Saved report definition (template / composition JSON).</summary>
public sealed class ReportDefinition
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string ReportDataJson { get; set; } = null!;

    [MaxLength(1000)]
    public string? Tags { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(32)]
    public string? DefaultFormat { get; set; }

    [MaxLength(500)]
    public string? DefaultFileName { get; set; }

    [MaxLength(500)]
    public string? DefaultPathPrefix { get; set; }

    [MaxLength(200)]
    public string? GenerationProfileKey { get; set; }

    [MaxLength(500)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public List<ReportDefinitionParameter> Parameters { get; set; } = [];

    public List<ReportGeneration> Generations { get; set; } = [];
}
