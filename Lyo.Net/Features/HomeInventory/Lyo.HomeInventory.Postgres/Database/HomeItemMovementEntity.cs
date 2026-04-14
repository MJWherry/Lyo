using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemMovementEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public int MovementType { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public Guid? FromLocationId { get; set; }

    public Guid? ToLocationId { get; set; }

    [MaxLength(200)]
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? CreatedByEntityType { get; set; }

    [MaxLength(200)]
    public string? CreatedByEntityId { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public HomeItemEntity Item { get; set; } = null!;

    public HomeLocationEntity? FromLocation { get; set; }

    public HomeLocationEntity? ToLocation { get; set; }
}