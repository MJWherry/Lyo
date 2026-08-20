using System.ComponentModel.DataAnnotations;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>Instance parameter values for a single report generation.</summary>
public sealed class ReportGenerationParameter
{
    public Guid Id { get; set; }

    public Guid ReportGenerationId { get; set; }

    [MaxLength(3000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Type { get; set; } = null!;

    [MaxLength(3000)]
    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public ReportGeneration ReportGeneration { get; set; } = null!;
}