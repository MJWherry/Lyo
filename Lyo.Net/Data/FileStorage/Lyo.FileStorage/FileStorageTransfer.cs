using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.Hashing;
using Lyo.KeyStore;
using Lyo.Streams;

namespace Lyo.FileStorage;

/// <summary>What a file-storage transfer copies.</summary>
public enum FileStorageTransferScope
{
    /// <summary>Key versions referenced by the selected rows. Objects stay where they are.</summary>
    KeysOnly = 0,

    /// <summary>Referenced keys plus every selected object and its metadata row.</summary>
    Everything = 1
}

/// <summary>How destination ciphertext is keyed.</summary>
public enum FileStorageKeyMode
{
    /// <summary>Copy ciphertext and the same key-encryption keys. Requires <see cref="IFileStoragePhysical"/> on both services.</summary>
    PreserveKeys = 0,

    /// <summary>Mint one new key on the destination and re-save every file under it.</summary>
    NewKey = 1,

    /// <summary>Re-save every file under a key the destination already has. No key material is written.</summary>
    ExistingKey = 2
}

/// <summary>Whether the source file stays after a verified destination copy.</summary>
public enum FileStorageTransferDisposition
{
    /// <summary>Leave the source file in place.</summary>
    Copy = 0,

    /// <summary>After the destination hashes match, tombstone the source file. Source keys stay.</summary>
    Move = 1
}

/// <summary>Which part of a transfer a progress snapshot describes.</summary>
public enum FileStorageTransferPhase
{
    /// <summary>The catalog list or file-id selection has been taken.</summary>
    Listing = 0,

    /// <summary>Key versions are being copied.</summary>
    Keys = 1,

    /// <summary>File objects are being copied.</summary>
    Files = 2
}

/// <summary>What happened to the file named in a progress snapshot.</summary>
public enum FileStorageTransferItemOutcome
{
    /// <summary>Copy of this file has started, or bytes are still moving.</summary>
    Started = 0,

    /// <summary>The file was copied and its hashes matched.</summary>
    Copied = 1,

    /// <summary>The destination already had this file and the recomputed hashes matched.</summary>
    Skipped = 2,

    /// <summary>This file failed. Earlier files stay on the destination. A failed file stays on the source.</summary>
    Failed = 3,

    /// <summary>The destination hashes matched and the source file was tombstoned.</summary>
    Removed = 4
}

/// <summary>Inputs for <c>FileStorageTransfer.TransferAsync</c>.</summary>
/// <param name="Scope">Keys only, or keys plus objects.</param>
/// <param name="KeyMode">Preserve ciphertext, mint a key, or reuse a destination key.</param>
/// <param name="TargetKeyId">Required for <see cref="FileStorageKeyMode.NewKey"/> and <see cref="FileStorageKeyMode.ExistingKey"/>.</param>
/// <param name="TargetKeyVersion">Destination version. New keys default to <c>1</c>. Existing keys default to the destination current version.</param>
/// <param name="ListCap">Maximum catalog rows when <paramref name="FileIds"/> is null. A full page is treated as truncated and throws before any write.</param>
/// <param name="ProgressByteInterval">Minimum new bytes between mid-file progress snapshots. Must be at least 1.</param>
/// <param name="Disposition">Copy leaves the source. Move tombstones it after the destination hashes match.</param>
/// <param name="FileIds">Ids to transfer. Null selects the catalog. Empty throws before any write.</param>
public sealed record FileStorageTransferRequest(
    FileStorageTransferScope Scope,
    FileStorageKeyMode KeyMode,
    string? TargetKeyId = null,
    string? TargetKeyVersion = null,
    int ListCap = int.MaxValue,
    int ProgressByteInterval = 1024 * 1024,
    FileStorageTransferDisposition Disposition = FileStorageTransferDisposition.Copy,
    IReadOnlyCollection<Guid>? FileIds = null);

/// <summary>One side of a transfer: storage, catalog, and key store.</summary>
/// <param name="Storage">File storage service.</param>
/// <param name="Metadata">Catalog for that service. A shared catalog is the same instance on both sides.</param>
/// <param name="Keys">Key store for that service.</param>
public sealed record FileStorageTransferEndpoint(IFileStorageService Storage, IFileMetadataStore Metadata, IKeyStore Keys);

