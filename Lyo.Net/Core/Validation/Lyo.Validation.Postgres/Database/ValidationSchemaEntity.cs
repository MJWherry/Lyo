using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Validation.Postgres.Database;

/// <summary>Persisted <see cref="Lyo.Validation.ValidationSchema" /> row in the <c>validation</c> schema.</summary>
public sealed class ValidationSchemaEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? TargetTypeName { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string ConstraintsJson { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? MessagesJson { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}
