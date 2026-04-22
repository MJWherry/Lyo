namespace Lyo.Cache;

/// <summary>Controls how list-query and GET cache entries are tagged for invalidation.</summary>
public enum QueryCacheTagGranularity
{
    /// <summary>Per-row and include-cascade instance tags (<c>entity:&lt;type&gt;:&lt;pk&gt;</c>) so mutations can bust only affected cached pages. Higher CPU when storing cache entries.</summary>
    Granular = 0,

    /// <summary>
    /// Type-scoped tags only (<c>entity:&lt;typename&gt;</c>, plus <c>queries</c> / <c>projshape:</c> where applicable), similar to pre–granular-invalidation behavior. Coarser
    /// invalidation: any change to an entity type clears all cached queries (and GETs) tagged for that type.
    /// </summary>
    Broad = 1
}