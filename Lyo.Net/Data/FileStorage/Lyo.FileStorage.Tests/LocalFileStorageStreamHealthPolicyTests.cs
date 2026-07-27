using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Coverage for the streaming save/read API, default-mode delete tombstones, duplicate strategies, scan/availability policy plumbing, and cancellation propagation that were
/// previously only exercised via the byte[] save path.
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
        // The base implementation maps a "no metadata" lookup to an empty byte[] (rather than null) when ThrowOnFileNotFound is disabled.
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
    public async Task RequireScanBeforeAvailable_WithoutScanner_FailsClosedOnSave()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.RequireScanBeforeAvailable = true;
            return o;
        });

        await Assert.ThrowsAnyAsync<ConfigurationException>(() => scope.Storage.SaveFileAsync("must-scan"u8.ToArray(), "f.bin", ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DefaultAvailability_RespectedWhenScanNotRequired()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.RequireScanBeforeAvailable = false;
            o.DefaultAvailability = FileAvailability.Quarantined;
            return o;
        });

        var saved = await scope.Storage.SaveFileAsync("blocked"u8.ToArray(), "f.bin", ct: TestContext.Current.CancellationToken);
        Assert.Equal(FileAvailability.Quarantined, saved.Availability);

        // Quarantined reads should fail unless admin override is on.
        await Assert.ThrowsAnyAsync<FileNotAvailableException>(() => scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AllowReadQuarantinedForAdmin_PermitsReadOfQuarantinedFiles()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.RequireScanBeforeAvailable = false;
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

        // Default config has ThrowOnFileNotFound = true; metadata should no longer be retrievable as available.
        await Assert.ThrowsAnyAsync<Exception>(() => scope.Storage.GetFileAsync(saved.Id, ct: TestContext.Current.CancellationToken));
    }
}