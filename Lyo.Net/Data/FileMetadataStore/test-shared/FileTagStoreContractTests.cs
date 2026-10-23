using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Tests;

/// <summary>Shared file-tag contract for Postgres and SQLite metadata stores.</summary>
public abstract class FileTagStoreContractTests
{
    private const int MaxKeys = 100_000;

    /// <summary>Root fixture provider.</summary>
    protected abstract IServiceProvider Services { get; }

    /// <summary>Metadata store on the scoped context. Tag operations go through this store.</summary>
    protected abstract IFileMetadataStore CreateStore(IServiceScope scope);

    [Fact]
    public async Task Add_SameTagTwice_IsIdempotent()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "notes.txt", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
        var first = await files.AddFileTagAsync(file.Id, "client", "clienta", null, false, TestCt);
        var second = await files.AddFileTagAsync(file.Id, "client", "clienta", "again", false, TestCt);
        var listed = await files.ListFileTagsAsync(file.Id, TestCt);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(listed, row => row.TagType == "client" && row.Value == "clienta");
    }

    [Fact]
    public async Task Remove_UnknownTag_IsNoOp()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "notes.txt", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
        Assert.False(await files.RemoveFileTagAsync(file.Id, "client", "missing", TestCt));
    }

    [Fact]
    public async Task ListGroup_ClientThenGroup_NestsFiles()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "report.pdf", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc), "reports/2024");
        var client = "client-" + file.Id.ToString("N");
        var group = "group-" + file.Id.ToString("N");
        await files.AddFileTagAsync(file.Id, "client", client, null, false, TestCt);
        await files.AddFileTagAsync(file.Id, "group", group, null, false, TestCt);
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value), new("group", FileTagGrain.Value) };
        var root = await files.ListTagGroupAsync(keys, null, MaxKeys, TestCt);
        Assert.Contains(root.Entries, entry => entry.IsDirectory && entry.Name == client);
        Assert.DoesNotContain(root.Entries, entry => entry.FileId == file.Id);
        var groups = await files.ListTagGroupAsync(keys, client, MaxKeys, TestCt);
        Assert.Contains(groups.Entries, entry => entry.IsDirectory && entry.Name == group);
        var leaf = await files.ListTagGroupAsync(keys, client + "/" + group, MaxKeys, TestCt);
        Assert.Equal(file.Id, Assert.Single(leaf.Entries).FileId);
        Assert.Equal("reports/2024", (await files.GetMetadataAsync(file.Id, TestCt)).PathPrefix);
    }

    [Fact]
    public async Task ListGroup_TwoClientValues_FileAppearsTwice()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "report.pdf", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
        var clientA = "clienta-" + file.Id.ToString("N");
        var clientB = "clientb-" + file.Id.ToString("N");
        await files.AddFileTagAsync(file.Id, "client", clientA, null, false, TestCt);
        await files.AddFileTagAsync(file.Id, "client", clientB, null, false, TestCt);
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value) };
        var root = await files.ListTagGroupAsync(keys, null, MaxKeys, TestCt);
        Assert.Contains(root.Entries, entry => entry.IsDirectory && entry.Name == clientA);
        Assert.Contains(root.Entries, entry => entry.IsDirectory && entry.Name == clientB);
        Assert.Equal(file.Id, Assert.Single((await files.ListTagGroupAsync(keys, clientA, MaxKeys, TestCt)).Entries).FileId);
        Assert.Equal(file.Id, Assert.Single((await files.ListTagGroupAsync(keys, clientB, MaxKeys, TestCt)).Entries).FileId);
    }

    [Fact]
    public async Task ListGroup_MissingInnerTag_FileSitsInParent()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "report.pdf", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
        var client = "client-" + file.Id.ToString("N");
        await files.AddFileTagAsync(file.Id, "client", client, null, false, TestCt);
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value), new("group", FileTagGrain.Value) };
        var parent = await files.ListTagGroupAsync(keys, client, MaxKeys, TestCt);
        var stored = Assert.Single(parent.Entries);
        Assert.False(stored.IsDirectory);
        Assert.Equal(file.Id, stored.FileId);
    }

    [Fact]
    public async Task ListGroup_ImportedYearMonthDay_NestsByCalendar()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var stamp = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);
        var file = await SaveAsync(files, "report.pdf", stamp);
        await files.AddFileTagAsync(file.Id, FileTagTypes.Imported, FileTagTypes.FormatImported(stamp), null, true, TestCt);
        var keys = new FileTagGroupKey[] {
            new(FileTagTypes.Imported, FileTagGrain.Year), new(FileTagTypes.Imported, FileTagGrain.Month), new(FileTagTypes.Imported, FileTagGrain.Day)
        };

        Assert.Contains((await files.ListTagGroupAsync(keys, null, MaxKeys, TestCt)).Entries, entry => entry.IsDirectory && entry.Name == "2026");
        var months = await files.ListTagGroupAsync(keys, "2026", MaxKeys, TestCt);
        Assert.Contains(months.Entries, entry => entry.IsDirectory && entry.Name == "10");
        Assert.DoesNotContain(months.Entries, entry => entry.IsDirectory && entry.Name == "2026-10");
        var days = await files.ListTagGroupAsync(keys, "2026/10", MaxKeys, TestCt);
        Assert.Contains(days.Entries, entry => entry.IsDirectory && entry.Name == "16");
        Assert.Contains((await files.ListTagGroupAsync(keys, "2026/10/16", MaxKeys, TestCt)).Entries, entry => entry.FileId == file.Id);
    }

    [Fact]
    public async Task ListGroup_ImportedMonthFirst_UsesYearMonth()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var stamp = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);
        var file = await SaveAsync(files, "report.pdf", stamp);
        await files.AddFileTagAsync(file.Id, FileTagTypes.Imported, FileTagTypes.FormatImported(stamp), null, true, TestCt);
        var keys = new FileTagGroupKey[] { new(FileTagTypes.Imported, FileTagGrain.Month) };
        var root = await files.ListTagGroupAsync(keys, null, MaxKeys, TestCt);
        Assert.Contains(root.Entries, entry => entry.IsDirectory && entry.Name == "2026-10");
        Assert.DoesNotContain(root.Entries, entry => entry.IsDirectory && entry.Name == "10");
        Assert.Contains((await files.ListTagGroupAsync(keys, "2026-10", MaxKeys, TestCt)).Entries, entry => entry.FileId == file.Id);
    }

    [Fact]
    public async Task ListGroup_SoftDeletedFile_IsOmitted()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "report.pdf", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
        var client = "client-" + file.Id.ToString("N");
        await files.AddFileTagAsync(file.Id, "client", client, null, false, TestCt);
        Assert.True(await files.DeleteMetadataAsync(file.Id, TestCt));
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value) };
        var root = await files.ListTagGroupAsync(keys, null, MaxKeys, TestCt);
        Assert.DoesNotContain(root.Entries, entry => entry.Name == client || entry.FileId == file.Id);
    }

    [Fact]
    public async Task Purge_RemovesTagRows()
    {
        using var scope = Services.CreateScope();
        var files = CreateStore(scope);
        var file = await SaveAsync(files, "report.pdf", new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
        await files.AddFileTagAsync(file.Id, "client", "clienta", null, false, TestCt);
        Assert.True(await files.PurgeMetadataAsync(file.Id, TestCt));
        Assert.Empty(await files.ListFileTagsAsync(file.Id, TestCt));
    }

    private static async Task<FileStoreResult> SaveAsync(IFileMetadataStore files, string name, DateTime timestamp, string? pathPrefix = null)
    {
        var id = LyoGuid.CreateV7();
        var hash = new byte[] { 1, 2, 3, 4 };
        var row = new FileStoreResult(
            id, name, 8, hash, id.ToString("N"), 8, hash, false, null, null, null, false, null, null, null, null, null, null, null, null, timestamp, pathPrefix);

        await files.SaveMetadataAsync(id, row, TestCt);
        return row;
    }
}