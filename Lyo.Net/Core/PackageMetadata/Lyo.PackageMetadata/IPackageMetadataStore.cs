namespace Lyo.PackageMetadata;

/// <summary>Resolves package catalog metadata for a stack frame. Implementations may be in-memory, Postgres-backed, or similar.</summary>
/// <remarks>
/// Third-party implementations must ship <see cref="TryGetManyForStrippedMethodPrefixesAsync" /> alongside <see cref="TryGetForFrameAsync" /> (see README for the contract).
/// Matching uses <see cref="StringComparison.Ordinal" /> StartsWith on the stripped method versus registered prefixes; the longest registered prefix wins. The lookup
/// methods expose a separate frame-namespace string that is <strong>reserved</strong>; it is not used for matching today.
/// </remarks>
public interface IPackageMetadataStore
{
    /// <summary>
    /// Returns package metadata when a registered namespace prefix matches <paramref name="strippedMethodPrefix" /> (longest prefix wins); otherwise
    /// <see langword="null" />.
    /// </summary>
    /// <param name="namespacePrefix">Reserved. Matching does not consult the frame CLR namespace segment today.</param>
    /// <param name="strippedMethodPrefix">Method path with generic arity markers removed, as used for classification.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<PackageMetadata?> TryGetForFrameAsync(string namespacePrefix, string strippedMethodPrefix, CancellationToken ct = default);

    /// <summary>Resolves package metadata for many stripped method paths in one store call when the store can batch.</summary>
    /// <remarks>
    /// For each distinct key in <paramref name="strippedMethodPrefixes" />, yields the same metadata as calling <see cref="TryGetForFrameAsync" /> with that string as
    /// <c>strippedMethodPrefix</c> (longest registered namespace prefix wins; the frame namespace prefix argument stays unused). The map omits duplicates: each key appears once.
    /// When no prefix matches a key, the value is <see langword="null" />. An empty input list yields an empty map.
    /// </remarks>
    ValueTask<IReadOnlyDictionary<string, PackageMetadata?>> TryGetManyForStrippedMethodPrefixesAsync(IReadOnlyList<string> strippedMethodPrefixes, CancellationToken ct = default);

    /// <summary>Registers or replaces package rows and stack prefixes in bulk (same semantics as repeated single registration; Postgres updates <c>updated_at</c>).</summary>
    Task RegisterManyAsync(IReadOnlyList<PackageMetadataRegistration> registrations, CancellationToken ct = default);
}