namespace Lyo.EntityReference.Models;

/// <summary>Provenance for an ingested row: external source key and when the link was recorded.</summary>
/// <param name="Source">External entity this row was imported from (e.g. Endato PS person).</param>
/// <param name="ImportedAt">When this source link was recorded (UTC).</param>
public readonly record struct EntitySourceRecord(EntityRef Source, DateTime ImportedAt)
{
    /// <summary>Creates a provenance record for import; owner is determined from the parent aggregate on persist.</summary>
    public static EntitySourceRecord From(EntityRef source, DateTime importedAt)
        => new(source, importedAt);

    /// <summary>Creates a provenance record using the logical type of <typeparamref name="T" /> and a <see cref="Guid" /> source key.</summary>
    /// <typeparam name="T">CLR type used to resolve the stored source entity type discriminator.</typeparam>
    /// <param name="sourceId">External source identifier stored using the GUID's default string format.</param>
    /// <param name="importedAt">When this source link was recorded (UTC).</param>
    /// <returns>A new <see cref="EntitySourceRecord" />.</returns>
    public static EntitySourceRecord From<T>(Guid sourceId, DateTime importedAt)
        => From(EntityRef.For<T>(sourceId), importedAt);

    /// <summary>Creates a provenance record using the logical type of <typeparamref name="T" /> and a string source key.</summary>
    /// <typeparam name="T">CLR type used to resolve the stored source entity type discriminator.</typeparam>
    /// <param name="sourceId">Non-empty external source identifier string.</param>
    /// <param name="importedAt">When this source link was recorded (UTC).</param>
    /// <returns>A new <see cref="EntitySourceRecord" />.</returns>
    public static EntitySourceRecord From<T>(string sourceId, DateTime importedAt)
        => From(EntityRef.For<T>(sourceId), importedAt);

    /// <summary>Creates a provenance record using the logical type of <typeparamref name="T" /> and one or more source key segments.</summary>
    /// <typeparam name="T">CLR type used to resolve the stored source entity type discriminator.</typeparam>
    /// <param name="importedAt">When this source link was recorded (UTC).</param>
    /// <param name="keys">One or more non-empty key segments; multiple segments form a composite source id.</param>
    /// <returns>A new <see cref="EntitySourceRecord" />.</returns>
    public static EntitySourceRecord From<T>(DateTime importedAt, params object[] keys)
        => From(EntityRef.For<T>(keys), importedAt);

    /// <summary>Creates a provenance record from an entity instance and a selector that extracts the source key or keys.</summary>
    /// <typeparam name="T">CLR type used to resolve the stored source entity type discriminator.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns a single key, a non-string <see cref="IEnumerable" /> of keys, or an <c>object[]</c> of keys.</param>
    /// <param name="importedAt">When this source link was recorded (UTC).</param>
    /// <returns>A new <see cref="EntitySourceRecord" />.</returns>
    public static EntitySourceRecord From<T>(T entity, Func<T, object?> selector, DateTime importedAt)
        where T : class
        => From(EntityRef.For(entity, selector), importedAt);

    /// <summary>Creates a provenance record from an entity instance and a selector that returns one or more source keys.</summary>
    /// <typeparam name="T">CLR type used to resolve the stored source entity type discriminator.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns key segments, including via collection expressions (e.g. <c>e => [e.Id]</c>).</param>
    /// <param name="importedAt">When this source link was recorded (UTC).</param>
    /// <returns>A new <see cref="EntitySourceRecord" />.</returns>
    public static EntitySourceRecord From<T>(T entity, Func<T, object?[]> selector, DateTime importedAt)
        where T : class
        => From(EntityRef.For(entity, selector), importedAt);
}
