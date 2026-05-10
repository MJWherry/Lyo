namespace Lyo.EntityReference.Models;

/// <summary>
/// Helpers for mapping an <see cref="EntityRef"/> to a single PostgreSQL <c>uuid</c> column (“Option A” persistence).
/// </summary>
/// <remarks>
/// Composite ids produced by <see cref="EntityRef.For{T}(object[])"/> do not round-trip through these methods;
/// use stores that persist <see cref="EntityRef.EntityId"/> as text or a dedicated encoding instead.
/// </remarks>
public static class EntityRefPersistedGuid
{
    /// <summary>
    /// Attempts to interpret <paramref name="entityRef"/>.<see cref="EntityRef.EntityId"/> as exactly one <see cref="Guid"/>.
    /// </summary>
    /// <param name="entityRef">Reference whose <see cref="EntityRef.EntityId"/> should be a GUID string.</param>
    /// <param name="guid">The parsed identifier when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if trimming <see cref="EntityRef.EntityId"/> yields a valid GUID string.</returns>
    public static bool TryGetPersistedGuid(EntityRef entityRef, out Guid guid)
    {
        guid = default;
        return Guid.TryParse(entityRef.EntityId.Trim(), out guid);
    }

    /// <summary>
    /// Returns the GUID backing <paramref name="entityRef"/> for Option A stores, or throws <see cref="EntityRefPersistenceException"/>.
    /// </summary>
    /// <param name="entityRef">Reference to validate.</param>
    /// <returns>The parsed <see cref="Guid"/>.</returns>
    /// <exception cref="EntityRefPersistenceException">
    /// <see cref="EntityRef.EntityId"/> is not exactly one valid GUID (for example composite or malformed text).
    /// </exception>
    public static Guid RequirePersistedGuid(EntityRef entityRef)
    {
        if (!TryGetPersistedGuid(entityRef, out var guid))
            throw new EntityRefPersistenceException(
                $"EntityRef.EntityId must be a single Guid for this store (got composite or invalid key for type '{entityRef.EntityType}').");

        return guid;
    }
}
