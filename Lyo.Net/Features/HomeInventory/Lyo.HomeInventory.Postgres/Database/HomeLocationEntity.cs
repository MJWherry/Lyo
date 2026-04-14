using System.ComponentModel.DataAnnotations;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeLocationEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ParentLocationId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public HomeLocationEntity? Parent { get; set; }

    public List<HomeLocationEntity> Children { get; set; } = [];

    public List<HomeItemStockEntity> StockRows { get; set; } = [];

    public List<HomeItemMovementEntity> MovementsFrom { get; set; } = [];

    public List<HomeItemMovementEntity> MovementsTo { get; set; } = [];
}