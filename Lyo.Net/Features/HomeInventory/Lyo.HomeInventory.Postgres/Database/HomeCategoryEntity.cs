using System.ComponentModel.DataAnnotations;

namespace Lyo.HomeInventory.Postgres.Database;

public sealed class HomeCategoryEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ParentCategoryId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public HomeCategoryEntity? Parent { get; set; }

    public List<HomeCategoryEntity> Children { get; set; } = [];

    public List<HomeItemEntity> Items { get; set; } = [];
}