using System.ComponentModel.DataAnnotations;
using Lyo.Parameters;

namespace Lyo.Job.Postgres.Database;

public class JobParameter
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

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

    /// <summary>Optional regex the value must match. Null means no pattern restriction.</summary>
    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    /// <summary>Minimum string length. Null means no minimum.</summary>
    public int? MinLength { get; set; }

    /// <summary>Maximum string length. Null means no maximum.</summary>
    public int? MaxLength { get; set; }

    /// <summary>JSON array of permitted values for closed-list validation (keys only). Null means no restriction.</summary>
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

    /// <summary>Display order among parameters on this definition. Smaller values appear first. Reassigned when the editor list is dragged.</summary>
    public int Order { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;
}