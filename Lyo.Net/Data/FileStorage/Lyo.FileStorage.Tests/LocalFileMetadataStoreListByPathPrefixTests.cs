using Lyo.Encryption;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore.Tests;

public class LocalFileMetadataStoreListByPathPrefixTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-meta-list-" + Guid.NewGuid().ToString("N"));

    public LocalFileMetadataStoreListByPathPrefixTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch {
            // best effort
        }
    }

    [Fact]
    public async Task ListByPathPrefixAsync_FiltersImmediateVsDescendants()
    {
        var store = new LocalFileMetadataStore(_root);
        var root = Meta(Guid.NewGuid(), null, "root.txt");
        var reports = Meta(Guid.NewGuid(), "reports", "a.pdf");
        var nested = Meta(Guid.NewGuid(), "reports/2024", "b.pdf");
        await store.SaveMetadataAsync(root.Id, root, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(reports.Id, reports, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(nested.Id, nested, TestContext.Current.CancellationToken);
        var immediate = await store.ListByPathPrefixAsync("reports", false, 50, TestContext.Current.CancellationToken);
        Assert.Single(immediate);
        Assert.Equal(reports.Id, immediate[0].Id);
        var descendants = await store.ListByPathPrefixAsync("reports", true, 50, TestContext.Current.CancellationToken);
        Assert.Equal(2, descendants.Count);
    }

    private static FileStoreResult Meta(Guid id, string? prefix, string name)
        => new(
            id, name, 1, [1], id.ToString("N") + ".bin", 1, [1], false, null, null, null, false, null, null, null, null, null, null, null, null, DateTime.UtcNow, prefix,
            HashAlgorithm.Sha256);
}
