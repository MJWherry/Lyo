using System.ComponentModel.DataAnnotations;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Person Search query.</summary>
public sealed class EndatoPsQueryEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(25)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = null!;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    public int? TotalRequestExecutionTime { get; set; }

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    public int RequestTime { get; set; }

    [Required]
    public DateTimeOffset RequestTimestamp { get; set; }

    public ICollection<EndatoPsPersonEntity> People { get; set; } = new List<EndatoPsPersonEntity>();
}