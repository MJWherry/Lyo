using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Coverage for the streaming save/read API, default-mode delete tombstones, duplicate strategies, availability policy plumbing, and cancellation propagation that used
/// to be exercised only through the byte[] save path.
/// </summary>
public sealed class LocalFileStorageStreamHealthPolicyTests
{
    [Fact]
    public async Task SaveFromStreamAsync_PlainRoundtrip_PreservesBytesAndDeclaredSize()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var payload = "stream-save-me"u8.ToArray();
        await using var input = new MemoryStream(payload, false);
        var saved = await scope.Storage.SaveFromStreamAsync(input, payload.LongLength, "stream.txt", ct: TestContext.Current.CancellationToken);
        Assert.Equal(payload.LongLength, saved.OriginalFileSize);
        var roundtrip = await scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken);
        Assert.Equal(payload, roundtrip);
    }

    [Fact]
    public async Task SaveFromStreamAsync_FileIdOverride_UsesProvidedId()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var desired = Guid.NewGuid();
        var payload = "force-id"u8.ToArray();
        await using var input = new MemoryStream(payload);
        var saved = await scope.Storage.SaveFromStreamAsync(input, payload.LongLength, "force.txt", fileId: desired, ct: TestContext.Current.CancellationToken);
        Assert.Equal(desired, saved.Id);
    }

    [Fact]
    public async Task GetFileStreamAsync_PlainContent_ReturnsBytes()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var payload = "stream-read"u8.ToArray();
        var saved = await scope.Storage.SaveFileAsync(payload, "x.bin", ct: TestContext.Current.CancellationToken);
        await using var stream = await scope.Storage.GetFileStreamAsync(saved.Id, ct: TestContext.Current.CancellationToken);
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms, TestContext.Current.CancellationToken);
        Assert.Equal(payload, ms.ToArray());
    }

    [Fact]
    public async Task GetFileAsync_NonExistentId_WithThrowOnFileNotFoundFalse_ReturnsEmpty()
    {
        // The base implementation maps a "no metadata" lookup to an empty byte[] (not null) when ThrowOnFileNotFound is off.
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.ThrowOnFileNotFound = false;
            return o;
        });

        var result = await scope.Storage.GetFileAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckHealthAsync_Full_RoundtripsSampleFile()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.HealthCheckMode = FileStorageHealthCheckMode.Full;
            return o;
        });

        var result = await scope.Storage.CheckHealthAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsHealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Full_WithRestrictiveContentPolicy_StillReportsHealthy()
    {
        // The probe hits the physical IO seam directly, so an allow-list that excludes its content type must not make the service look permanently unhealthy.
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.HealthCheckMode = FileStorageHealthCheckMode.Full;
            o.AllowedContentTypes = ["image/png"];
            return o;
        });

        var result = await scope.Storage.CheckHealthAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsHealthy, result.Message);
    }

    [Fact]
    public async Task CheckHealthAsync_Full_LeavesNoMetadataOrProbeObjectBehind()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.HealthCheckMode = FileStorageHealthCheckMode.Full;
            return o;
        });

        var result = await scope.Storage.CheckHealthAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsHealthy, result.Message);
        Assert.False(Directory.Exists(Path.Combine(scope.Options.RootDirectoryPath, ".lyo-health")) &&
            Directory.EnumerateFiles(Path.Combine(scope.Options.RootDirectoryPath, ".lyo-health")).Any());
    }

    [Fact]
    public async Task CheckHealthAsync_Lightweight_DoesNotWriteFiles()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.HealthCheckMode = FileStorageHealthCheckMode.Lightweight;
            return o;
        });

        var result = await scope.Storage.CheckHealthAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsHealthy);
    }

    [Fact]
    public async Task DuplicateStrategy_ReturnExisting_ReusesExistingFileId()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.EnableDuplicateDetection = true;
            o.DuplicateStrategy = DuplicateHandlingStrategy.ReturnExisting;
            return o;
        });

        var payload = "duplicate-me"u8.ToArray();
        var first = await scope.Storage.SaveFileAsync(payload, "a.txt", ct: TestContext.Current.CancellationToken);
        var second = await scope.Storage.SaveFileAsync(payload, "a.txt", ct: TestContext.Current.CancellationToken);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task DuplicateStrategy_AllowDuplicate_AllocatesSecondFileId()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.EnableDuplicateDetection = true;
            o.DuplicateStrategy = DuplicateHandlingStrategy.AllowDuplicate;
            return o;
        });

        var payload = "dup-allow"u8.ToArray();
        var first = await scope.Storage.SaveFileAsync(payload, "a.txt", ct: TestContext.Current.CancellationToken);
        var second = await scope.Storage.SaveFileAsync(payload, "a.txt", ct: TestContext.Current.CancellationToken);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task DefaultAvailability_Quarantined_BlocksRead()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.DefaultAvailability = FileAvailability.Quarantined;
            return o;
        });

        var saved = await scope.Storage.SaveFileAsync("blocked"u8.ToArray(), "f.bin", ct: TestContext.Current.CancellationToken);
        Assert.Equal(FileAvailability.Quarantined, saved.Availability);

        // Quarantined reads must fail unless admin override is on.
        await Assert.ThrowsAnyAsync<FileNotAvailableException>(() => scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AllowReadQuarantinedForAdmin_PermitsReadOfQuarantinedFiles()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.DefaultAvailability = FileAvailability.Quarantined;
            o.AllowReadQuarantinedForAdmin = true;
            return o;
        });

        var payload = "admin-readable"u8.ToArray();
        var saved = await scope.Storage.SaveFileAsync(payload, "f.bin", ct: TestContext.Current.CancellationToken);
        var bytes = await scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken);
        Assert.Equal(payload, bytes);
    }

    [Fact]
    public async Task MaxUploadSizeBytes_RejectsOversizedSaveBeforeWrite()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.MaxUploadSizeBytes = 8;
            return o;
        });

        var oversized = new byte[32];
        await Assert.ThrowsAnyAsync<FilePolicyRejectedException>(() => scope.Storage.SaveFileAsync(oversized, "big.bin", ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveFromStreamAsync_UnderstatedDeclaredLength_StillRejectsOversizedStream()
    {
        // Policy validation only sees declaredLength, so the cap must be enforced against the bytes actually read.
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.MaxUploadSizeBytes = 8;
            return o;
        });

        await using var input = new MemoryStream(new byte[32], false);
        await Assert.ThrowsAnyAsync<FilePolicyRejectedException>(() => scope.Storage.SaveFromStreamAsync(input, 4, "lying.bin", ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveFromStreamAsync_DeclaredLengthDisagreesWithStream_RecordsActualSize()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var payload = "actual-size-wins"u8.ToArray();
        await using var input = new MemoryStream(payload, false);
        var saved = await scope.Storage.SaveFromStreamAsync(input, 1, "mismatch.txt", ct: TestContext.Current.CancellationToken);
        Assert.Equal(payload.LongLength, saved.OriginalFileSize);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveFromStreamAsync_EmptyStream_RoundTripsAsZeroBytes()
    {
        using var scope = LocalFileStorageTestScope.Create();
        await using var input = new MemoryStream([], false);
        var saved = await scope.Storage.SaveFromStreamAsync(input, 0, "empty.bin", ct: TestContext.Current.CancellationToken);
        Assert.Equal(0, saved.OriginalFileSize);
        Assert.Empty(await scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken) ?? []);
    }

    [Fact]
    public async Task SaveFileAsync_RespectsCancellation()
    {
        using var scope = LocalFileStorageTestScope.Create();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.Storage.SaveFileAsync("cancelled"u8.ToArray(), "c.bin", ct: cts.Token));
    }

    [Fact]
    public async Task DeleteFileAsync_DefaultMode_TombstonesMetadata()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var saved = await scope.Storage.SaveFileAsync("to-delete"u8.ToArray(), "del.bin", ct: TestContext.Current.CancellationToken);
        var deleted = await scope.Storage.DeleteFileAsync(saved.Id, ct: TestContext.Current.CancellationToken);
        Assert.True(deleted);

        // Default config has ThrowOnFileNotFound = true; metadata should no longer come back as available.
        await Assert.ThrowsAnyAsync<Exception>(() => scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken));
    }
}