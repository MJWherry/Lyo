using Lyo.Api.FileStorage.Models;
using Lyo.FileStorage.Web.Components.FileStorageManagement;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStoragePresenceUiTests
{
    [Fact]
    public void TryChip_FileBoth_UsesBackendKind()
    {
        var entry = new FileSystemEntry(
            "/a", "a", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            new Dictionary<string, string> { ["Presence"] = nameof(FileStoragePresence.Both) });
        Assert.True(FileStoragePresenceUi.TryChip(entry, FileStorageBackendKind.AwsS3, out var label, out var hint, out _, out _));
        Assert.Equal("Both", label);
        Assert.Contains("catalog", hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("S3", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void TryChip_Physical_SaysS3Only()
    {
        var entry = new FileSystemEntry(
            "/a", "a.bin", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            new Dictionary<string, string> { ["Presence"] = nameof(FileStoragePresence.Physical) });
        Assert.True(FileStoragePresenceUi.TryChip(entry, FileStorageBackendKind.AwsS3, out var label, out var hint, out _, out _));
        Assert.Equal("S3 only", label);
        Assert.Contains("not in the catalog", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryChip_Store_SaysCatalogOnly()
    {
        var entry = new FileSystemEntry(
            "/a", "a", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            new Dictionary<string, string> { ["Presence"] = nameof(FileStoragePresence.Store) });
        Assert.True(FileStoragePresenceUi.TryChip(entry, FileStorageBackendKind.Local, out var label, out var hint, out _, out _));
        Assert.Equal("Catalog only", label);
        Assert.Contains("Local", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void TryChip_Directory_Hidden()
    {
        var entry = new FileSystemEntry(
            "/report", "report", true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            new Dictionary<string, string> {
                ["Presence"] = nameof(FileStoragePresence.Physical),
                ["OriginalFileName"] = "report"
            });
        Assert.False(FileStoragePresenceUi.TryChip(entry, FileStorageBackendKind.Local, out _, out _, out _, out _));
    }

    [Theory]
    [InlineData(FileStoragePresence.Store, true)]
    [InlineData(FileStoragePresence.Both, true)]
    [InlineData(FileStoragePresence.None, true)]
    [InlineData(FileStoragePresence.Physical, false)]
    public void InCatalog_PhysicalOnly_IsFalse(FileStoragePresence presence, bool expected)
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal(expected, FileStoragePresenceUi.InCatalog(presence, fileId));
    }

    [Fact]
    public void Collect_SplitsStoreOnlyAndPhysical()
    {
        var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var store = new FileTreeNode(
            new FileSystemEntry(
                "/" + storeId.ToString("D"), "gone.txt", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                new Dictionary<string, string> {
                    ["Presence"] = nameof(FileStoragePresence.Store),
                    ["FileId"] = storeId.ToString("D")
                }));
        var physical = new FileTreeNode(
            new FileSystemEntry(
                "/orphan.bin", "orphan.bin", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                new Dictionary<string, string> {
                    ["Presence"] = nameof(FileStoragePresence.Physical),
                    ["PhysicalKey"] = "orphan.bin"
                }));
        var ids = new HashSet<Guid>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        FileStoragePresenceUi.Collect(store, ids, keys);
        FileStoragePresenceUi.Collect(physical, ids, keys);
        Assert.Equal(storeId, Assert.Single(ids));
        Assert.Equal("orphan.bin", Assert.Single(keys));
    }
}
