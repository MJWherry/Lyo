using System.Collections;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Reference to an entity using a compound key of type and id for flexibility.</summary>
/// <remarks>EntityType identifies the kind of entity (e.g. "Person", "Product"). EntityId is typically a Guid string but can be any identifier.</remarks>
public readonly record struct EntityRef(string EntityType, string EntityId)
{
    public string EntityType { get; } = string.IsNullOrWhiteSpace(EntityType) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(EntityType)) : EntityType;

    public string EntityId { get; } = string.IsNullOrWhiteSpace(EntityId) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(EntityId)) : EntityId;

    /// <summary>Creates a reference from an entity instance using a selector to extract the key(s).</summary>
    /// <example>EntityRef.For(docket, d => d.Id), EntityRef.For(order, o => new object[] { o.OrderId, o.LineId })</example>
    public static EntityRef For<T>(T entity, Func<T, object?> selector)
        where T : class
    {
        ArgumentHelpers.ThrowIfNull(entity);
        ArgumentHelpers.ThrowIfNull(selector);
        var key = selector(entity);
        ArgumentHelpers.ThrowIfNull(key);
        var keys = key switch {
            object[] arr => arr,
            IEnumerable e and not string => e.Cast<object>().ToArray(),
            var _ => [key]
        };

        return For<T>(keys!);
    }

    /// <summary>Creates a reference using the full type name of T and the given key(s). Composite keys are ordered and joined with ":".</summary>
    /// <example>EntityRef.For&lt;Docket&gt;(guid), EntityRef.For&lt;User&gt;(123), EntityRef.For&lt;Order&gt;("ord-1", "line-2")</example>
    public static EntityRef For<T>(params object[] keys)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(keys);
        var ordered = keys.Select((k, i) => k?.ToString() is { Length: > 0 } s ? s : throw new ArgumentException($"Key at index {i} cannot be null or empty.", nameof(keys)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        var entityId = ordered.Length == 1 ? ordered[0] : string.Join(":", ordered);
        return new(typeof(T).FullName ?? typeof(T).Name, entityId);
    }

    /// <summary>Creates a reference for an entity identified by a Guid.</summary>
    public static EntityRef ForGuid(string entityType, Guid entityId) => new(entityType, entityId.ToString());

    /// <summary>Creates a reference for an entity with a string identifier.</summary>
    public static EntityRef ForKey(string entityType, string entityId) => new(entityType, entityId);
}