/// <summary>One file the transfer could not copy. Earlier files are left on the destination.</summary>
/// <param name="FileId">File id when the failure happened after the row was known.</param>
/// <param name="Message">Exception message.</param>
public sealed record FileStorageTransferFileError(Guid? FileId, string Message);

/// <summary>Counts from one transfer run. <see cref="FilesFailed"/> greater than zero means the run did not fully succeed.</summary>
/// <param name="FilesCopied">Objects whose destination hashes matched after a write.</param>
/// <param name="FilesSkipped">Rows left unwritten because the recomputed destination hashes already matched.</param>
/// <param name="FilesFailed">Rows that failed. Their destination objects are not rolled back, and they stay on the source.</param>
/// <param name="KeysCopied">Key versions written on the destination, or one when a new key was minted.</param>
/// <param name="Errors">Per-file failures. Empty when every row succeeded or was skipped.</param>
/// <param name="FilesRemoved">Source files tombstoned after a verified copy or skip. Zero for <see cref="FileStorageTransferDisposition.Copy"/>.</param>
public sealed record FileStorageTransferResult(
    int FilesCopied,
    int FilesSkipped,
    int FilesFailed,
    int KeysCopied,
    IReadOnlyList<FileStorageTransferFileError> Errors,
    int FilesRemoved = 0);

/// <summary>One immutable progress snapshot. Null progress reports nothing.</summary>
/// <param name="Phase">Listing, keys, or files.</param>
/// <param name="FilesCompleted">File rows finished, including skips and failures.</param>
/// <param name="FilesTotal">Rows selected to copy. Zero until listing finishes, and zero for a keys-only run.</param>
/// <param name="FilesCopied">Objects copied so far.</param>
/// <param name="FilesSkipped">Rows skipped so far.</param>
/// <param name="FilesFailed">Rows failed so far.</param>
/// <param name="BytesCopied">Bytes copied so far. Stored size for a raw copy, plaintext size for a re-save.</param>
/// <param name="BytesTotal">Sum of those sizes across the selected rows.</param>
/// <param name="CurrentFileId">File in progress, when the phase is files.</param>
/// <param name="CurrentFileBytesCopied">Bytes copied for the current file.</param>
/// <param name="CurrentFileBytesTotal">Stored size for a raw copy, plaintext size for a re-save.</param>
/// <param name="KeysCompleted">Key ids finished.</param>
/// <param name="KeysTotal">Key ids selected for copy.</param>
/// <param name="ItemOutcome">What the current file has done. Null during listing and the key phase.</param>
/// <param name="FilesRemoved">Source files tombstoned so far.</param>
public sealed record FileStorageTransferProgress(
    FileStorageTransferPhase Phase,
    int FilesCompleted,
    int FilesTotal,
    int FilesCopied,
    int FilesSkipped,
    int FilesFailed,
    long BytesCopied,
    long BytesTotal,
    Guid? CurrentFileId,
    long CurrentFileBytesCopied,
    long CurrentFileBytesTotal,
    int KeysCompleted,
    int KeysTotal,
    FileStorageTransferItemOutcome? ItemOutcome,
    int FilesRemoved);

/// <summary>
/// Copies or moves files from one storage service onto another. Move tombstones a source file only after the destination hashes match. A later file failure does not roll back
/// earlier destination files, and it leaves the failed file on the source.
/// </summary>
public static class FileStorageTransfer
{
    /// <summary>
    /// Transfers <paramref name="source"/> onto <paramref name="destination"/> using <paramref name="request"/>.
    /// Invalid mode combinations and a truncated catalog throw before any destination write.
    /// </summary>
    /// <param name="source">Service being copied. Move tombstones files that land on the destination. Copy does not modify it.</param>
    /// <param name="destination">Service that receives keys and, when the scope includes objects, files.</param>
    /// <param name="request">Scope, key mode, disposition, and limits.</param>
    /// <param name="ct">Cancellation token. Cancellation throws and is not a successful result.</param>
    public static Task<FileStorageTransferResult> TransferAsync(
        FileStorageTransferEndpoint source,
        FileStorageTransferEndpoint destination,
        FileStorageTransferRequest request,
        CancellationToken ct = default) =>
        TransferAsync(source, destination, request, progress: null, ct);

