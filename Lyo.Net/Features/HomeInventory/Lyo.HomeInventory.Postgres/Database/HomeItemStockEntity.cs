using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemStockEntity
{
    public Guid ItemId { get; set; }

    public Guid LocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityOnHand { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityReserved { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ReorderPoint { get; set; }

    [Required]
    public DateTime UpdatedTimestamp { get; set; }

    public HomeItemEntity Item { get; set; } = null!;

    public HomeLocationEntity Location { get; set; } = null!;
}