using Lyo.FileStorage;
using Lyo.FileStorage.Models;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Recipe.FileStorage.Tests;

[Trait("Category", "Fast")]
public sealed class RecipeAttachmentFilesTests
{
    [Fact]
    public async Task SaveAsync_WritesFileAndAttachmentRow()
    {
        var ct = TestCt;
        var root = Path.Combine(Path.GetTempPath(), "lyo-recipe-fs-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            using var storage = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = root });
            var store = new MemoryRecipeStore();
            var helper = new RecipeAttachmentFiles(storage, store);
            var recipeId = LyoGuid.CreateV7();
            await using var content = new MemoryStream("hello"u8.ToArray());
            var row = await helper.SaveAsync(
                RecipeEntityTypes.Recipe, recipeId, content, content.Length, "scan.bin", RecipeAttachmentRole.Scan, "caption", tenantId: null, ct: ct);
            Assert.Equal(recipeId, row.EntityId);
            Assert.NotEqual(Guid.Empty, row.FileId);
            Assert.Equal(RecipeAttachmentRole.Scan, row.Role);
            Assert.Single(store.Attachments);
            var loaded = await storage.GetMetadataAsync(row.FileId, ct);
            Assert.Equal(RecipeFilePaths.ForRecipe(recipeId), loaded.PathPrefix);
            var bytes = await storage.GetFileAsync(row.FileId, ct: ct);
            Assert.Equal("hello"u8.ToArray(), bytes);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveAsync_CardRole_StoresPdfBytes()
    {
        var ct = TestCt;
        var root = Path.Combine(Path.GetTempPath(), "lyo-recipe-fs-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            using var storage = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = root });
            var store = new MemoryRecipeStore();
            var helper = new RecipeAttachmentFiles(storage, store);
            var recipeId = LyoGuid.CreateV7();
            var pdf = "%PDF-1.4 card"u8.ToArray();
            await using var content = new MemoryStream(pdf);
            var row = await helper.SaveAsync(
                RecipeEntityTypes.Recipe, recipeId, content, content.Length, "card.pdf", RecipeAttachmentRole.Card, ct: ct);
            Assert.Equal(RecipeAttachmentRole.Card, row.Role);
            var bytes = await storage.GetFileAsync(row.FileId, ct: ct);
            Assert.Equal(pdf, bytes);
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
        var root = Path.Combine(Path.GetTempPath(), "lyo-recipe-fs-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            using var storage = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = root });
            var store = new MemoryRecipeStore();
            var helper = new RecipeAttachmentFiles(storage, store);
            var recipeId = LyoGuid.CreateV7();
            await using var content = new MemoryStream("bye"u8.ToArray());
            var row = await helper.SaveAsync(RecipeEntityTypes.Recipe, recipeId, content, content.Length, "x.bin", RecipeAttachmentRole.Document, ct: ct);
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
