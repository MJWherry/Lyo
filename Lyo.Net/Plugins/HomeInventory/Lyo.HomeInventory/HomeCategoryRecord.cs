using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models;

namespace Lyo.HomeInventory;

/// <summary>grouping (e.g. Networking, Kitchen appliances, TVs). Present only when supplied.</summary>
[EntityRefLogicalType(HomeInventoryEntityTypes.Category)]
public sealed class HomeCategoryRecord
{
    public Guid Id { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}