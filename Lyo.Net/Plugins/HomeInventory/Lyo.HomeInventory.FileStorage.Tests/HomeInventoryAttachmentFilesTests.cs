using Lyo.FileStorage;
using Lyo.FileStorage.Models;
using Lyo.HomeInventory;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.Common.Core.Identifiers;

namespace Lyo.HomeInventory.FileStorage.Tests;

[Trait("Category", "Fast")]
public sealed class HomeInventoryAttachmentFilesTests
{
    [Fact]
    public async Task SaveAsync_WritesFileAndAttachmentRow()
    {
        var ct = TestCt;
        var root = Path.Combine(Path.GetTempPath(), "lyo-hi-fs-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            using var storage = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = root });
            var store = new MemoryHomeInventoryStore();
            var helper = new HomeInventoryAttachmentFiles(storage, store);
            var itemId = LyoGuid.CreateV7();
            await using var content = new MemoryStream("hello"u8.ToArray());
            var row = await helper.SaveAsync(
                HomeInventoryEntityTypes.Item, itemId, content, content.Length, "note.txt", HomeAttachmentRole.Document, "caption", tenantId: null, ct: ct);
            Assert.Equal(itemId, row.EntityId);
            Assert.NotEqual(Guid.Empty, row.FileId);
            Assert.Equal(HomeAttachmentRole.Document, row.Role);
            Assert.Single(store.Attachments);
            var loaded = await storage.GetMetadataAsync(row.FileId, ct);
            Assert.Equal(HomeInventoryFilePaths.ForItem(itemId), loaded.PathPrefix);
            var bytes = await storage.GetFileAsync(row.FileId, ct: ct);
            Assert.Equal("hello"u8.ToArray(), bytes);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesRowAndFile()
    {
        var ct = TestCt;
        var root = Path.Combine(Path.GetTempPath(), "lyo-hi-fs-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            using var storage = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = root });
            var store = new MemoryHomeInventoryStore();
            var helper = new HomeInventoryAttachmentFiles(storage, store);
            var itemId = LyoGuid.CreateV7();
            await using var content = new MemoryStream("bye"u8.ToArray());
            var row = await helper.SaveAsync(HomeInventoryEntityTypes.Item, itemId, content, content.Length, "x.bin", HomeAttachmentRole.Photo, ct: ct);
            await helper.DeleteAsync(row.Id, deleteStoredFile: true, tenantId: null, ct);
            Assert.Empty(store.Attachments);
            await Assert.ThrowsAnyAsync<Exception>(() => storage.GetMetadataAsync(row.FileId, ct));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
