using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing social media profiles in PostgreSQL.</summary>
public sealed class SocialMediaProfileEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ProfileUrl { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    public DateTime? AddedAt { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    [ForeignKey(nameof(PersonId))]
    public PersonEntity? Person { get; set; }
}