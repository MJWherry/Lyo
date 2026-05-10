using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Lyo.Common.Extensions;
using Lyo.Exceptions;

namespace Lyo.EntityReference.Models;

/// <summary>Reference to an entity using a compound key of type and id for flexibility.</summary>
/// <remarks>
/// <para><see cref="EntityType"/> identifies the kind of entity (e.g. <c>Person</c>, <c>Product</c>). <see cref="EntityId"/> is typically a Guid string but may be a composite (multi-part keys from <see cref="For{T}(object[])"/>).</para>
/// <para><b>Stable types:</b> apply <see cref="EntityRefLogicalTypeAttribute"/> so persisted rows do not depend on CLR <see cref="Type.FullName"/>.</para>
/// <para><b>Opaque token:</b> <see cref="ToOpaqueToken"/> / <see cref="TryParseOpaque"/> use U+001F as separator (unlikely in natural text).</para>
/// <para><b>JSON:</b> use <see cref="EntityRefJsonConverter"/> for the documented <c>entityType</c> / <c>entityId</c> object shape.</para>
/// </remarks>
/// <param name="EntityType">Logical entity kind; stored in <see cref="EntityType"/>.</param>
/// <param name="EntityId">Identifier string; stored in <see cref="EntityId"/>.</param>
[DebuggerDisplay("{EntityType,nq}: {EntityId,nq}")]
public readonly record struct EntityRef(string EntityType, string EntityId)
{
    /// <summary>Separator for <see cref="ToOpaqueToken"/> and <see cref="TryParseOpaque"/>.</summary>
    public const char OpaqueSeparator = '\x1f';

    static readonly ConcurrentDictionary<Type, string> LogicalTypeCache = new();

    /// <summary>Type discriminator for the referenced entity.</summary>
    /// <exception cref="ArgumentException">The assigned value is null or whitespace.</exception>
    public string EntityType { get; } = EntityType.IsNullOrWhitespace() ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(EntityType)) : EntityType;

    /// <summary>Identifier string for the referenced entity (often a canonical GUID string or a composite encoding).</summary>
    /// <exception cref="ArgumentException">The assigned value is null or whitespace.</exception>
    public string EntityId { get; } = EntityId.IsNullOrWhitespace() ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(EntityId)) : EntityId;

    /// <summary>Creates a reference from an entity instance using a selector to extract the key or keys.</summary>
    /// <typeparam name="T">CLR type used to resolve the logical entity type name.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns a single key, a non-string <see cref="IEnumerable"/> of keys, or an <c>object[]</c> of keys.</param>
    /// <returns>An <see cref="EntityRef"/> whose <see cref="EntityType"/> comes from <typeparamref name="T"/> and whose <see cref="EntityId"/> is built from the keys.</returns>
    /// <example>EntityRef.For(docket, d => d.Id) or EntityRef.For(order, o => new object[] { o.OrderId, o.LineId }).</example>
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

        return For<T>(keys);
    }

    /// <summary>
    /// Creates a reference using the logical or CLR type name of <typeparamref name="T"/> (see <see cref="EntityRefLogicalTypeAttribute"/>) and the given key(s).
    /// Multiple keys are ordered lexically and encoded with <see cref="EntityRefCompositeEncoding"/> so literal <c>:</c> inside a segment remains unambiguous.
    /// </summary>
    /// <typeparam name="T">CLR type used to resolve the stored entity type discriminator.</typeparam>
    /// <param name="keys">One or more non-empty key segments; multiple segments form a composite <see cref="EntityId"/>.</param>
    /// <returns>A new <see cref="EntityRef"/>.</returns>
    /// <exception cref="ArgumentException">A key is null or empty after <see cref="object.ToString"/>.</exception>
    /// <example>Pass one Guid or string key, or several keys to build a composite EntityId (segments ordered lexically).</example>
    public static EntityRef For<T>(params object[] keys)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(keys);
        var ordered = keys.Select((k, i) => k.ToString() is { Length: > 0 } s ? s : throw new ArgumentException($"Key at index {i} cannot be null or empty.", nameof(keys)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        var entityType = ResolveLogicalType(typeof(T));
        var entityId = ordered.Length == 1 ? ordered[0] : EntityRefCompositeEncoding.JoinComposite(ordered);
        return new(entityType, entityId);
    }

    /// <summary>Creates a reference for an entity identified by a <see cref="Guid"/> under the given type name.</summary>
    /// <param name="entityType">Logical entity kind (non-empty).</param>
    /// <param name="entityId">Identifier stored using the GUID's default string format.</param>
    /// <returns>A new <see cref="EntityRef"/>.</returns>
    public static EntityRef ForGuid(string entityType, Guid entityId) => new(entityType, entityId.ToString());

    /// <summary>Creates a reference with an explicit type name and id string.</summary>
    /// <param name="entityType">Logical entity kind (non-empty).</param>
    /// <param name="entityId">Identifier string (non-empty).</param>
    /// <returns>A new <see cref="EntityRef"/>.</returns>
    public static EntityRef ForKey(string entityType, string entityId) => new(entityType, entityId);

    /// <summary>Returns <c>entityType + <see cref="OpaqueSeparator"/> + entityId</c> for logs, opaque blobs, or non-JSON framing.</summary>
    /// <returns>A round-trippable opaque string when both parts are non-empty.</returns>
    public readonly string ToOpaqueToken() => EntityType + OpaqueSeparator + EntityId;

    /// <summary>Parses output from <see cref="ToOpaqueToken"/>.</summary>
    /// <param name="value">Span containing type, U+001F, then id.</param>
    /// <param name="result">The parsed value when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if the span matches the expected opaque layout.</returns>
    public static bool TryParseOpaque(ReadOnlySpan<char> value, out EntityRef result)
    {
        result = default;
        var idx = value.IndexOf(OpaqueSeparator);
        if (idx <= 0 || idx >= value.Length - 1)
            return false;

        var type = value[..idx].ToString();
        var id = value[(idx + 1)..].ToString();
        if (type.IsNullOrWhitespace() || id.IsNullOrWhitespace())
            return false;

        result = new(type, id);
        return true;
    }

    /// <summary>Parses <see cref="ToOpaqueToken"/> output or throws <see cref="FormatException"/>.</summary>
    /// <param name="value">Opaque string produced by <see cref="ToOpaqueToken"/>.</param>
    /// <returns>The parsed <see cref="EntityRef"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="FormatException">The string is not a valid opaque token.</exception>
    public static EntityRef ParseOpaque(string value)
    {
        ArgumentHelpers.ThrowIfNull(value);
        return TryParseOpaque(value.AsSpan(), out var r)
            ? r
            : throw new FormatException("Value must be non-empty entityType, U+001F separator, then non-empty entityId.");
    }

    /// <summary>Returns a compact human-readable form <c>entityType: entityId</c> (not the opaque token; see <see cref="ToOpaqueToken"/>).</summary>
    /// <returns>Type and id separated by <c>": "</c>.</returns>
    public override string ToString() => $"{EntityType}: {EntityId}";

    /// <summary>Resolves the persisted entity type name for <paramref name="type"/> using <see cref="EntityRefLogicalTypeAttribute"/> or CLR metadata.</summary>
    /// <param name="type">CLR type used when constructing references via <see cref="For{T}(object[])"/>.</param>
    /// <returns>Stable logical name or CLR full name / short name.</returns>
    internal static string ResolveLogicalType(Type type) =>
        LogicalTypeCache.GetOrAdd(type, static t => {
            var attr = t.GetCustomAttribute<EntityRefLogicalTypeAttribute>();
            if (attr?.Name is { Length: > 0 } n)
                return n;

            return t.FullName ?? t.Name;
        });
}
