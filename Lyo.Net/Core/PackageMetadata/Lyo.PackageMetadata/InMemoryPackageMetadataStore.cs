using Lyo.Exceptions;

namespace Lyo.PackageMetadata;

/// <summary>Thread-safe in-memory <see cref="IPackageMetadataStore" /> keyed by namespace prefixes (longest match wins). Use for tests or seed data.</summary>
public sealed class InMemoryPackageMetadataStore : IPackageMetadataStore
{
    private readonly object _gate = new();
    private readonly List<(string Prefix, PackageMetadata Meta)> _entries = [];

    /// <summary>Registers prefixes for one package. Prefixes are normalised to end with <c>.</c> when missing.</summary>
    public void Register(IReadOnlyList<string> namespacePrefixes, PackageMetadata package)
    {
        ArgumentHelpers.ThrowIfNull(namespacePrefixes);
        ArgumentHelpers.ThrowIfNull(package);

        lock (_gate) {
            AddPrefixes(namespacePrefixes, package);
            SortEntries();
        }
    }

    /// <inheritdoc />
    public Task RegisterManyAsync(IReadOnlyList<PackageMetadataRegistration> registrations, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(registrations);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) {
            foreach (var reg in registrations) {
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentHelpers.ThrowIfNull(reg);
                AddPrefixes(reg.NamespacePrefixes, reg.Package);
            }

            SortEntries();
        }

        return Task.CompletedTask;
    }

    private void AddPrefixes(IReadOnlyList<string> namespacePrefixes, PackageMetadata package)
    {
        _entries.RemoveAll(t => t.Meta.Id == package.Id);

        foreach (var raw in namespacePrefixes) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var p = raw.Trim();
            if (!p.EndsWith(".", StringComparison.Ordinal))
                p += ".";

            _entries.Add((p, package));
        }
    }

    private void SortEntries()
    {
        _entries.Sort(static (a, b) => {
            var c = b.Prefix.Length.CompareTo(a.Prefix.Length);
            return c != 0 ? c : string.CompareOrdinal(a.Prefix, b.Prefix);
        });
    }

    /// <inheritdoc />
    public ValueTask<PackageMetadata?> TryGetForFrameAsync(string namespacePrefix, string strippedMethodPrefix, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(strippedMethodPrefix);
        _ = namespacePrefix;
        (string Prefix, PackageMetadata Meta)[] snapshot;
        lock (_gate)
            snapshot = _entries.ToArray();

        return new(PackageMetadataPrefixMatch.MatchLongest(snapshot, strippedMethodPrefix));
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyDictionary<string, PackageMetadata?>> TryGetManyForStrippedMethodPrefixesAsync(IReadOnlyList<string> strippedMethodPrefixes,
        CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(strippedMethodPrefixes);
        cancellationToken.ThrowIfCancellationRequested();

        var dict = new Dictionary<string, PackageMetadata?>(strippedMethodPrefixes.Count, StringComparer.Ordinal);
        if (strippedMethodPrefixes.Count == 0)
            return new ValueTask<IReadOnlyDictionary<string, PackageMetadata?>>(dict);

        (string Prefix, PackageMetadata Meta)[] snapshot;
        lock (_gate)
            snapshot = _entries.ToArray();

        foreach (var key in strippedMethodPrefixes) {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentHelpers.ThrowIfNull(key);

            if (dict.ContainsKey(key))
                continue;

            dict[key] = PackageMetadataPrefixMatch.MatchLongest(snapshot, key);
        }

        return new(dict);
    }
}