    /// <summary>
    /// Transfers <paramref name="source"/> onto <paramref name="destination"/> and reports progress.
    /// </summary>
    /// <param name="source">Service being copied. Move tombstones files that land on the destination. Copy does not modify it.</param>
    /// <param name="destination">Service that receives keys and, when the scope includes objects, files.</param>
    /// <param name="request">Scope, key mode, disposition, and limits.</param>
    /// <param name="progress">Listener. A throw from the listener stops the transfer.</param>
    /// <param name="ct">Cancellation token. Cancellation throws and is not a successful result.</param>
    public static async Task<FileStorageTransferResult> TransferAsync(
        FileStorageTransferEndpoint source,
        FileStorageTransferEndpoint destination,
        FileStorageTransferRequest request,
        IProgress<FileStorageTransferProgress>? progress,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(destination);
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(source.Storage);
        ArgumentHelpers.ThrowIfNull(source.Metadata);
        ArgumentHelpers.ThrowIfNull(source.Keys);
        ArgumentHelpers.ThrowIfNull(destination.Storage);
        ArgumentHelpers.ThrowIfNull(destination.Metadata);
        ArgumentHelpers.ThrowIfNull(destination.Keys);
        ct.ThrowIfCancellationRequested();
        if (request.ProgressByteInterval < 1)
            throw new ArgumentOutOfRangeException(nameof(request), "ProgressByteInterval must be at least 1.");

        if (ReferenceEquals(source.Storage, destination.Storage))
            throw new InvalidOperationException("Source and destination file storage services must be different instances.");

        var reencrypt = request.KeyMode is FileStorageKeyMode.NewKey or FileStorageKeyMode.ExistingKey;
        if (request.Scope == FileStorageTransferScope.KeysOnly && reencrypt)
            throw new InvalidOperationException("KeysOnly only supports PreserveKeys. Re-encryption needs the file bytes.");

        if (request.Disposition == FileStorageTransferDisposition.Move && request.Scope == FileStorageTransferScope.KeysOnly)
            throw new InvalidOperationException("Move deletes source files. KeysOnly does not copy files.");

        if (reencrypt && ReferenceEquals(source.Metadata, destination.Metadata))
            throw new InvalidOperationException("NewKey and ExistingKey need a separate destination catalog. A shared catalog cannot hold two ciphertexts for one file id.");

        if (request.Disposition == FileStorageTransferDisposition.Move && ReferenceEquals(source.Metadata, destination.Metadata))
            throw new InvalidOperationException("Move needs a separate destination catalog. A shared catalog cannot drop the source file without hiding it from the destination.");

        if (reencrypt)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.TargetKeyId);

