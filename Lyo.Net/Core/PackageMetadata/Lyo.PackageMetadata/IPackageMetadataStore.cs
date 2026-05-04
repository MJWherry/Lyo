namespace Lyo.PackageMetadata;

/// <summary>Resolves NuGet-style package metadata for a stack frame. Implementations may be in-memory, backed by Postgres, etc.</summary>
/// <remarks>
/// Third-party implementations must ship <see cref="TryGetManyForStrippedMethodPrefixesAsync"/> alongside
/// <see cref="TryGetForFrameAsync"/> (see README for contract). Matching uses <see cref="StringComparison.Ordinal" /> StartsWith semantics on the stripped method vs registered prefixes where the longest registered prefix wins.
/// The lookup methods expose a separate frame-namespace string parameter that is <strong>reserved</strong>; it is not used for matching today.
/// </remarks>
public interface IPackageMetadataStore
{
    /// <summary>Returns package metadata when a registered namespace prefix matches <paramref name="strippedMethodPrefix" /> (longest prefix wins), else <see langword="null" />.</summary>
    /// <param name="namespacePrefix">Reserved. Matching does not consult the frame's CLR namespace segment today.</param>
    /// <param name="strippedMethodPrefix">Method path with generic arity markers removed, as used for classification.</param>
    ValueTask<PackageMetadata?> TryGetForFrameAsync(string namespacePrefix, string strippedMethodPrefix, CancellationToken cancellationToken = default);

    /// <summary>Resolves package metadata for many stripped method paths in one store operation where possible.</summary>
    /// <remarks>
    /// For each distinct key in <paramref name="strippedMethodPrefixes"/>, yields the same metadata as calling <see cref="TryGetForFrameAsync"/> with that string as <c>strippedMethodPrefix</c>
    /// (longest registered namespace prefix wins; the frame namespace prefix argument remains unused). Returned map omits duplicates: each key appears once.
    /// When no prefix matches a key, the value is <see langword="null" />. An empty input list yields an empty map.
    /// </remarks>
    ValueTask<IReadOnlyDictionary<string, PackageMetadata?>> TryGetManyForStrippedMethodPrefixesAsync(IReadOnlyList<string> strippedMethodPrefixes,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk register or replace package rows and stack prefixes (same semantics as repeated single registration; Postgres updates <c>updated_at</c>).</summary>
    Task RegisterManyAsync(IReadOnlyList<PackageMetadataRegistration> registrations, CancellationToken cancellationToken = default);
}
