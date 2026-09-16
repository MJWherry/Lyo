using Lyo.Exceptions;
using Lyo.PackageMetadata.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Lyo.Common.Core.Identifiers;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>PostgreSQL <see cref="IPackageMetadataStore" /> (longest matching prefix wins).</summary>
public sealed class PostgresPackageMetadataStore : IPackageMetadataStore
{
    /// <remarks>Snapshot plus <see cref="_mutationVersion" /> together.</remarks>
    private readonly Lock _catalogLock = new();

    private readonly IDbContextFactory<PackageMetadataDbContext> _contextFactory;
    private readonly PostgresPackageMetadataOptions _options;

    private PrefixCatalogSnapshot? _catalogSnapshot;

    private long _mutationVersion;

    private int _prefixCatalogHitCount;

    private int _prefixCatalogLoadCount;

    internal int PrefixCatalogHitCount => Volatile.Read(ref _prefixCatalogHitCount);

    internal int PrefixCatalogLoadCount => Volatile.Read(ref _prefixCatalogLoadCount);

    /// <inheritdoc cref="PostgresPackageMetadataStore" />
    public PostgresPackageMetadataStore(IDbContextFactory<PackageMetadataDbContext> contextFactory, PostgresPackageMetadataOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
        _options = options ?? new PostgresPackageMetadataOptions();
    }

    /// <inheritdoc />
    public async ValueTask<PackageMetadata?> TryGetForFrameAsync(string namespacePrefix, string strippedMethodPrefix, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(strippedMethodPrefix);
        _ = namespacePrefix;
        var map = await TryGetManyForStrippedMethodPrefixesAsync([strippedMethodPrefix], ct).ConfigureAwait(false);
        return map[strippedMethodPrefix];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<string, PackageMetadata?>> TryGetManyForStrippedMethodPrefixesAsync(
        IReadOnlyList<string> strippedMethodPrefixes,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(strippedMethodPrefixes);
        var dict = new Dictionary<string, PackageMetadata?>(strippedMethodPrefixes.Count, StringComparer.Ordinal);
        if (strippedMethodPrefixes.Count == 0)
            return dict;

        var orderedModels = _options.PrefixCatalogCaching == PostgresPrefixCatalogCachingMode.InvalidateOnRegisterManyOrClear
            ? await ResolveOrderedModelsWithCatalogCacheAsync(ct).ConfigureAwait(false)
            : await LoadOrderedModelsColdAsync(ct).ConfigureAwait(false);

        foreach (var key in strippedMethodPrefixes) {
            ct.ThrowIfCancellationRequested();
            ArgumentHelpers.ThrowIfNull(key);
            if (dict.ContainsKey(key))
                continue;

            dict[key] = PackageMetadataPrefixMatch.MatchLongest(orderedModels, key);
        }

        return dict;
    }

    /// <inheritdoc />
    public async Task RegisterManyAsync(IReadOnlyList<PackageMetadataRegistration> registrations, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(registrations);
        if (registrations.Count == 0)
            return;

        var folded = FoldRegistrations(registrations);
        await using var db = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var utc = DateTime.UtcNow;
        var ids = new Guid[folded.Count];
        var claimed = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < folded.Count; i++) {
            ids[i] = folded[i].Package.Id;
            foreach (var prefix in folded[i].Prefixes)
                claimed.Add(prefix);
        }

        var existing = await db.Packages.Include(p => p.StackPrefixes).Where(p => ids.Contains(p.Id)).ToListAsync(ct).ConfigureAwait(false);
        var claimedList = claimed.ToList();
        var externalPrefixes = claimedList.Count == 0
            ? []
            : await db.StackPrefixes.Where(p => claimedList.Contains(p.NormalizedPrefix) && !ids.Contains(p.PackageMetadataId)).ToListAsync(ct).ConfigureAwait(false);
        var byId = existing.ToDictionary(e => e.Id);
        var toInsert = new List<PackageStackPrefixEntity>();
        foreach (var item in folded) {
            ct.ThrowIfCancellationRequested();
            var pkg = item.Package;
            if (!byId.TryGetValue(pkg.Id, out var entity)) {
                var created = pkg.CreatedAt ?? utc;
                var updated = pkg.UpdatedAt ?? utc;
                entity = new() { CreatedAt = created, UpdatedAt = updated };
                PackageMetadataMapping.CopyContentFromModel(pkg, entity);
                entity.CreatedAt = created;
                entity.UpdatedAt = updated;
                db.Packages.Add(entity);
                byId[entity.Id] = entity;
            }
            else {
                PackageMetadataMapping.CopyContentFromModel(pkg, entity);
                entity.UpdatedAt = utc;
            }

            var desired = new HashSet<string>(item.Prefixes, StringComparer.Ordinal);
            var remove = entity.StackPrefixes.Where(p => !desired.Contains(p.NormalizedPrefix)).ToList();
            if (remove.Count > 0) {
                db.StackPrefixes.RemoveRange(remove);
                foreach (var prefix in remove)
                    entity.StackPrefixes.Remove(prefix);
            }

            var have = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prefix in entity.StackPrefixes)
                have.Add(prefix.NormalizedPrefix);

            foreach (var prefix in item.Prefixes) {
                if (!have.Add(prefix))
                    continue;

                toInsert.Add(new() { Id = LyoGuid.CreateV7(), PackageMetadataId = entity.Id, NormalizedPrefix = prefix });
            }
        }

