using Lyo.Api.FileStorage.Models;
using Lyo.FileStorage.Web.Components.FileStorageManagement;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStorageHttpTreeSourceTests
{
    [Fact]
    public void ToEntryAndToPathNode_RoundTripPresenceFlags()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        foreach (var presence in new[] { FileStoragePresence.None, FileStoragePresence.Store, FileStoragePresence.Physical, FileStoragePresence.Both }) {
            var dto = new FileStorageFolderEntryDto("keep.txt", false, "reports", fileId, presence, "keep.txt", 4, "reports/" + fileId.ToString("N"));
            var entry = FileStorageHttpTreeSource.ToEntry(dto);
            Assert.Equal(presence.ToString(), entry.Properties!["Presence"]);
            var node = FileStorageHttpTreeSource.ToPathNode(entry);
            Assert.Equal(presence, node.Presence);
            Assert.Equal(fileId, node.FileId);
            Assert.Equal("reports", node.PathPrefix);
            Assert.Equal("reports/" + fileId.ToString("N"), node.PhysicalKey);
            Assert.Equal("reports/" + fileId.ToString("N"), entry.Properties!["PhysicalKey"]);
        }
    }

    [Fact]
    public void ToEntry_Directory_OmitsFileChips()
    {
        var dto = new FileStorageFolderEntryDto("report", true, "report", null, FileStoragePresence.Physical, null, 0, null);
        var entry = FileStorageHttpTreeSource.ToEntry(dto);
        Assert.Equal(nameof(FileStoragePresence.Physical), entry.Properties!["Presence"]);
        Assert.False(entry.Properties.ContainsKey("FileId"));
        Assert.False(entry.Properties.ContainsKey("OriginalFileName"));
        Assert.False(entry.Properties.ContainsKey("PhysicalKey"));
    }

    [Fact]
    public void ToPathNode_PhysicalOnly_KeepsPhysicalKeyWithoutCatalog()
    {
        var dto = new FileStorageFolderEntryDto("orphan.bin", false, null, null, FileStoragePresence.Physical, null, 0, "orphan.bin");
        var entry = FileStorageHttpTreeSource.ToEntry(dto);
        var node = FileStorageHttpTreeSource.ToPathNode(entry);
        Assert.Equal(FileStoragePresence.Physical, node.Presence);
        Assert.Null(node.FileId);
        Assert.Equal("orphan.bin", node.PhysicalKey);
    }

    [Fact]
    public void PresenceEnum_HasNoBlobOrCloudNames()
    {
        var names = Enum.GetNames<FileStoragePresence>();
        Assert.Contains(nameof(FileStoragePresence.Store), names);
        Assert.Contains(nameof(FileStoragePresence.Physical), names);
        Assert.Contains(nameof(FileStoragePresence.Both), names);
        Assert.DoesNotContain(names, n => n.Contains("Blob", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Cloud", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PrefixFromPath_RootAndFolder()
    {
        Assert.Null(FileStorageHttpTreeSource.PrefixFromPath("/"));
        Assert.Equal("reports/2024", FileStorageHttpTreeSource.PrefixFromPath("/reports/2024"));
    }

    [Fact]
    public void DirectoryNode_DoesNotNeedAPopulatedRoot()
    {
        var root = FileStorageHttpTreeSource.DirectoryNode(null);
        Assert.True(root.IsDirectory);
        Assert.Null(root.PathPrefix);
        Assert.Equal(FileStoragePathTreeBuilder.RootDisplayName, root.Name);
        Assert.Equal("/", FileStorageHttpTreeSource.DirectoryVfsPath(null));

        var nested = FileStorageHttpTreeSource.DirectoryNode("reports/2024");
        Assert.True(nested.IsDirectory);
        Assert.Equal("reports/2024", nested.PathPrefix);
        Assert.Equal("2024", nested.Name);
        Assert.Equal("/reports/2024", FileStorageHttpTreeSource.DirectoryVfsPath("reports/2024"));
    }
}
