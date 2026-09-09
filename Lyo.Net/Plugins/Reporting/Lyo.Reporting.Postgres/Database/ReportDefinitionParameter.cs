using System.ComponentModel.DataAnnotations;
using Lyo.Parameters;

namespace Lyo.Reporting.Postgres.Database;

/// <summary>Parameter schema and default for a report definition.</summary>
public sealed class ReportDefinitionParameter
{
    public Guid Id { get; set; }

    public Guid ReportDefinitionId { get; set; }

    [MaxLength(3000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(1024)]
    public string Type { get; set; } = null!;

    [MaxLength(3000)]
    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool Required { get; set; }

    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    [MaxLength(1000)]
    public string? AllowedValues { get; set; }

    /// <summary>JSON picker source (static key/label list or root QueryReq). Null means no options picker.</summary>
    public string? Options { get; set; }

    /// <summary>Where the default comes from: Literal (<see cref="Value" />) or Expression (<see cref="DefaultTemplate" />). Stored as a string.</summary>
    [Required]
    [MaxLength(10)]
    public string DefaultKind { get; set; } = nameof(LyoParameterDefaultKind.Literal);

    /// <summary>SmartFormat template rendered to produce the default when <see cref="DefaultKind" /> is Expression. Null when the default is literal.</summary>
    public string? DefaultTemplate { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ReportDefinition ReportDefinition { get; set; } = null!;
}