        if (externalPrefixes.Count > 0)
            db.StackPrefixes.RemoveRange(externalPrefixes);

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (toInsert.Count > 0) {
            db.StackPrefixes.AddRange(toInsert);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        await tx.CommitAsync(ct).ConfigureAwait(false);
        InvalidateCatalogSnapshot();
    }

    /// <summary>Clears the in-process ordered-prefix snapshot so the next bulk lookup reloads from PostgreSQL.</summary>
    /// <remarks>
    /// Call this when another process or tool mutates <c>package_metadata</c> tables without going through <see cref="RegisterManyAsync" />. When
    /// <see cref="PostgresPackageMetadataOptions.PrefixCatalogCaching" /> is <see cref="PostgresPrefixCatalogCachingMode.Disabled" />, there is no snapshot to drop; the method only
    /// bumps an internal generation counter (harmless for lookups).
    /// </remarks>
    public void ClearPrefixCatalogCache() => InvalidateCatalogSnapshot();

    private async Task<List<(string Prefix, PackageMetadata Metadata)>> LoadOrderedModelsColdAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _prefixCatalogLoadCount);
        await using var db = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await QueryAndMaterializeOrderedModelsAsync(db, ct).ConfigureAwait(false);
    }

    private async Task<List<(string Prefix, PackageMetadata Metadata)>> ResolveOrderedModelsWithCatalogCacheAsync(CancellationToken ct)
    {
        while (true) {
            long version;
            lock (_catalogLock) {
                if (_catalogSnapshot is { MutationVersion: var v } snap && v == _mutationVersion) {
                    Interlocked.Increment(ref _prefixCatalogHitCount);
                    return snap.Ordered;
                }

                version = _mutationVersion;
            }

            Interlocked.Increment(ref _prefixCatalogLoadCount);
            await using var db = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var freshOrdered = await QueryAndMaterializeOrderedModelsAsync(db, ct).ConfigureAwait(false);
            lock (_catalogLock) {
                if (_mutationVersion != version)
                    continue;

                if (_catalogSnapshot is not null && _catalogSnapshot.MutationVersion == _mutationVersion)
                    return _catalogSnapshot.Ordered;

                var immutableCopy = freshOrdered.ConvertAll(static x => x);
                _catalogSnapshot = new(version, immutableCopy);
                return immutableCopy;
            }
        }
    }

    private static List<FoldedRegistration> FoldRegistrations(IReadOnlyList<PackageMetadataRegistration> registrations)
    {
        var order = new List<Guid>();
        var map = new Dictionary<Guid, FoldedRegistration>();
        foreach (var reg in registrations) {
            ArgumentHelpers.ThrowIfNull(reg);
            var package = reg.Package;
            ArgumentHelpers.ThrowIfNull(package);
            var prefixes = PackageMetadataPrefixMatch.NormalizePrefixes(reg.NamespacePrefixes);
            var claimed = new HashSet<string>(prefixes, StringComparer.Ordinal);
            foreach (var existing in map.Values)
                existing.Prefixes.RemoveAll(claimed.Contains);

            if (!map.TryGetValue(package.Id, out var folded)) {
                folded = new(package);
                map.Add(package.Id, folded);
                order.Add(package.Id);
            }
            else
                folded.Package = package;

            folded.Prefixes.Clear();
            folded.Prefixes.AddRange(prefixes);
        }

        var foldedList = new List<FoldedRegistration>(order.Count);
        foreach (var id in order)
            foldedList.Add(map[id]);

        return foldedList;
    }

    private sealed class FoldedRegistration(PackageMetadata package)
    {
        public PackageMetadata Package { get; set; } = package;

        public List<string> Prefixes { get; } = [];
    }

    private static async Task<List<(string Prefix, PackageMetadata Metadata)>> QueryAndMaterializeOrderedModelsAsync(PackageMetadataDbContext db, CancellationToken ct)
    {
        var rows = await db.StackPrefixes.AsNoTracking()
            .Include(p => p.Package)
            .OrderByDescending(p => p.NormalizedPrefix.Length)
            .ThenBy(p => p.NormalizedPrefix)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var orderedModels = new List<(string Prefix, PackageMetadata Metadata)>(rows.Count);
        orderedModels.AddRange(rows.Select(row => (row.NormalizedPrefix, row.Package.ToModel())));
        return orderedModels;
    }

    private void InvalidateCatalogSnapshot()
    {
        lock (_catalogLock) {
            _mutationVersion++;
            _catalogSnapshot = null;
        }
    }

    private sealed record PrefixCatalogSnapshot(long MutationVersion, List<(string Prefix, PackageMetadata Metadata)> Ordered);
}