        if (request.KeyMode == FileStorageKeyMode.NewKey && await destination.Keys.HasKeyAsync(request.TargetKeyId!, ct: ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Destination already has key '{request.TargetKeyId}'. NewKey refuses to overwrite it.");

        string? existingVersion = null;
        if (request.KeyMode == FileStorageKeyMode.ExistingKey)
            existingVersion = await RequireExistingKeyAsync(destination.Keys, request.TargetKeyId!, request.TargetKeyVersion, ct).ConfigureAwait(false);

        var preserveBytes = request.Scope == FileStorageTransferScope.Everything && request.KeyMode == FileStorageKeyMode.PreserveKeys;
        if (preserveBytes && (source.Storage is not IFileStoragePhysical || destination.Storage is not IFileStoragePhysical))
            throw new InvalidOperationException("PreserveKeys needs physical access on both services. FTP and SFTP are not supported for a byte copy; use NewKey or ExistingKey.");

        var rows = await SelectRowsAsync(source.Metadata, request, ct).ConfigureAwait(false);
        var copyingFiles = request.Scope == FileStorageTransferScope.Everything;
        var state = new RunState(request.ProgressByteInterval) {
            FilesTotal = copyingFiles ? rows.Count : 0,
            BytesTotal = copyingFiles ? rows.Sum(row => ProgressBytes(request, row)) : 0
        };
        Report(progress, state, FileStorageTransferPhase.Listing, null, null, 0, 0);

        var keysCopied = 0;
        if (request.KeyMode == FileStorageKeyMode.PreserveKeys)
            keysCopied = await CopyReferencedKeysAsync(source, destination, rows, state, progress, ct).ConfigureAwait(false);
        else if (request.KeyMode == FileStorageKeyMode.NewKey)
            keysCopied = await MintKeyAsync(destination.Keys, request.TargetKeyId!, request.TargetKeyVersion ?? "1", state, progress, ct).ConfigureAwait(false);

        if (request.Scope == FileStorageTransferScope.KeysOnly)
            return new(0, 0, 0, keysCopied, []);

        var targetVersion = request.KeyMode switch {
            FileStorageKeyMode.NewKey => request.TargetKeyVersion ?? "1",
            FileStorageKeyMode.ExistingKey => existingVersion!,
            _ => null
        };
        var errors = new List<FileStorageTransferFileError>();
        foreach (var row in rows) {
            ct.ThrowIfCancellationRequested();
            await CopyFileAsync(source, destination, row, request, targetVersion, state, progress, errors, ct).ConfigureAwait(false);
        }

        return new(state.FilesCopied, state.FilesSkipped, state.FilesFailed, keysCopied, errors, state.FilesRemoved);
    }

    private static async Task<List<FileStoreResult>> SelectRowsAsync(IFileMetadataStore metadata, FileStorageTransferRequest request, CancellationToken ct)
    {
        if (request.FileIds is null) {
            var listed = await metadata.ListByPathPrefixAsync(null, includeDescendants: true, request.ListCap, ct).ConfigureAwait(false);
            if (listed.Count == request.ListCap)
                throw new InvalidOperationException(
                    $"Catalog returned {listed.Count} rows, which is the list cap. Raise ListCap or the listing was truncated. Nothing was written.");

            return listed.Where(static row => row.Availability != FileAvailability.PendingDirectUpload).ToList();
        }

        if (request.FileIds.Count == 0)
            throw new ArgumentException("FileIds is empty.", nameof(request));

        var rows = new List<FileStoreResult>();
        foreach (var id in request.FileIds.Distinct()) {
            ct.ThrowIfCancellationRequested();
            FileStoreResult row;
            try {
                row = await metadata.GetMetadataAsync(id, ct).ConfigureAwait(false);
            }
            catch (FileNotFoundException) {
                throw new InvalidOperationException($"Source has no current file {id}. Nothing was written.");
            }

            if (row.Availability is FileAvailability.PendingDirectUpload or FileAvailability.Deleted || row.DeletedAt is not null)
                throw new InvalidOperationException($"Source file {id} is not available to transfer. Nothing was written.");

            rows.Add(row);
        }

        return rows;
    }

    private static async Task<string> RequireExistingKeyAsync(IKeyStore keys, string keyId, string? version, CancellationToken ct)
    {
        if (!await keys.HasKeyAsync(keyId, ct: ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Destination has no key '{keyId}'.");

        var resolved = string.IsNullOrWhiteSpace(version) ? await keys.GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false) : version;
        if (string.IsNullOrWhiteSpace(resolved) || !await keys.HasKeyAsync(keyId, resolved, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Destination key '{keyId}' has no version '{resolved}'.");

        var material = await keys.GetKeyAsync(keyId, resolved, ct).ConfigureAwait(false);
        if (material is null || material.Length == 0)
            throw new InvalidOperationException($"Destination key '{keyId}' version '{resolved}' did not return key bytes.");

        return resolved!;
    }

    private static async Task<int> CopyReferencedKeysAsync(
        FileStorageTransferEndpoint source,
        FileStorageTransferEndpoint destination,
        List<FileStoreResult> rows,
        RunState state,
        IProgress<FileStorageTransferProgress>? progress,
        CancellationToken ct)
    {
        var keyIds = rows
            .Where(static row => row.IsEncrypted && !string.IsNullOrWhiteSpace(row.DataEncryptionKeyId))
            .Select(static row => row.DataEncryptionKeyId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (keyIds.Count == 0)
            return 0;

        var keyProgress = new Progress<KeyStoreMigrationProgress>(snapshot =>
        {
            state.KeysCompleted = snapshot.KeysCompleted;
            state.KeysTotal = snapshot.KeysTotal;
            Report(progress, state, FileStorageTransferPhase.Keys, null, null, 0, 0);
        });
        var copied = await KeyStoreMigrator.CopyAsync(source.Keys, destination.Keys, keyIds, keyProgress, ct).ConfigureAwait(false);
        return copied.VersionsCopied;
    }

    private static async Task<int> MintKeyAsync(
        IKeyStore keys,
        string keyId,
        string version,
        RunState state,
        IProgress<FileStorageTransferProgress>? progress,
        CancellationToken ct)
    {
        var material = SecureKeyGenerator.GenerateKey(32);
        await keys.AddKeyAsync(keyId, version, material, ct).ConfigureAwait(false);
        var stored = await keys.GetKeyAsync(keyId, version, ct).ConfigureAwait(false);
        if (stored is null || !HashingService.Shared.FixedTimeEquals(material, stored))
            throw new InvalidOperationException($"Destination key '{keyId}' version '{version}' did not round-trip.");

        await keys.SetCurrentVersionAsync(keyId, version, ct).ConfigureAwait(false);
        state.KeysCompleted = 1;
        state.KeysTotal = 1;
        Report(progress, state, FileStorageTransferPhase.Keys, null, null, 0, 0);
        return 1;
    }

    private static async Task CopyFileAsync(
        FileStorageTransferEndpoint source,
        FileStorageTransferEndpoint destination,
        FileStoreResult row,
        FileStorageTransferRequest request,
        string? targetVersion,
        RunState state,
        IProgress<FileStorageTransferProgress>? progress,
        List<FileStorageTransferFileError> errors,
        CancellationToken ct)
    {
        var progressTotal = ProgressBytes(request, row);
        state.CurrentFileId = row.Id;
        state.CurrentFileBytesCopied = 0;
        state.CurrentFileBytesTotal = progressTotal;
        Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Started, 0, progressTotal);
        var accounted = false;
        try {
            if (row.OriginalFileHash is not { Length: > 0 })
                throw new InvalidOperationException($"Source file {row.Id} has no OriginalFileHash.");

            if (request.KeyMode == FileStorageKeyMode.PreserveKeys && row.SourceFileHash is not { Length: > 0 })
                throw new InvalidOperationException($"Source file {row.Id} has no SourceFileHash.");

            var wrote = request.KeyMode == FileStorageKeyMode.PreserveKeys
                ? await CopyPreservedBytesAsync(source, destination, row, state, progress, ct).ConfigureAwait(false)
                : await ReencryptAsync(source, destination, row, request, targetVersion!, state, progress, ct).ConfigureAwait(false);
            accounted = true;
            if (wrote) {
                state.FilesCopied++;
                state.FilesCompleted++;
                state.BytesCopied += progressTotal;
                state.CurrentFileBytesCopied = progressTotal;
            }

            if (request.Disposition == FileStorageTransferDisposition.Move) {
                await source.Storage.DeleteFileAsync(row.Id, FileDeletionMode.RemoveObjectAndTombstoneMetadata, ct).ConfigureAwait(false);
                state.FilesRemoved++;
                state.CurrentFileBytesCopied = progressTotal;
                Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Removed, progressTotal, progressTotal);
                return;
            }

            if (wrote)
                Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Copied, progressTotal, progressTotal);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !state.ProgressFailed) {
            state.FilesFailed++;
            if (!accounted)
                state.FilesCompleted++;

            errors.Add(new(row.Id, ex.Message));
            Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Failed, state.CurrentFileBytesCopied, progressTotal);
        }
    }

    private static async Task<bool> CopyPreservedBytesAsync(
        FileStorageTransferEndpoint source,
        FileStorageTransferEndpoint destination,
        FileStoreResult row,
        RunState state,
        IProgress<FileStorageTransferProgress>? progress,
        CancellationToken ct)
    {
        var sourcePhysical = (IFileStoragePhysical)source.Storage;
        var destinationPhysical = (IFileStoragePhysical)destination.Storage;
        var key = ObjectKey(row);
        var objectSize = ObjectSize(row);
        var sharedCatalog = ReferenceEquals(source.Metadata, destination.Metadata);
        if (!sharedCatalog && await DestinationMetadataAsync(destination.Metadata, row.Id, ct).ConfigureAwait(false) is { } existing
            && !HashesEqual(existing.OriginalFileHash, row.OriginalFileHash))
            throw new InvalidOperationException($"Destination file {row.Id} exists with a different OriginalFileHash. It was not overwritten.");

        if (await DestinationAlreadyMatchesAsync(destination, row, preserveBytes: true, ct).ConfigureAwait(false)) {
            NoteSkipped(state, progress, row);
            return false;
        }

        var nextReport = (long)state.ProgressByteInterval;
        var copyProgress = new Progress<long>(copied =>
        {
            state.CurrentFileBytesCopied = copied;
            if (copied < nextReport && copied != objectSize)
                return;

            nextReport = copied + state.ProgressByteInterval;
            Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Started, copied, state.CurrentFileBytesTotal);
        });
        var input = await sourcePhysical.Physical.OpenReadAsync(key, ct).ConfigureAwait(false);
        var output = await destinationPhysical.Physical.OpenCreateAsync(key, ct).ConfigureAwait(false);
        try {
            await input.CopyToAsync(output, progress: copyProgress, ct: ct).ConfigureAwait(false);
        }
        finally {
            await DisposeStreamAsync(output).ConfigureAwait(false);
            await DisposeStreamAsync(input).ConfigureAwait(false);
        }

        var written = await destinationPhysical.Physical.GetLengthAsync(key, ct).ConfigureAwait(false);
        if (written != objectSize)
            throw new InvalidOperationException($"Destination object for {row.Id} is {written} bytes; the source object is {objectSize}.");

        if (!sharedCatalog)
            await destination.Metadata.SaveMetadataAsync(row.Id, row, ct).ConfigureAwait(false);

        await VerifyDestinationAsync(destination, row, preserveBytes: true, ct).ConfigureAwait(false);
        return true;
    }

