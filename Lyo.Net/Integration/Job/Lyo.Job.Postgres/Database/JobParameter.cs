using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobParameter
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Type { get; set; } = null!;

    [MaxLength(300)]
    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool AllowMultiple { get; set; }

    public bool Required { get; set; }

    /// <summary>Optional regex pattern the value must match. Null = no pattern restriction.</summary>
    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    /// <summary>Minimum string length. Null = no minimum.</summary>
    public int? MinLength { get; set; }

    /// <summary>Maximum string length. Null = no maximum.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Pipe-separated list of allowed values (e.g. <c>A|B|C</c>). Null = no restriction.</summary>
    [MaxLength(1000)]
    public string? AllowedValues { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;
}