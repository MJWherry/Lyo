using Lyo.Query.Services.WhereClause;

namespace Lyo.Api.Services.Crud.Read.Project;

/// <summary>
/// Detects SQL-level projected shapes that fan out relational rows (collection navigations), so callers can prefer split queries.
/// </summary>
internal static class SqlProjectionJoinShape
{
    /// <summary>
    /// True when the projection likely causes one-to-many joins or multiple result rows per root (reader fan-out).
    /// </summary>
    internal static bool LikelyCausesReaderFanOut(Type entityType, SqlProjectionConversionPlan? plan, IReadOnlyList<ProjectedFieldSpec> specs) 
        => plan?.Slots.Any(s => s is SqlProjectionMergedCollectionSlot) == true 
            || specs.Where(spec => !ProjectionCollectionZip.NormalizedPartsContainWildcard(spec.NormalizedParts))
                .Any(spec => FieldPathCrossesCollectionNavigation(entityType, spec.NormalizedParts));

    /// <summary>
    /// True when the path traverses a collection navigation (same row multiplication as a join to a child table).
    /// </summary>
    private static bool FieldPathCrossesCollectionNavigation(Type rootType, string[] parts)
    {
        if (parts.Length == 0)
            return false;

        var current = rootType;
        foreach (var t in parts) {
            if (SharedEntityMetadataCache.IsCollectionType(current))
                current = SharedEntityMetadataCache.GetCollectionElementType(current);

            var property = SharedEntityMetadataCache.ResolveProperty(current, t);
            if (property is null)
                return false;

            if (SharedEntityMetadataCache.IsCollectionType(property.PropertyType))
                return true;

            current = property.PropertyType;
        }

        return false;
    }
}
