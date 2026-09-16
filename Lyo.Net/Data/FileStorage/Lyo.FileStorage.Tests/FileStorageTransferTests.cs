using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;
using Lyo.Testing;
using KeyNotFoundException = Lyo.KeyStore.Exceptions.KeyNotFoundException;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageTransferTests
{
    [Fact]
    public async Task KeysOnly_CopiesReferencedVersions_DoesNotCopyBlobsOrUnreferencedKeys()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 11);
        await AddKeyAsync(pair.SourceKeys, "app", "2", 12);
        await pair.SourceKeys.SetCurrentVersionAsync("app", "2", TestCt);
        await AddKeyAsync(pair.SourceKeys, "unused", "1", 13);
        var payload = TestData.Create(64, TestData.Seed);
        var saved = await SaveAsync(pair.Source, payload, "notes.bin", encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.KeysOnly, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(0, result.FilesCopied);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(2, result.KeysCopied);
        Assert.Equal(await pair.SourceKeys.GetKeyAsync("app", "1", TestCt), await pair.DestinationKeys.GetKeyAsync("app", "1", TestCt));
        Assert.Equal(await pair.SourceKeys.GetKeyAsync("app", "2", TestCt), await pair.DestinationKeys.GetKeyAsync("app", "2", TestCt));
        Assert.Equal("2", await pair.DestinationKeys.GetCurrentVersionAsync("app", TestCt));
        Assert.False(await pair.DestinationKeys.HasKeyAsync("unused", ct: TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
        Assert.Equal(payload, await pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task KeysOnly_MissingReferencedKey_ThrowsAndWritesNoDestinationKeys()
    {
        using var pair = CreatePair();
        var encrypting = new LocalKeyStore();
        await AddKeyAsync(encrypting, "app", "1", 21);
        using var source = CreateScope(encrypting);
        var payload = TestData.Create(48, TestData.Seed + 1);
        var saved = await SaveAsync(source, payload, "secret.bin", encrypt: true, keyId: "app");
        var sourceEndpoint = new FileStorageTransferEndpoint(source.Storage, source.Storage.MetadataStore, pair.SourceKeys);

        var error = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => FileStorageTransfer.TransferAsync(sourceEndpoint, pair.DestinationEndpoint, new(FileStorageTransferScope.KeysOnly, FileStorageKeyMode.PreserveKeys), TestCt));

        Assert.Contains("app", error.Message, StringComparison.Ordinal);
        Assert.Empty(await pair.DestinationKeys.GetAvailableKeyIdsAsync(TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task KeysOnly_WithNewKeyMode_ThrowsBeforeWrites()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 31);
        var saved = await SaveAsync(pair.Source, TestData.Create(32, TestData.Seed + 2), "a.bin", encrypt: true, keyId: "app");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransferAsync(pair, new(FileStorageTransferScope.KeysOnly, FileStorageKeyMode.NewKey, TargetKeyId: "fresh")));

        Assert.False(await pair.DestinationKeys.HasKeyAsync("fresh", ct: TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Everything_PreserveKeys_DestinationObjectBytesMatchSource()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 41);
        var payload = TestData.Create(96, TestData.Seed + 3);
        var saved = await SaveAsync(pair.Source, payload, "blob.bin", encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(await File.ReadAllBytesAsync(ObjectPath(pair.Source, saved), TestCt), await File.ReadAllBytesAsync(ObjectPath(pair.Destination, saved), TestCt));
    }

    [Fact]
    public async Task Everything_PreserveKeys_ReadBackPlaintextMatches()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 51);
        var payload = TestData.Create(80, TestData.Seed + 4);
        var saved = await SaveAsync(pair.Source, payload, "plain.bin", encrypt: true, keyId: "app");

        await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task Everything_NewKey_ReadBackPlaintextMatches_CiphertextDiffers()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 61);
        var payload = TestData.Create(80, TestData.Seed + 5);
        var saved = await SaveAsync(pair.Source, payload, "sealed.bin", encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.NewKey, TargetKeyId: "app-2026"));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        var sourceObject = await File.ReadAllBytesAsync(ObjectPath(pair.Source, saved), TestCt);
        var destinationRow = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt);
        var destinationObject = await File.ReadAllBytesAsync(ObjectPath(pair.Destination, destinationRow), TestCt);
        Assert.NotEqual(sourceObject, destinationObject);
    }

    [Fact]
    public async Task Everything_PreserveKeys_RoundTripsPlaintextCompressedAndEncrypted_SameIds()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 71);
        var ownerId = LyoGuid.CreateV7();
        using var caller = JsonDocument.Parse("{\"kind\":\"note\"}");
        var callerJson = caller.RootElement.Clone();
        var plain = TestData.Create(40, TestData.Seed + 6);
        var compressed = TestData.Create(40, TestData.Seed + 7);
        var encrypted = TestData.Create(40, TestData.Seed + 8);
        var plainRow = await SaveAsync(
            pair.Source, plain, "note.txt", pathPrefix: "incoming", contentType: "text/plain", charset: "utf-8", metadata: callerJson, ownerId: ownerId,
            availability: FileAvailability.PendingScan);
        var compressedRow = await SaveAsync(pair.Source, compressed, "note.gz", compress: true, pathPrefix: "incoming");
        var encryptedRow = await SaveAsync(pair.Source, encrypted, "note.enc", encrypt: true, keyId: "app", pathPrefix: "incoming", ownerId: ownerId);
        await AddKeyAsync(pair.SourceKeys, "app", "2", 72);
        await pair.SourceKeys.SetCurrentVersionAsync("app", "2", TestCt);

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(3, result.FilesCopied);
        Assert.Equal(0, result.FilesFailed);
        var destPlain = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(plainRow.Id, TestCt);
        Assert.Equal(plainRow.Id, destPlain.Id);
        Assert.Equal("incoming", destPlain.PathPrefix);
        Assert.Equal("text/plain", destPlain.ContentType);
        Assert.Equal("utf-8", destPlain.Charset);
        Assert.Equal(callerJson.GetRawText(), destPlain.Metadata?.GetRawText());
        Assert.Equal(ownerId, destPlain.OwnerId);
        Assert.Equal(FileAvailability.PendingScan, destPlain.Availability);
        Assert.Equal(plainRow.Timestamp, destPlain.Timestamp);
        Assert.Equal(compressed, await pair.Destination.Storage.GetFileAsync(compressedRow.Id, ct: TestCt));
        Assert.Equal(encrypted, await pair.Destination.Storage.GetFileAsync(encryptedRow.Id, ct: TestCt));
        Assert.Equal("2", await pair.DestinationKeys.GetCurrentVersionAsync("app", TestCt));
        Assert.Equal("1", (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(encryptedRow.Id, TestCt)).DataEncryptionKeyVersion);
        Assert.Equal(encrypted, await pair.Source.Storage.GetFileAsync(encryptedRow.Id, ct: TestCt));
    }

    [Fact]
    public async Task Everything_PreserveKeys_SharedMetadata_CopiesBytesAndDoesNotSkipOnExistingRow()
    {
        var metaRoot = NewDirectory();
        var shared = new LocalFileMetadataStore(metaRoot);
        using var pair = CreatePair(sharedMetadata: shared, extraDirectories: [metaRoot]);
        var payload = TestData.Create(70, TestData.Seed + 9);
        var saved = await SaveAsync(pair.Source, payload, "shared.bin");
        var before = (await shared.GetMetadataAsync(saved.Id, TestCt)).Timestamp;

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(0, result.FilesSkipped);
        Assert.Equal(before, (await shared.GetMetadataAsync(saved.Id, TestCt)).Timestamp);
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.True(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Everything_PreserveKeys_RelativeKey_DoesNotIncludeStoragePrefix()
    {
        using var pair = CreatePair();
        var saved = await SaveAsync(pair.Source, TestData.Create(24, TestData.Seed + 10), "rel.bin", pathPrefix: "incoming");

        await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        var key = CloudObjectKeyBuilder.FromMetadata(saved.Id, saved.SourceFileName, saved.PathPrefix);
        Assert.StartsWith("incoming/", key, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(pair.Destination.Options.RootDirectoryPath, key.Replace('/', Path.DirectorySeparatorChar))));
        Assert.False(File.Exists(Path.Combine(pair.Destination.Options.RootDirectoryPath, "files", key.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task Everything_NewKey_ReencryptsEncryptedFiles_LeavesPlaintextUnencrypted()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 81);
        var ownerId = LyoGuid.CreateV7();
        var plain = TestData.Create(36, TestData.Seed + 11);
        var sealedBytes = TestData.Create(36, TestData.Seed + 12);
        var plainRow = await SaveAsync(pair.Source, plain, "open.bin");
        var sealedRow = await SaveAsync(pair.Source, sealedBytes, "closed.bin", encrypt: true, keyId: "app", ownerId: ownerId);
        var sourceBefore = await pair.Source.Storage.MetadataStore.GetMetadataAsync(sealedRow.Id, TestCt);

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.NewKey, TargetKeyId: "app-2026"));

        Assert.Equal(2, result.FilesCopied);
        var destPlain = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(plainRow.Id, TestCt);
        var destSealed = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(sealedRow.Id, TestCt);
        Assert.False(destPlain.IsEncrypted);
        Assert.Equal(plain, await pair.Destination.Storage.GetFileAsync(plainRow.Id, ct: TestCt));
        Assert.True(destSealed.IsEncrypted);
        Assert.Equal("app-2026", destSealed.DataEncryptionKeyId);
        Assert.Equal("1", destSealed.DataEncryptionKeyVersion);
        Assert.Equal(ownerId, destSealed.OwnerId);
        Assert.Equal(sealedBytes, await pair.Destination.Storage.GetFileAsync(sealedRow.Id, ct: TestCt));
        var sourceAfter = await pair.Source.Storage.MetadataStore.GetMetadataAsync(sealedRow.Id, TestCt);
        Assert.Equal(sourceBefore.DataEncryptionKeyId, sourceAfter.DataEncryptionKeyId);
        Assert.Equal(sourceBefore.DataEncryptionKeyVersion, sourceAfter.DataEncryptionKeyVersion);
    }

    [Fact]
    public async Task Everything_NewKey_WhenKeyIdExists_ThrowsAndWritesNoFiles()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 91);
        await AddKeyAsync(pair.DestinationKeys, "app-2026", "1", 92);
        var saved = await SaveAsync(pair.Source, TestData.Create(28, TestData.Seed + 13), "a.bin", encrypt: true, keyId: "app");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.NewKey, TargetKeyId: "app-2026")));

        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Everything_NewKey_SameMetadataInstance_ThrowsBeforeWrites()
    {
        var metaRoot = NewDirectory();
        var shared = new LocalFileMetadataStore(metaRoot);
        using var pair = CreatePair(sharedMetadata: shared, extraDirectories: [metaRoot]);
        await AddKeyAsync(pair.SourceKeys, "app", "1", 101);
        var saved = await SaveAsync(pair.Source, TestData.Create(28, TestData.Seed + 14), "a.bin", encrypt: true, keyId: "app");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.NewKey, TargetKeyId: "app-2026")));

        Assert.False(await pair.DestinationKeys.HasKeyAsync("app-2026", ct: TestCt));
        Assert.Equal("app", (await shared.GetMetadataAsync(saved.Id, TestCt)).DataEncryptionKeyId);
    }

    [Fact]
    public async Task Everything_ExistingKey_UsesThatVersion()
    {
        using var missingKey = CreatePair();
        await AddKeyAsync(missingKey.SourceKeys, "app", "1", 111);
        var missingKeyFile = await SaveAsync(missingKey.Source, TestData.Create(24, TestData.Seed + 15), "a.bin", encrypt: true, keyId: "app");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TransferAsync(missingKey, new(FileStorageTransferScope.Everything, FileStorageKeyMode.ExistingKey, TargetKeyId: "app", TargetKeyVersion: "1")));
        Assert.False(File.Exists(ObjectPath(missingKey.Destination, missingKeyFile)));

        using var missingVersion = CreatePair();
        await AddKeyAsync(missingVersion.SourceKeys, "app", "1", 112);
        await AddKeyAsync(missingVersion.DestinationKeys, "app", "1", 113);
        var missingVersionFile = await SaveAsync(missingVersion.Source, TestData.Create(24, TestData.Seed + 16), "b.bin", encrypt: true, keyId: "app");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransferAsync(missingVersion, new(FileStorageTransferScope.Everything, FileStorageKeyMode.ExistingKey, TargetKeyId: "app", TargetKeyVersion: "9")));
        Assert.False(File.Exists(ObjectPath(missingVersion.Destination, missingVersionFile)));

        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 114);
        await AddKeyAsync(pair.DestinationKeys, "app", "1", 115);
        await AddKeyAsync(pair.DestinationKeys, "app", "2", 116);
        await pair.DestinationKeys.SetCurrentVersionAsync("app", "1", TestCt);
        var payload = TestData.Create(44, TestData.Seed + 17);
        var saved = await SaveAsync(pair.Source, payload, "c.bin", encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.ExistingKey, TargetKeyId: "app", TargetKeyVersion: "2"));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(0, result.KeysCopied);
        var destination = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt);
        Assert.Equal("app", destination.DataEncryptionKeyId);
        Assert.Equal("2", destination.DataEncryptionKeyVersion);
        Assert.Equal("1", await pair.DestinationKeys.GetCurrentVersionAsync("app", TestCt));
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task Everything_SaveReturnsDifferentId_FailsThatFile()
    {
        using var pair = CreatePair(destinationDuplicates: true);
        await AddKeyAsync(pair.DestinationKeys, "app", "1", 121);
        var payload = TestData.Create(52, TestData.Seed + 18);
        await SaveAsync(pair.Destination, payload, "existing.bin");
        var source = await SaveAsync(pair.Source, payload, "incoming.bin");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.ExistingKey, TargetKeyId: "app"));

        Assert.Equal(0, result.FilesCopied);
        Assert.Equal(1, result.FilesFailed);
        Assert.Contains(result.Errors, error => error.FileId == source.Id);
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Destination.Storage.MetadataStore.GetMetadataAsync(source.Id, TestCt));
    }

    [Fact]
    public async Task Migrate_WhenListCountEqualsCap_ThrowsAndWritesNothing()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 131);
        var first = await SaveAsync(pair.Source, TestData.Create(16, TestData.Seed + 19), "a.bin", encrypt: true, keyId: "app");
        var second = await SaveAsync(pair.Source, TestData.Create(16, TestData.Seed + 20), "b.bin", encrypt: true, keyId: "app");

        await Assert.ThrowsAsync<InvalidOperationException>(() => TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, ListCap: 1)));

        Assert.Empty(await pair.DestinationKeys.GetAvailableKeyIdsAsync(TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, first)));
        Assert.False(File.Exists(ObjectPath(pair.Destination, second)));
    }

    [Fact]
    public async Task Migrate_DestinationIdWithSameHash_Skips()
    {
        using var pair = CreatePair();
        var saved = await SaveAsync(pair.Source, TestData.Create(20, TestData.Seed + 21), "a.bin");

        var first = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));
        var second = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(1, first.FilesCopied);
        Assert.Equal(0, second.FilesCopied);
        Assert.Equal(1, second.FilesSkipped);
        Assert.Equal(0, second.FilesFailed);
        Assert.Equal(saved.OriginalFileHash, (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt)).OriginalFileHash);
    }

    [Fact]
    public async Task Migrate_DestinationIdWithDifferentHash_FailsThatFile()
    {
        using var pair = CreatePair();
        var saved = await SaveAsync(pair.Source, TestData.Create(20, TestData.Seed + 22), "a.bin");
        var tweaked = saved with { OriginalFileHash = TestData.Create(saved.OriginalFileHash.Length, TestData.Seed + 23) };
        await pair.Destination.Storage.MetadataStore.SaveMetadataAsync(saved.Id, tweaked, TestCt);
        var before = (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt)).OriginalFileHash;

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(0, result.FilesCopied);
        Assert.Equal(1, result.FilesFailed);
        Assert.Equal(before, (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt)).OriginalFileHash);
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Migrate_SecondFileWriteThrows_ReportsFailure_SourceIntact()
    {
        var metaRoot = NewDirectory();
        var inner = new LocalFileMetadataStore(metaRoot);
        var gated = new SaveGateMetadataStore(inner, throwOnSave: 2);
        using var pair = CreatePair(destinationMetadata: gated, extraDirectories: [metaRoot]);
        var first = TestData.Create(18, TestData.Seed + 24);
        var second = TestData.Create(18, TestData.Seed + 25);
        var firstRow = await SaveAsync(pair.Source, first, "a.bin");
        var secondRow = await SaveAsync(pair.Source, second, "b.bin");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(1, result.FilesFailed);
        Assert.Single(result.Errors);
        Assert.Equal(first, await pair.Source.Storage.GetFileAsync(firstRow.Id, ct: TestCt));
        Assert.Equal(second, await pair.Source.Storage.GetFileAsync(secondRow.Id, ct: TestCt));
    }

    [Fact]
    public async Task Migrate_SameServiceInstance_Throws()
    {
        using var pair = CreatePair();
        var endpoint = pair.SourceEndpoint;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FileStorageTransfer.TransferAsync(endpoint, endpoint, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys), TestCt));
    }

    [Fact]
    public async Task Migrate_PendingDirectUploadAndTombstone_Skipped()
    {
        using var pair = CreatePair();
        var live = await SaveAsync(pair.Source, TestData.Create(16, TestData.Seed + 26), "live.bin");
        var pending = await SaveAsync(pair.Source, TestData.Create(16, TestData.Seed + 27), "pending.bin", availability: FileAvailability.PendingDirectUpload);
        var tombstone = await SaveAsync(pair.Source, TestData.Create(16, TestData.Seed + 28), "gone.bin");
        Assert.True(await pair.Source.Storage.DeleteFileAsync(tombstone.Id, ct: TestCt));

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(live.OriginalFileHash, (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(live.Id, TestCt)).OriginalFileHash);
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Destination.Storage.MetadataStore.GetMetadataAsync(pending.Id, TestCt));
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Destination.Storage.MetadataStore.GetMetadataAsync(tombstone.Id, TestCt));
    }

    [Fact]
    public async Task Migrate_Cancelled_ThrowsAndIsNotSuccess()
    {
        using var pair = CreatePair();
        var saved = await SaveAsync(pair.Source, TestData.Create(16, TestData.Seed + 29), "a.bin");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => FileStorageTransfer.TransferAsync(
                pair.SourceEndpoint, pair.DestinationEndpoint, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys), cts.Token));

        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt));
    }

    [Fact]
    public async Task Migrate_Progress_ReportsListingTotalThenEachFileFinish()
    {
        using var pair = CreatePair();
        await SaveAsync(pair.Source, TestData.Create(12, TestData.Seed + 30), "a.bin");
        await SaveAsync(pair.Source, TestData.Create(12, TestData.Seed + 31), "b.bin");
        var request = new FileStorageTransferRequest(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys);
        var first = await TransferAsync(pair, request);
        Assert.Equal(2, first.FilesCopied);

        var progress = new RecordingProgress();
        var second = await FileStorageTransfer.TransferAsync(pair.SourceEndpoint, pair.DestinationEndpoint, request, progress, TestCt);
        var snapshots = progress.Snapshots;
        var firstFinish = snapshots.FindIndex(snapshot =>
            snapshot.ItemOutcome is FileStorageTransferItemOutcome.Copied or FileStorageTransferItemOutcome.Skipped or FileStorageTransferItemOutcome.Failed);

        Assert.True(firstFinish > 0);
        Assert.Contains(snapshots.Take(firstFinish), snapshot => snapshot.FilesTotal == 2);
        Assert.Contains(snapshots, snapshot => snapshot.ItemOutcome == FileStorageTransferItemOutcome.Skipped);
        var last = snapshots[^1];
        Assert.Equal(second.FilesCopied, last.FilesCopied);
        Assert.Equal(second.FilesSkipped, last.FilesSkipped);
        Assert.Equal(second.FilesFailed, last.FilesFailed);
        Assert.Equal(2, second.FilesSkipped);
        Assert.Equal(0, second.FilesCopied);
    }

    [Fact]
    public async Task Migrate_Progress_ByteInterval_ReportsMidFile()
    {
        using var pair = CreatePair();
        var payload = TestData.Create(200 * 1024, TestData.Seed + 32);
        await SaveAsync(pair.Source, payload, "wide.bin");
        var progress = new RecordingProgress();
        var request = new FileStorageTransferRequest(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, ProgressByteInterval: 1);

        var result = await FileStorageTransfer.TransferAsync(pair.SourceEndpoint, pair.DestinationEndpoint, request, progress, TestCt);

        Assert.Equal(1, result.FilesCopied);
        var snapshots = progress.Snapshots;
        var mid = snapshots.FindIndex(snapshot => snapshot.CurrentFileBytesCopied > 0 && snapshot.CurrentFileBytesCopied < snapshot.CurrentFileBytesTotal);
        var finish = snapshots.FindIndex(snapshot => snapshot.ItemOutcome == FileStorageTransferItemOutcome.Copied);
        Assert.True(mid >= 0);
        Assert.True(finish > mid);
    }

    [Fact]
    public async Task Transfer_FileIds_CopiesOnlyThoseFiles()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 201);
        await AddKeyAsync(pair.SourceKeys, "other", "1", 202);
        var keepPayload = TestData.Create(48, TestData.Seed + 40);
        var dropPayload = TestData.Create(48, TestData.Seed + 41);
        var keep = await SaveAsync(pair.Source, keepPayload, "keep.bin", encrypt: true, keyId: "app");
        var drop = await SaveAsync(pair.Source, dropPayload, "drop.bin", encrypt: true, keyId: "other");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, FileIds: [keep.Id]));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(keepPayload, await pair.Destination.Storage.GetFileAsync(keep.Id, ct: TestCt));
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Destination.Storage.MetadataStore.GetMetadataAsync(drop.Id, TestCt));
        Assert.True(await pair.DestinationKeys.HasKeyAsync("app", ct: TestCt));
        Assert.False(await pair.DestinationKeys.HasKeyAsync("other", ct: TestCt));
        Assert.Equal(dropPayload, await pair.Source.Storage.GetFileAsync(drop.Id, ct: TestCt));
    }

    [Fact]
    public async Task Transfer_FileIds_MissingId_ThrowsBeforeWrites()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 211);
        var saved = await SaveAsync(pair.Source, TestData.Create(24, TestData.Seed + 42), "a.bin", encrypt: true, keyId: "app");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, FileIds: [LyoGuid.CreateV7()])));

        Assert.Empty(await pair.DestinationKeys.GetAvailableKeyIdsAsync(TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Transfer_Move_AfterPlaintextHashMatch_DeletesSource_KeyRemains()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 221);
        var payload = TestData.Create(64, TestData.Seed + 43);
        var saved = await SaveAsync(pair.Source, payload, "move.bin", encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(1, result.FilesRemoved);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.Equal(saved.OriginalFileHash, (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt)).OriginalFileHash);
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.True(await pair.SourceKeys.HasKeyAsync("app", ct: TestCt));
        Assert.Equal(await pair.SourceKeys.GetKeyAsync("app", "1", TestCt), await pair.DestinationKeys.GetKeyAsync("app", "1", TestCt));
    }

    [Fact]
    public async Task Transfer_Move_PreserveKeys_CompressedAndEncrypted_StoredHashMatches()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 231);
        var payload = TestData.Create(72, TestData.Seed + 44);
        var saved = await SaveAsync(pair.Source, payload, "both.bin", compress: true, encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move));

        Assert.Equal(1, result.FilesRemoved);
        var destination = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt);
        Assert.True(destination.IsCompressed);
        Assert.True(destination.IsEncrypted);
        Assert.Equal(saved.SourceFileHash, destination.SourceFileHash);
        Assert.Equal(saved.SourceFileHash, System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(ObjectPath(pair.Destination, destination), TestCt)));
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task Transfer_Move_NewKey_Compressed_PlaintextHashMatches_StoredBytesDiffer_FlagsStay()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 241);
        var payload = TestData.Create(72, TestData.Seed + 45);
        var saved = await SaveAsync(pair.Source, payload, "sealed.gz", compress: true, encrypt: true, keyId: "app");
        var sourceObject = await File.ReadAllBytesAsync(ObjectPath(pair.Source, saved), TestCt);

        var result = await TransferAsync(
            pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.NewKey, TargetKeyId: "app-2026", Disposition: FileStorageTransferDisposition.Move));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(1, result.FilesRemoved);
        var destination = await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt);
        Assert.True(destination.IsCompressed);
        Assert.True(destination.IsEncrypted);
        Assert.Equal("app-2026", destination.DataEncryptionKeyId);
        Assert.Equal(saved.OriginalFileHash, destination.OriginalFileHash);
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.NotEqual(sourceObject, await File.ReadAllBytesAsync(ObjectPath(pair.Destination, destination), TestCt));
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task Transfer_Move_DestinationAlreadyMatches_StillRemovesSource()
    {
        using var pair = CreatePair();
        var payload = TestData.Create(32, TestData.Seed + 46);
        var saved = await SaveAsync(pair.Source, payload, "again.bin");
        var copied = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        var moved = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move));

        Assert.Equal(1, copied.FilesCopied);
        Assert.Equal(0, moved.FilesCopied);
        Assert.Equal(1, moved.FilesSkipped);
        Assert.Equal(1, moved.FilesRemoved);
        Assert.Equal(0, moved.FilesFailed);
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        await Assert.ThrowsAsync<FileNotFoundException>(() => pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task Transfer_Move_HashMismatch_LeavesSource()
    {
        using var pair = CreatePair();
        var payload = TestData.Create(28, TestData.Seed + 47);
        var saved = await SaveAsync(pair.Source, payload, "clash.bin");
        var tweaked = saved with { OriginalFileHash = TestData.Create(saved.OriginalFileHash.Length, TestData.Seed + 48) };
        await pair.Destination.Storage.MetadataStore.SaveMetadataAsync(saved.Id, tweaked, TestCt);

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move));

        Assert.Equal(0, result.FilesCopied);
        Assert.Equal(0, result.FilesRemoved);
        Assert.Equal(1, result.FilesFailed);
        Assert.Equal(payload, await pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.Equal(tweaked.OriginalFileHash, (await pair.Destination.Storage.MetadataStore.GetMetadataAsync(saved.Id, TestCt)).OriginalFileHash);
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Transfer_Move_SecondFileFails_RemovesOnlyTheCopiedFile()
    {
        var metaRoot = NewDirectory();
        var inner = new LocalFileMetadataStore(metaRoot);
        var gated = new SaveGateMetadataStore(inner, throwOnSave: 2);
        using var pair = CreatePair(destinationMetadata: gated, extraDirectories: [metaRoot]);
        var first = TestData.Create(18, TestData.Seed + 49);
        var second = TestData.Create(18, TestData.Seed + 50);
        var firstRow = await SaveAsync(pair.Source, first, "a.bin");
        var secondRow = await SaveAsync(pair.Source, second, "b.bin");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(1, result.FilesFailed);
        Assert.Equal(1, result.FilesRemoved);
        var firstGone = await SourceMissingAsync(pair, firstRow.Id);
        var secondGone = await SourceMissingAsync(pair, secondRow.Id);
        Assert.NotEqual(firstGone, secondGone);
        var keptId = firstGone ? secondRow.Id : firstRow.Id;
        var keptPayload = firstGone ? second : first;
        Assert.Equal(keptPayload, await pair.Source.Storage.GetFileAsync(keptId, ct: TestCt));
    }

    [Fact]
    public async Task Transfer_Move_SharedCatalog_ThrowsBeforeWrites()
    {
        var metaRoot = NewDirectory();
        var shared = new LocalFileMetadataStore(metaRoot);
        using var pair = CreatePair(sharedMetadata: shared, extraDirectories: [metaRoot]);
        var payload = TestData.Create(24, TestData.Seed + 51);
        var saved = await SaveAsync(pair.Source, payload, "shared.bin");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move)));

        Assert.Equal(payload, await pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Transfer_Move_KeysOnly_ThrowsBeforeWrites()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 261);
        var payload = TestData.Create(24, TestData.Seed + 52);
        var saved = await SaveAsync(pair.Source, payload, "keys.bin", encrypt: true, keyId: "app");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransferAsync(pair, new(FileStorageTransferScope.KeysOnly, FileStorageKeyMode.PreserveKeys, Disposition: FileStorageTransferDisposition.Move)));

        Assert.Empty(await pair.DestinationKeys.GetAvailableKeyIdsAsync(TestCt));
        Assert.Equal(payload, await pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.False(File.Exists(ObjectPath(pair.Destination, saved)));
    }

    [Fact]
    public async Task Transfer_Copy_EncryptedAndCompressed_LeavesSourceReadable()
    {
        using var pair = CreatePair();
        await AddKeyAsync(pair.SourceKeys, "app", "1", 271);
        var payload = TestData.Create(56, TestData.Seed + 53);
        var saved = await SaveAsync(pair.Source, payload, "stay.bin", compress: true, encrypt: true, keyId: "app");

        var result = await TransferAsync(pair, new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys));

        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(0, result.FilesRemoved);
        Assert.Equal(payload, await pair.Source.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.Equal(payload, await pair.Destination.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.True(File.Exists(ObjectPath(pair.Source, saved)));
    }

    [Fact]
    public async Task GetContentIntegrity_ReturnsPlaintextHashAndSize()
    {
        using var pair = CreatePair();
        var payload = TestData.Create(40, TestData.Seed + 54);
        var saved = await SaveAsync(pair.Source, payload, "hash.bin");

        var integrity = await pair.Source.Storage.GetContentIntegrityAsync(saved.Id, TestCt);

        Assert.Equal(saved.OriginalFileHash, integrity.OriginalFileHash);
        Assert.Equal(saved.OriginalFileSize, integrity.OriginalFileSize);
        Assert.Equal(saved.HashAlgorithm ?? HashAlgorithm.Sha256, integrity.HashAlgorithm);
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(payload), integrity.OriginalFileHash);
    }

    private static async Task<bool> SourceMissingAsync(TransferPair pair, Guid fileId)
    {
        try {
            await pair.Source.Storage.GetFileAsync(fileId, ct: TestCt);
            return false;
        }
        catch (FileNotFoundException) {
            return true;
        }
    }

    private static async Task<FileStorageTransferResult> TransferAsync(TransferPair pair, FileStorageTransferRequest request) =>
        await FileStorageTransfer.TransferAsync(pair.SourceEndpoint, pair.DestinationEndpoint, request, TestCt);

    private static async Task AddKeyAsync(LocalKeyStore store, string keyId, string version, int seed)
    {
        await store.AddKeyAsync(keyId, version, TestData.Create(32, seed), TestCt);
        await store.SetCurrentVersionAsync(keyId, version, TestCt);
    }

    private static async Task<FileStoreResult> SaveAsync(
        LocalFileStorageTestScope scope,
        byte[] payload,
        string name,
        bool compress = false,
        bool encrypt = false,
        string? keyId = null,
        string? pathPrefix = null,
        string? contentType = null,
        string? charset = null,
        JsonElement? metadata = null,
        Guid? ownerId = null,
        FileAvailability? availability = null)
    {
        var saved = await scope.Storage.SaveFileAsync(
            payload, name, compress, encrypt, keyId, pathPrefix, contentType: contentType, charset: charset, metadata: metadata, ct: TestCt);
        if (ownerId is null && availability is null)
            return saved;

        var patched = saved with { OwnerId = ownerId ?? saved.OwnerId, Availability = availability ?? saved.Availability };
        await scope.Storage.MetadataStore.SaveMetadataAsync(saved.Id, patched, TestCt);
        return patched;
    }

    private static string ObjectPath(LocalFileStorageTestScope scope, FileStoreResult row)
    {
        var key = CloudObjectKeyBuilder.FromMetadata(row.Id, row.SourceFileName, row.PathPrefix);
        return Path.Combine(scope.Options.RootDirectoryPath, key.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string NewDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-mig-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static TransferPair CreatePair(
        IFileMetadataStore? sharedMetadata = null,
        IFileMetadataStore? destinationMetadata = null,
        bool destinationDuplicates = false,
        string[]? extraDirectories = null)
    {
        var sourceKeys = new LocalKeyStore();
        var destinationKeys = new LocalKeyStore();
        var source = CreateScope(sourceKeys, sharedMetadata);
        var destination = CreateScope(destinationKeys, destinationMetadata ?? sharedMetadata, destinationDuplicates);
        return new(source, destination, sourceKeys, destinationKeys, extraDirectories ?? []);
    }

    private static LocalFileStorageTestScope CreateScope(LocalKeyStore keys, IFileMetadataStore? metadata = null, bool duplicates = false)
    {
        var encryption = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(new AesGcmEncryptionService(keys), keys);
        var compression = new CompressionService([new GZipCompressorFactory(), new DeflateCompressorFactory(), new BrotliCompressorFactory(), new ZLibCompressorFactory()]);
        return LocalFileStorageTestScope.Create(
            builder: duplicates ? options =>
            {
                options.EnableDuplicateDetection = true;
                return options;
            } : null, metadataService: metadata, compressionService: compression, twoKeyEncryptionService: encryption);
    }

    private sealed class TransferPair(
        LocalFileStorageTestScope source,
        LocalFileStorageTestScope destination,
        LocalKeyStore sourceKeys,
        LocalKeyStore destinationKeys,
        string[] extraDirectories) : IDisposable
    {
        public LocalFileStorageTestScope Source { get; } = source;

        public LocalFileStorageTestScope Destination { get; } = destination;

        public LocalKeyStore SourceKeys { get; } = sourceKeys;

        public LocalKeyStore DestinationKeys { get; } = destinationKeys;

        public FileStorageTransferEndpoint SourceEndpoint => new(Source.Storage, Source.Storage.MetadataStore, SourceKeys);

        public FileStorageTransferEndpoint DestinationEndpoint => new(Destination.Storage, Destination.Storage.MetadataStore, DestinationKeys);

        public void Dispose()
        {
            Source.Dispose();
            Destination.Dispose();
            foreach (var extra in extraDirectories) {
                try {
                    if (Directory.Exists(extra))
                        Directory.Delete(extra, true);
                }
                catch (IOException) {
                }
            }
        }
    }

    private sealed class RecordingProgress : IProgress<FileStorageTransferProgress>
    {
        public List<FileStorageTransferProgress> Snapshots { get; } = [];

        public void Report(FileStorageTransferProgress value) => Snapshots.Add(value);
    }

    private sealed class SaveGateMetadataStore(IFileMetadataStore inner, int throwOnSave) : IFileMetadataStore
    {
        private int _saves;

        public Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default) => inner.GetMetadataAsync(fileId, ct);

        public Task SaveMetadataAsync(Guid fileId, FileStoreResult metadata, CancellationToken ct = default)
        {
            _saves++;
            if (_saves == throwOnSave)
                throw new IOException("destination metadata save failed");

            return inner.SaveMetadataAsync(fileId, metadata, ct);
        }

        public Task<bool> DeleteMetadataAsync(Guid fileId, CancellationToken ct = default) => inner.DeleteMetadataAsync(fileId, ct);

        public Task<bool> PurgeMetadataAsync(Guid fileId, CancellationToken ct = default) => inner.PurgeMetadataAsync(fileId, ct);

        public Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default) => inner.FindByHashAsync(hash, ct);

        public Task<IReadOnlyList<FileStoreResult>> ListByPathPrefixAsync(string? pathPrefix, bool includeDescendants, int maxKeys, CancellationToken ct = default)
            => inner.ListByPathPrefixAsync(pathPrefix, includeDescendants, maxKeys, ct);

        public Task<IEnumerable<FileStoreResult>> FindByKeyIdAndVersionAsync(string keyId, string? keyVersion = null, CancellationToken ct = default)
            => inner.FindByKeyIdAndVersionAsync(keyId, keyVersion, ct);

        public Task<IReadOnlyList<FileStoreResult>> ListPendingDirectUploadsAsync(DateTime beganBefore, int maxKeys, CancellationToken ct = default)
            => inner.ListPendingDirectUploadsAsync(beganBefore, maxKeys, ct);

        public Task<IReadOnlyList<FileStoreResult>> SearchByNameAsync(string text, int maxKeys, CancellationToken ct = default) => inner.SearchByNameAsync(text, maxKeys, ct);

        public Task<FileRevision> SaveNewVersionAsync(Guid fileId, string storedKey, CancellationToken ct = default) => inner.SaveNewVersionAsync(fileId, storedKey, ct);

        public Task<FileRevision?> GetVersionAsync(Guid revisionId, CancellationToken ct = default) => inner.GetVersionAsync(revisionId, ct);
    }
}
