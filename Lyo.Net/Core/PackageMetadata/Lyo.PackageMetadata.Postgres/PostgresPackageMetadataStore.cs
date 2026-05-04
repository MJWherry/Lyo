using System.Threading;
using Lyo.Exceptions;
using Lyo.PackageMetadata;
using Lyo.PackageMetadata.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.PackageMetadata.Postgres;

/// <summary><see cref="IPackageMetadataStore" /> backed by PostgreSQL (longest matching prefix wins).</summary>
public sealed class PostgresPackageMetadataStore : IPackageMetadataStore
{
    private readonly IDbContextFactory<PackageMetadataDbContext> _contextFactory;
    private readonly PostgresPackageMetadataOptions _options;

    /// <remarks>Snapshot + <see cref="_mutationVersion" /> together.</remarks>
    private readonly object _catalogLock = new();

    private PrefixCatalogSnapshot? _catalogSnapshot;

    private long _mutationVersion;

    private int _prefixCatalogHitCount;

    private int _prefixCatalogLoadCount;

    /// <inheritdoc cref="PostgresPackageMetadataStore" />
    public PostgresPackageMetadataStore(IDbContextFactory<PackageMetadataDbContext> contextFactory,
        PostgresPackageMetadataOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
        _options = options ?? new PostgresPackageMetadataOptions();
    }

    internal int PrefixCatalogHitCount => Volatile.Read(ref _prefixCatalogHitCount);

    internal int PrefixCatalogLoadCount => Volatile.Read(ref _prefixCatalogLoadCount);

    /// <summary>Drops the in-process ordered-prefix snapshot so the next bulk lookup reloads from PostgreSQL.</summary>
    /// <remarks>
    /// Use when another process or tool mutates <c>package_metadata</c> tables without going through <see cref="RegisterManyAsync" />.
    /// When <see cref="PostgresPackageMetadataOptions.PrefixCatalogCaching" /> is <see cref="PostgresPrefixCatalogCachingMode.Disabled" />, there is no snapshot to clear; the method only bumps an internal generation counter (harmless for lookups).
    /// </remarks>
    public void ClearPrefixCatalogCache()
        => InvalidateCatalogSnapshot();

    private sealed record PrefixCatalogSnapshot(long MutationVersion, List<(string Prefix, PackageMetadata Metadata)> Ordered);

    /// <inheritdoc />
    public async ValueTask<PackageMetadata?> TryGetForFrameAsync(string namespacePrefix, string strippedMethodPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(strippedMethodPrefix);
        _ = namespacePrefix;
        var map = await TryGetManyForStrippedMethodPrefixesAsync([strippedMethodPrefix], cancellationToken).ConfigureAwait(false);
        return map[strippedMethodPrefix];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<string, PackageMetadata?>> TryGetManyForStrippedMethodPrefixesAsync(
        IReadOnlyList<string> strippedMethodPrefixes, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(strippedMethodPrefixes);
        var dict = new Dictionary<string, PackageMetadata?>(strippedMethodPrefixes.Count, StringComparer.Ordinal);
        if (strippedMethodPrefixes.Count == 0)
            return dict;

        var orderedModels = _options.PrefixCatalogCaching == PostgresPrefixCatalogCachingMode.InvalidateOnRegisterManyOrClear
            ? await ResolveOrderedModelsWithCatalogCacheAsync(cancellationToken).ConfigureAwait(false)
            : await LoadOrderedModelsColdAsync(cancellationToken).ConfigureAwait(false);

        foreach (var key in strippedMethodPrefixes) {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentHelpers.ThrowIfNull(key);

            if (dict.ContainsKey(key))
                continue;

            dict[key] = PackageMetadataPrefixMatch.MatchLongest(orderedModels, key);
        }

        return dict;
    }

    private async Task<List<(string Prefix, PackageMetadata Metadata)>> LoadOrderedModelsColdAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _prefixCatalogLoadCount);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await QueryAndMaterializeOrderedModelsAsync(db, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<(string Prefix, PackageMetadata Metadata)>> ResolveOrderedModelsWithCatalogCacheAsync(
        CancellationToken cancellationToken)
    {
        lock (_catalogLock) {
            if (_catalogSnapshot is { MutationVersion: var v } snap && v == _mutationVersion) {
                Interlocked.Increment(ref _prefixCatalogHitCount);
                return snap.Ordered;
            }
        }

        Interlocked.Increment(ref _prefixCatalogLoadCount);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var freshOrdered = await QueryAndMaterializeOrderedModelsAsync(db, cancellationToken).ConfigureAwait(false);

        lock (_catalogLock) {
            if (_catalogSnapshot is not null && _catalogSnapshot.MutationVersion == _mutationVersion)
                return _catalogSnapshot.Ordered;

            var immutableCopy = freshOrdered.ConvertAll(static x => x);
            _catalogSnapshot = new PrefixCatalogSnapshot(_mutationVersion, immutableCopy);
            return immutableCopy;
        }
    }

    private static async Task<List<(string Prefix, PackageMetadata Metadata)>> QueryAndMaterializeOrderedModelsAsync(
        PackageMetadataDbContext db,
        CancellationToken cancellationToken)
    {
        var rows = await db.StackPrefixes.AsNoTracking()
            .Include(p => p.Package)
            .OrderByDescending(p => p.NormalizedPrefix.Length)
            .ThenBy(p => p.NormalizedPrefix)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var orderedModels = new List<(string Prefix, PackageMetadata Metadata)>(rows.Count);
        foreach (var row in rows) {
            if (row.Package is null)
                continue;

            orderedModels.Add((row.NormalizedPrefix, row.Package.ToModel()));
        }

        return orderedModels;
    }

    /// <inheritdoc />
    public async Task RegisterManyAsync(IReadOnlyList<PackageMetadataRegistration> registrations,
        CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(registrations);
        if (registrations.Count == 0)
            return;

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var utc = DateTime.UtcNow;

        foreach (var reg in registrations) {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentHelpers.ThrowIfNull(reg);
            var pkg = reg.Package;
            ArgumentHelpers.ThrowIfNull(pkg);

            var entity = await db.Packages
                .Include(p => p.StackPrefixes)
                .FirstOrDefaultAsync(p => p.Id == pkg.Id, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null) {
                var created = pkg.CreatedAt ?? utc;
                var updated = pkg.UpdatedAt ?? utc;
                entity = new PackageMetadataEntity {
                    CreatedAt = created,
                    UpdatedAt = updated
                };
                PackageMetadataMapping.CopyContentFromModel(pkg, entity);
                entity.CreatedAt = created;
                entity.UpdatedAt = updated;
                db.Packages.Add(entity);
            }
            else {
                PackageMetadataMapping.CopyContentFromModel(pkg, entity);
                entity.UpdatedAt = utc;
                db.StackPrefixes.RemoveRange(entity.StackPrefixes);
                entity.StackPrefixes.Clear();
            }

            foreach (var raw in reg.NamespacePrefixes) {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var p = raw.Trim();
                if (!p.EndsWith(".", StringComparison.Ordinal))
                    p += ".";

                entity.StackPrefixes.Add(new PackageStackPrefixEntity {
                    Id = Guid.NewGuid(),
                    PackageMetadataId = entity.Id,
                    NormalizedPrefix = p
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        InvalidateCatalogSnapshot();
    }

    private void InvalidateCatalogSnapshot()
    {
        lock (_catalogLock) {
            _mutationVersion++;
            _catalogSnapshot = null;
        }
    }
}
