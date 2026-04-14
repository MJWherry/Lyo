namespace Lyo.Web.Components.DataGrid;

[Flags]
public enum LyoDataGridFeatureFlags
{
    None = 0,
    BulkMenu = 1 << 0,
    BulkExport = 1 << 1,
    BulkPatch = 1 << 2,
    BulkDelete = 1 << 3,
    Filterable = 1 << 4,
    Searchable = 1 << 5,
    AutoRefresh = 1 << 6,
    All = Filterable | Searchable | AutoRefresh | BulkMenu | BulkExport | BulkPatch | BulkDelete
}

public static class LyoDataGridFeatureFlagsExtensions
{
    public static bool HasFeature(this LyoDataGridFeatureFlags flags, LyoDataGridFeatureFlags feature) => (flags & feature) == feature;
}