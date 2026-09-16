using Lyo.Common.Core.Identifiers;
using Lyo.Encryption;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Tests;

public class FileMetadataNameAndVersionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-meta-name-" + LyoGuid.CreateV7().ToString("N"));

    public FileMetadataNameAndVersionTests() => Directory.CreateDirectory(_root);

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
    public async Task SearchByName_FindsSubstring_AndSkipsOtherFile()
    {
        var store = new LocalFileMetadataStore(_root);
        var match = Meta(LyoGuid.CreateV7(), "quarterly-report.pdf");
        var other = Meta(LyoGuid.CreateV7(), "notes.txt");
        await store.SaveMetadataAsync(match.Id, match, TestCt);
        await store.SaveMetadataAsync(other.Id, other, TestCt);

        var found = await store.SearchByNameAsync("report", 20, TestCt);

        Assert.Contains(found, row => row.Id == match.Id);
        Assert.DoesNotContain(found, row => row.Id == other.Id);
    }

    [Fact]
    public async Task SaveNewVersion_PreviousKeyStillReadable()
    {
        var store = new LocalFileMetadataStore(_root);
        var file = Meta(LyoGuid.CreateV7(), "a.txt") with { SourceFileName = "old-key.bin" };
        await store.SaveMetadataAsync(file.Id, file, TestCt);

        var revision = await store.SaveNewVersionAsync(file.Id, "new-key.bin", TestCt);
        var loaded = await store.GetVersionAsync(revision.Id, TestCt);
        var current = await store.GetMetadataAsync(file.Id, TestCt);

        Assert.NotNull(loaded);
        Assert.Equal("old-key.bin", loaded.StoredKey);
        Assert.Equal(file.Id, loaded.ParentId);
        Assert.Equal("new-key.bin", current.SourceFileName);
    }

    private static FileStoreResult Meta(Guid id, string name)
        => new(
            id, name, 1, [1], id.ToString("N") + ".bin", 1, [1], false, null, null, null, false, null, null, null, null, null, null, null, null, DateTime.UtcNow, null,
            HashAlgorithm.Sha256);
}
