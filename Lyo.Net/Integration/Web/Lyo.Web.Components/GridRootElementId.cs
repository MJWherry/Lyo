namespace Lyo.Web.Components;

/// <summary>Stable grid root HTML <c>id</c> prefixes (<c>lyo-data-grid-…</c> / <c>lyo-data-grid-projected-…</c>).</summary>
internal static class GridRootElementId
{
    public static string DataGrid(string gridKey) => $"lyo-data-grid-{ElementIdSegmentNormalizer.NormalizeOrDefault(gridKey)}";

    public static string DataGridProjected(string gridKey) => $"lyo-data-grid-projected-{ElementIdSegmentNormalizer.NormalizeOrDefault(gridKey)}";
}