using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeItemEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped item. May be omitted.</summary>
    public Guid? TenantId { get; set; }

    [MaxLength(200)]
    public string? OwnerEntityType { get; set; }

    [MaxLength(200)]
    public string? OwnerEntityId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? ParentItemId { get; set; }

    /// <summary>Optional room or location this unique item lives in. Independent of stock rows.</summary>
    public Guid? LocationId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(8192)]
    public string? Description { get; set; }

    public int Status { get; set; }

    public int Condition { get; set; }

    /// <summary>jsonb array of <c>{kind,value,note}</c> product-key rows. Never null; empty list is <c>[]</c>.</summary>
    [Column(TypeName = "jsonb")]
    public string ProductKeysJson { get; set; } = "[]";

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public HomeCategoryEntity? Category { get; set; }

    public HomeLocationEntity? Location { get; set; }

    public HomeItemEntity? ParentItem { get; set; }

    public List<HomeItemEntity> ChildItems { get; set; } = [];

    public List<HomeItemStockEntity> StockRows { get; set; } = [];

    public List<HomeItemMovementEntity> Movements { get; set; } = [];
}
