using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Contact Enrichment query.</summary>
public sealed class EndatoCeQueryEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(25)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = null!;

    [MaxLength(50)]
    public string? AddressLineOne { get; set; }

    [Required]
    [MaxLength(50)]
    public string AddressLineTwo { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    [Required]
    public int IdentityScore { get; set; }

    public int? TotalRequestExecutionTime { get; set; }

    public Guid? EndatoCePersonId { get; set; }

    [ForeignKey(nameof(EndatoCePersonId))]
    public EndatoCePersonEntity? EndatoCePerson { get; set; }

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    public DateTimeOffset RequestTime { get; set; }

    [Required]
    public DateTimeOffset RequestTimestamp { get; set; }
}