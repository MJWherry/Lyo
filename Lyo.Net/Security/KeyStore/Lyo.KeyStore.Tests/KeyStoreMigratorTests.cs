using Lyo.Testing;
using KeyNotFoundException = Lyo.KeyStore.Exceptions.KeyNotFoundException;

namespace Lyo.KeyStore.Tests;

public class KeyStoreMigratorTests
{
    [Fact]
    public async Task CopyAsync_TwoVersions_DestinationGetKeyMatchesSourceAndCurrentPointer()
    {
        var source = new LocalKeyStore();
        source.AddKeyFromString("app", "1", "alpha-passphrase");
        source.AddKeyFromString("app", "2", "beta-passphrase");
        source.SetCurrentVersion("app", "2");
        var destination = new LocalKeyStore();
        await KeyStoreMigrator.CopyAsync(source, destination, ["app"], ct: TestCt);
        Assert.Equal(source.GetKey("app", "1"), destination.GetKey("app", "1"));
        Assert.Equal(source.GetKey("app", "2"), destination.GetKey("app", "2"));
        Assert.Equal("2", destination.GetCurrentVersion("app"));
        Assert.Equal(source.GetSaltForVersion("app", "1"), destination.GetSaltForVersion("app", "1"));
        Assert.Equal(source.GetSaltForVersion("app", "2"), destination.GetSaltForVersion("app", "2"));
    }

    [Fact]
    public async Task CopyAsync_KeyMissingOnSource_ThrowsBeforeDestinationWrites()
    {
        var source = new LocalKeyStore();
        source.AddKey("other", "1", TestData.Create(32));
        var destination = new LocalKeyStore();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => KeyStoreMigrator.CopyAsync(source, destination, ["missing"], ct: TestCt));
        Assert.Empty(await destination.GetAvailableKeyIdsAsync(TestCt));
    }

    [Fact]
    public async Task CopyAsync_DestinationVersionBytesDiffer_ThrowsBeforeOverwrite()
    {
        var sourceBytes = TestData.Create(32);
        var destinationBytes = TestData.Create(32, TestData.Seed + 1);
        var source = new LocalKeyStore();
        source.AddKey("app", "1", sourceBytes);
        source.AddKey("app", "2", TestData.Create(32, TestData.Seed + 2));
        source.SetCurrentVersion("app", "2");
        var destination = new LocalKeyStore();
        destination.AddKey("app", "1", destinationBytes);
        destination.SetCurrentVersion("app", "1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => KeyStoreMigrator.CopyAsync(source, destination, ["app"], ct: TestCt));
        Assert.Equal(destinationBytes, destination.GetKey("app", "1"));
        Assert.False(destination.HasKey("app", "2"));
    }

    [Fact]
    public async Task CopyAsync_DestinationCurrentDiffers_ThrowsAfterVersionsMatch()
    {
        var version1 = TestData.Create(32);
        var version2 = TestData.Create(32, TestData.Seed + 1);
        var source = new LocalKeyStore();
        source.AddKey("app", "1", version1);
        source.AddKey("app", "2", version2);
        source.SetCurrentVersion("app", "2");
        var destination = new LocalKeyStore();
        destination.AddKey("app", "1", version1);
        destination.AddKey("app", "2", version2);
        destination.SetCurrentVersion("app", "1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => KeyStoreMigrator.CopyAsync(source, destination, ["app"], ct: TestCt));
        Assert.Equal("1", destination.GetCurrentVersion("app"));
    }

    [Fact]
    public async Task CopyAsync_WhenDestinationGetKeyDiffers_ThrowsBeforeSetCurrentVersion()
    {
        var source = new LocalKeyStore();
        source.AddKey("app", "1", TestData.Create(32));
        source.SetCurrentVersion("app", "1");
        var destination = new DivergentKeyStore();
        await Assert.ThrowsAsync<InvalidOperationException>(() => KeyStoreMigrator.CopyAsync(source, destination, ["app"], ct: TestCt));
        Assert.False(destination.CurrentVersionWasSet);
    }

    [Fact]
    public async Task CopyInventoryAsync_CopiesEveryId_LeavesDestinationCurrentVersions()
    {
        var source = new LocalKeyStore();
        source.AddKey("app", "1", TestData.Create(32));
        source.SetCurrentVersion("app", "1");
        source.AddKey("other", "1", TestData.Create(32, TestData.Seed + 1));
        source.AddKey("other", "2", TestData.Create(32, TestData.Seed + 2));
        source.SetCurrentVersion("other", "2");
        var destination = new LocalKeyStore();
        await KeyStoreMigrator.CopyInventoryAsync(source, destination, ct: TestCt);
        Assert.Equal(source.GetKey("app", "1"), destination.GetKey("app", "1"));
        Assert.Equal("1", destination.GetCurrentVersion("app"));
        Assert.Equal(source.GetKey("other", "1"), destination.GetKey("other", "1"));
        Assert.Equal(source.GetKey("other", "2"), destination.GetKey("other", "2"));
        Assert.Equal("2", destination.GetCurrentVersion("other"));
    }

    [Fact]
    public async Task CopyAsync_Progress_ReportsEachVersion()
    {
        var source = new LocalKeyStore();
        source.AddKey("app", "1", TestData.Create(32));
        source.AddKey("app", "2", TestData.Create(32, TestData.Seed + 1));
        source.SetCurrentVersion("app", "2");
        var reports = new List<KeyStoreMigrationProgress>();
        await KeyStoreMigrator.CopyAsync(source, new LocalKeyStore(), ["app"], new ListProgress<KeyStoreMigrationProgress>(reports), TestCt);
        Assert.Equal(["1", "2"], reports.Select(report => report.Version).ToArray());
        Assert.All(
            reports, report => {
                Assert.Equal("app", report.KeyId);
                Assert.Equal(KeyStoreMigrationItemOutcome.Copied, report.Outcome);
                Assert.Equal(1, report.KeysTotal);
            });

        Assert.Equal(1, reports[^1].KeysCompleted);
    }

    private sealed class ListProgress<T>(List<T> items) : IProgress<T>
    {
        public void Report(T value) => items.Add(value);
    }
}