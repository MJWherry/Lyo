using System.ComponentModel.DataAnnotations;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing email addresses in PostgreSQL.</summary>
public sealed class EmailAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public DateTime? VerifiedAt { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}