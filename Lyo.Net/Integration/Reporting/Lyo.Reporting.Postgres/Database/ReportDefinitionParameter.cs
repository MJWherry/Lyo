using System.ComponentModel.DataAnnotations;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>Parameter schema + default for a report definition.</summary>
public sealed class ReportDefinitionParameter
{
    public Guid Id { get; set; }

    public Guid ReportDefinitionId { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Type { get; set; } = null!;

    [MaxLength(3000)]
    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool AllowMultiple { get; set; }

    public bool Required { get; set; }

    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    [MaxLength(1000)]
    public string? AllowedValues { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ReportDefinition ReportDefinition { get; set; } = null!;
}
