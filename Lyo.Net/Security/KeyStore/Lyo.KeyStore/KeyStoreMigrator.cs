using Lyo.Exceptions;
using KeyNotFoundException = Lyo.KeyStore.Exceptions.KeyNotFoundException;

#if NET10_0_OR_GREATER
using System.Security.Cryptography;
#endif

namespace Lyo.KeyStore;

/// <summary>
/// Copies key-encryption keys from one <see cref="IKeyStore" /> to another. Version strings are preserved with <c>AddKey</c>. The source is not modified. The source must
/// implement <see cref="IKeyInventoryStore" />.
/// </summary>
public static class KeyStoreMigrator
{
    /// <summary>Copies every version of <paramref name="keyIds" /> onto <paramref name="destination" /> and verifies each read-back.</summary>
    /// <param name="source">Store to read. Must implement <see cref="IKeyInventoryStore" />.</param>
    /// <param name="destination">Store to write.</param>
    /// <param name="keyIds">Key ids to copy. A missing id throws before any destination write.</param>
    /// <param name="progress">Optional per-version progress. Null reports nothing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How many key ids and versions were copied or skipped.</returns>
    public static async Task<KeyStoreMigrationResult> CopyAsync(
        IKeyStore source,
        IKeyStore destination,
        IReadOnlyCollection<string> keyIds,
        IProgress<KeyStoreMigrationProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(destination);
        ArgumentHelpers.ThrowIfNull(keyIds);
        if (source is not IKeyInventoryStore inventory)
            throw new InvalidOperationException("Source key store does not implement IKeyInventoryStore.");

        var ids = keyIds as IReadOnlyList<string> ?? keyIds.ToArray();
        var versionsCopied = 0;
        var versionsSkipped = 0;
        var keysCopied = 0;
        for (var i = 0; i < ids.Count; i++) {
            ct.ThrowIfCancellationRequested();
            var keyId = ids[i];
            var versions = await inventory.GetAvailableVersionsAsync(keyId, ct).ConfigureAwait(false);
            if (versions.Count == 0)
                throw new KeyNotFoundException($"Key '{keyId}' was not found on the source.");

            var sourceCurrent = await source.GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
            var destinationCurrent = await destination.GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
            for (var v = 0; v < versions.Count; v++) {
                ct.ThrowIfCancellationRequested();
                var version = versions[v];
                var material = await source.GetKeyAsync(keyId, version, ct).ConfigureAwait(false);
                if (material is null)
                    throw new KeyNotFoundException($"Key '{keyId}' version '{version}' was not found on the source.");

                KeyStoreMigrationItemOutcome outcome;
                if (await destination.HasKeyAsync(keyId, version, ct).ConfigureAwait(false)) {
                    var existing = await destination.GetKeyAsync(keyId, version, ct).ConfigureAwait(false);
                    if (existing is null || !FixedTimeEquals(existing, material))
                        throw new InvalidOperationException($"Destination key '{keyId}' version '{version}' already exists with different bytes.");

                    outcome = KeyStoreMigrationItemOutcome.Skipped;
                    versionsSkipped++;
                }
                else {
                    await destination.AddKeyAsync(keyId, version, material, ct).ConfigureAwait(false);
                    var roundTrip = await destination.GetKeyAsync(keyId, version, ct).ConfigureAwait(false);
                    if (roundTrip is null || !FixedTimeEquals(roundTrip, material))
                        throw new InvalidOperationException($"Destination key '{keyId}' version '{version}' did not round-trip.");

                    outcome = KeyStoreMigrationItemOutcome.Copied;
                    versionsCopied++;
                }

                var metadata = await source.GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
                if (metadata is not null)
                    await destination.SetKeyMetadataAsync(keyId, version, metadata, ct).ConfigureAwait(false);

                var isLast = v == versions.Count - 1;
                if (isLast)
                    await AlignCurrentVersionAsync(destination, keyId, sourceCurrent, destinationCurrent, ct).ConfigureAwait(false);

                var keysCompleted = keysCopied + (isLast ? 1 : 0);
                progress?.Report(new(keysCompleted, ids.Count, keyId, version, outcome));
            }

            keysCopied++;
        }

        return new(keysCopied, versionsCopied, versionsSkipped);
    }

    /// <summary>Copies every key id the source inventory returns.</summary>
    /// <param name="source">Store to read. Must implement <see cref="IKeyInventoryStore" />.</param>
    /// <param name="destination">Store to write.</param>
    /// <param name="progress">Optional per-version progress. Null reports nothing.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<KeyStoreMigrationResult> CopyInventoryAsync(
        IKeyStore source,
        IKeyStore destination,
        IProgress<KeyStoreMigrationProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        if (source is not IKeyInventoryStore inventory)
            throw new InvalidOperationException("Source key store does not implement IKeyInventoryStore.");

        var ids = await inventory.GetAvailableKeyIdsAsync(ct).ConfigureAwait(false);
        return await CopyAsync(source, destination, ids, progress, ct).ConfigureAwait(false);
    }

    private static async Task AlignCurrentVersionAsync(IKeyStore destination, string keyId, string? sourceCurrent, string? destinationCurrentBefore, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceCurrent))
            return;

        if (string.IsNullOrWhiteSpace(destinationCurrentBefore)) {
            await destination.SetCurrentVersionAsync(keyId, sourceCurrent, ct).ConfigureAwait(false);
            return;
        }

        if (!string.Equals(destinationCurrentBefore, sourceCurrent, StringComparison.Ordinal))
            throw new InvalidOperationException($"Destination key '{keyId}' current version is '{destinationCurrentBefore}', not '{sourceCurrent}'.");
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
#if NET10_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(left, right);
#else
        if (left.Length != right.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
            diff |= left[i] ^ right[i];

        return diff == 0;
#endif
    }
}

/// <summary>How a copied key version finished.</summary>
public enum KeyStoreMigrationItemOutcome
{
    /// <summary>The version was written and verified.</summary>
    Copied,

    /// <summary>The destination already had the same bytes.</summary>
    Skipped,

    /// <summary>The version could not be copied.</summary>
    Failed
}

/// <summary>One version finished copying.</summary>
/// <param name="KeysCompleted">Key ids fully finished so far.</param>
/// <param name="KeysTotal">Key ids in this copy.</param>
/// <param name="KeyId">Key id this report describes.</param>
/// <param name="Version">Version this report describes.</param>
/// <param name="Outcome">How that version finished.</param>
public sealed record KeyStoreMigrationProgress(int KeysCompleted, int KeysTotal, string KeyId, string Version, KeyStoreMigrationItemOutcome Outcome);

/// <summary>Counts from a finished key-store copy.</summary>
/// <param name="KeysCopied">Key ids that finished.</param>
/// <param name="VersionsCopied">Versions written and verified.</param>
/// <param name="VersionsSkipped">Versions already present with the same bytes.</param>
public sealed record KeyStoreMigrationResult(int KeysCopied, int VersionsCopied, int VersionsSkipped);