    private static async Task<bool> ReencryptAsync(
        FileStorageTransferEndpoint source,
        FileStorageTransferEndpoint destination,
        FileStoreResult row,
        FileStorageTransferRequest request,
        string targetVersion,
        RunState state,
        IProgress<FileStorageTransferProgress>? progress,
        CancellationToken ct)
    {
        var targetKeyId = request.TargetKeyId!;
        if (await DestinationMetadataAsync(destination.Metadata, row.Id, ct).ConfigureAwait(false) is { } existing && !HashesEqual(existing.OriginalFileHash, row.OriginalFileHash))
            throw new InvalidOperationException($"Destination file {row.Id} exists with a different OriginalFileHash. It was not overwritten.");

        if (await DestinationAlreadyMatchesAsync(destination, row, preserveBytes: false, ct).ConfigureAwait(false)) {
            NoteSkipped(state, progress, row);
            return false;
        }

        var progressTotal = state.CurrentFileBytesTotal;
        var restoreCurrent = false;
        string? previousCurrent = null;
        if (row.IsEncrypted && request.KeyMode == FileStorageKeyMode.ExistingKey) {
            previousCurrent = await destination.Keys.GetCurrentVersionAsync(targetKeyId, ct).ConfigureAwait(false);
            if (!string.Equals(previousCurrent, targetVersion, StringComparison.Ordinal)) {
                await destination.Keys.SetCurrentVersionAsync(targetKeyId, targetVersion, ct).ConfigureAwait(false);
                restoreCurrent = previousCurrent is not null;
            }
        }

        try {
            var input = await source.Storage.GetFileStreamAsync(row.Id, ct: ct).ConfigureAwait(false);
            if (input is null)
                throw new InvalidOperationException($"Source file {row.Id} did not return a stream.");

            var nextReport = (long)state.ProgressByteInterval;
            var readProgress = new Progress<long>(copied =>
            {
                state.CurrentFileBytesCopied = copied;
                if (copied < nextReport && copied != row.OriginalFileSize)
                    return;

                nextReport = copied + state.ProgressByteInterval;
                Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Started, copied, progressTotal);
            });
            var reporting = new ProgressStream(input, readProgress);
            FileStoreResult saved;
            try {
                saved = await destination.Storage.SaveFromStreamAsync(
                    reporting,
                    row.OriginalFileSize,
                    row.OriginalFileName,
                    row.IsCompressed,
                    row.IsEncrypted,
                    row.IsEncrypted ? targetKeyId : null,
                    row.PathPrefix,
                    chunkSize: null,
                    row.ContentType,
                    row.Charset,
                    row.TenantId,
                    row.Availability,
                    row.Id,
                    row.Metadata,
                    ct)
                .ConfigureAwait(false);
            }
            finally {
                await DisposeStreamAsync(reporting).ConfigureAwait(false);
                await DisposeStreamAsync(input).ConfigureAwait(false);
            }

            if (saved.Id != row.Id)
                throw new InvalidOperationException($"Destination save of {row.Id} returned {saved.Id}. The source id was not overwritten.");

            if (saved.OwnerId != row.OwnerId) {
                var current = await destination.Metadata.GetMetadataAsync(saved.Id, ct).ConfigureAwait(false);
                await destination.Metadata.SaveMetadataAsync(saved.Id, current with { OwnerId = row.OwnerId }, ct).ConfigureAwait(false);
            }

            await VerifyDestinationAsync(destination, row, preserveBytes: false, ct).ConfigureAwait(false);
            return true;
        }
        finally {
            if (restoreCurrent)
                await destination.Keys.SetCurrentVersionAsync(targetKeyId, previousCurrent!, ct).ConfigureAwait(false);
        }
    }

    private static async Task<bool> DestinationAlreadyMatchesAsync(FileStorageTransferEndpoint destination, FileStoreResult row, bool preserveBytes, CancellationToken ct)
    {
        var algorithm = row.HashAlgorithm ?? HashAlgorithm.Sha256;
        var plainMatched = false;
        Stream? plain = null;
        try {
            plain = await destination.Storage.GetFileStreamAsync(row.Id, ct: ct).ConfigureAwait(false);
            if (plain is null)
                return false;

            var plainHash = await HashContentsAsync(plain, algorithm, ct).ConfigureAwait(false);
            if (!HashingService.Shared.FixedTimeEquals(plainHash, row.OriginalFileHash))
                return false;

            plainMatched = true;
        }
        catch (FileNotFoundException) {
            return false;
        }
        catch (FileNotAvailableException) when (preserveBytes) {
        }
        finally {
            if (plain is not null)
                await DisposeStreamAsync(plain).ConfigureAwait(false);
        }

        if (!preserveBytes) {
            if (!plainMatched)
                return false;

            var saved = await destination.Metadata.GetMetadataAsync(row.Id, ct).ConfigureAwait(false);
            return saved.IsCompressed == row.IsCompressed && saved.IsEncrypted == row.IsEncrypted;
        }

        var physical = (IFileStoragePhysical)destination.Storage;
        var key = ObjectKey(row);
        if (!await physical.Physical.FileExistsAsync(key, ct).ConfigureAwait(false))
            return false;

        var raw = await physical.Physical.OpenReadAsync(key, ct).ConfigureAwait(false);
        byte[] rawHash;
        try {
            rawHash = await HashContentsAsync(raw, algorithm, ct).ConfigureAwait(false);
        }
        finally {
            await DisposeStreamAsync(raw).ConfigureAwait(false);
        }

        return HashingService.Shared.FixedTimeEquals(rawHash, row.SourceFileHash);
    }

    private static async Task VerifyDestinationAsync(FileStorageTransferEndpoint destination, FileStoreResult row, bool preserveBytes, CancellationToken ct)
    {
        var algorithm = row.HashAlgorithm ?? HashAlgorithm.Sha256;
        var plainChecked = false;
        Stream? plain = null;
        try {
            plain = await destination.Storage.GetFileStreamAsync(row.Id, ct: ct).ConfigureAwait(false);
            if (plain is null)
                throw new InvalidOperationException($"Destination file {row.Id} did not return a stream.");

            var plainHash = await HashContentsAsync(plain, algorithm, ct).ConfigureAwait(false);
            if (!HashingService.Shared.FixedTimeEquals(plainHash, row.OriginalFileHash))
                throw new InvalidOperationException($"Destination plaintext hash for {row.Id} does not match the source OriginalFileHash.");

            plainChecked = true;
        }
        catch (FileNotAvailableException) when (preserveBytes) {
        }
        finally {
            if (plain is not null)
                await DisposeStreamAsync(plain).ConfigureAwait(false);
        }

        if (!preserveBytes) {
            if (!plainChecked)
                throw new InvalidOperationException($"Destination file {row.Id} is not readable, so its plaintext hash was not checked.");

            var saved = await destination.Metadata.GetMetadataAsync(row.Id, ct).ConfigureAwait(false);
            if (saved.IsCompressed != row.IsCompressed || saved.IsEncrypted != row.IsEncrypted)
                throw new InvalidOperationException($"Destination file {row.Id} compression or encryption flags do not match the source.");

            return;
        }

        var physical = (IFileStoragePhysical)destination.Storage;
        var raw = await physical.Physical.OpenReadAsync(ObjectKey(row), ct).ConfigureAwait(false);
        byte[] rawHash;
        try {
            rawHash = await HashContentsAsync(raw, algorithm, ct).ConfigureAwait(false);
        }
        finally {
            await DisposeStreamAsync(raw).ConfigureAwait(false);
        }

        if (!HashingService.Shared.FixedTimeEquals(rawHash, row.SourceFileHash))
            throw new InvalidOperationException($"Destination object hash for {row.Id} does not match the source SourceFileHash.");
    }

    private static async Task<byte[]> HashContentsAsync(Stream stream, HashAlgorithm algorithm, CancellationToken ct)
    {
        var hashing = new HashingStream(stream, algorithm.Create());
        try {
            await hashing.CopyToAsync(Stream.Null, ct: ct).ConfigureAwait(false);
            return hashing.GetHash();
        }
        finally {
            await DisposeStreamAsync(hashing).ConfigureAwait(false);
        }
    }

    private static async Task DisposeStreamAsync(Stream stream)
    {
#if NETSTANDARD2_0
        stream.Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
#else
        await stream.DisposeAsync().ConfigureAwait(false);
#endif
    }

    private static void NoteSkipped(RunState state, IProgress<FileStorageTransferProgress>? progress, FileStoreResult row)
    {
        state.FilesSkipped++;
        state.FilesCompleted++;
        state.CurrentFileBytesCopied = state.CurrentFileBytesTotal;
        Report(progress, state, FileStorageTransferPhase.Files, row.Id, FileStorageTransferItemOutcome.Skipped, state.CurrentFileBytesTotal, state.CurrentFileBytesTotal);
    }

    private static async Task<FileStoreResult?> DestinationMetadataAsync(IFileMetadataStore metadata, Guid fileId, CancellationToken ct)
    {
        try {
            return await metadata.GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        }
        catch (FileNotFoundException) {
            return null;
        }
    }

    private static bool HashesEqual(byte[]? left, byte[]? right) => left is not null && right is not null && HashingService.Shared.FixedTimeEquals(left, right);

    private static string ObjectKey(FileStoreResult row) => CloudObjectKeyBuilder.FromMetadata(row.Id, row.SourceFileName, row.PathPrefix);

    private static long ObjectSize(FileStoreResult row) => row.IsEncrypted && row.EncryptedFileSize is > 0 ? row.EncryptedFileSize.Value : row.SourceFileSize;

    private static long ProgressBytes(FileStorageTransferRequest request, FileStoreResult row)
        => request.KeyMode == FileStorageKeyMode.PreserveKeys ? ObjectSize(row) : row.OriginalFileSize;

    private static void Report(
        IProgress<FileStorageTransferProgress>? progress,
        RunState state,
        FileStorageTransferPhase phase,
        Guid? fileId,
        FileStorageTransferItemOutcome? outcome,
        long currentBytes,
        long currentTotal)
    {
        if (progress is null)
            return;

        var bytesCopied = outcome == FileStorageTransferItemOutcome.Started ? state.BytesCopied + currentBytes : state.BytesCopied;
        try {
            progress.Report(
                new(
                    phase,
                    state.FilesCompleted,
                    state.FilesTotal,
                    state.FilesCopied,
                    state.FilesSkipped,
                    state.FilesFailed,
                    bytesCopied,
                    state.BytesTotal,
                    fileId ?? state.CurrentFileId,
                    currentBytes,
                    currentTotal,
                    state.KeysCompleted,
                    state.KeysTotal,
                    outcome,
                    state.FilesRemoved));
        }
        catch {
            state.ProgressFailed = true;
            throw;
        }
    }

    private sealed class RunState(int progressByteInterval)
    {
        public int ProgressByteInterval { get; } = progressByteInterval;

        public int FilesCompleted { get; set; }

        public int FilesTotal { get; set; }

        public int FilesCopied { get; set; }

        public int FilesSkipped { get; set; }

        public int FilesFailed { get; set; }

        public int FilesRemoved { get; set; }

        public long BytesCopied { get; set; }

        public long BytesTotal { get; set; }

        public Guid? CurrentFileId { get; set; }

        public long CurrentFileBytesCopied { get; set; }

        public long CurrentFileBytesTotal { get; set; }

        public int KeysCompleted { get; set; }

        public int KeysTotal { get; set; }

        public bool ProgressFailed { get; set; }
    }
}
