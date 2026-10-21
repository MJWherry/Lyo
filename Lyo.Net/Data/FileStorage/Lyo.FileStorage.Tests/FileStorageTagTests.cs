using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.KeyStore;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageTagTests : IDisposable
{
    private readonly IOTempSession _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Add_SameTagTwice_IsIdempotent()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        var first = await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        var second = await service.AddTagAsync(saved.Id, "client", "clienta", "again", TestCt);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(await service.GetTagsAsync(saved.Id, TestCt), tag => tag.TagType == "client" && tag.Value == "clienta");
    }

    [Fact]
    public async Task Add_ReservedType_IsRejected()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddTagAsync(saved.Id, FileTagTypes.FileType, "txt", ct: TestCt));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddTagAsync(saved.Id, FileTagTypes.Imported, "nope", ct: TestCt));
    }

    [Fact]
    public async Task Remove_AutomaticTag_IsRejected()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveTagAsync(saved.Id, FileTagTypes.Imported, FileTagTypes.FormatImported(saved.Timestamp), TestCt));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveTagAsync(saved.Id, FileTagTypes.FileType, "pdf", TestCt));
    }

    [Fact]
    public async Task Remove_UnknownTag_IsNoOp()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        Assert.False(await service.RemoveTagAsync(saved.Id, "client", "missing", TestCt));
    }

    [Fact]
    public async Task Save_NewFile_StampsFileTypeAndImported()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        var tags = await service.GetTagsAsync(saved.Id, TestCt);
        Assert.Equal("pdf", Assert.Single(tags, tag => tag.TagType == FileTagTypes.FileType).Value);
        var imported = Assert.Single(tags, tag => tag.TagType == FileTagTypes.Imported).Value;
        Assert.Equal(FileTagTypes.FormatImported(saved.Timestamp), imported);
        Assert.Null(saved.PathPrefix);
        var moved = saved with { Timestamp = saved.Timestamp.AddDays(3) };
        await service.MetadataStore.SaveMetadataAsync(saved.Id, moved, TestCt);
        Assert.Equal(imported, Assert.Single(await service.GetTagsAsync(saved.Id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value);
    }

    [Fact]
    public async Task Overwrite_SameId_DoesNotChangeImported()
    {
        using var session = new IOTempSession();
        using var service = new LocalFileStorageService(
            new() { RootDirectoryPath = session.SessionDirectory, EnableDuplicateDetection = true, DuplicateStrategy = DuplicateHandlingStrategy.Overwrite });

        var payload = "same-bytes"u8.ToArray();
        var first = await service.SaveFileAsync(payload, "report.PDF", ct: TestCt);
        var imported = Assert.Single(await service.GetTagsAsync(first.Id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value;
        var second = await service.SaveFileAsync(payload, "report.PDF", ct: TestCt);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(imported, Assert.Single(await service.GetTagsAsync(second.Id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value);
    }

    [Fact]
    public async Task Rename_ExtensionChange_ReplacesFileType()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        var imported = Assert.Single(await service.GetTagsAsync(saved.Id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value;
        await service.RenameFileAsync(saved.Id, new() { OriginalFileName = "notes.txt" }, TestCt);
        var tags = await service.GetTagsAsync(saved.Id, TestCt);
        Assert.Equal("txt", Assert.Single(tags, tag => tag.TagType == FileTagTypes.FileType).Value);
        Assert.Equal(imported, Assert.Single(tags, tag => tag.TagType == FileTagTypes.Imported).Value);
    }

    [Fact]
    public async Task Copy_NewFile_CopiesManualTagsAndRestampsImported()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        var imported = Assert.Single(await service.GetTagsAsync(saved.Id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value;
        var copy = await service.CopyFileAsync(saved.Id, ct: TestCt);
        var tags = await service.GetTagsAsync(copy.Id, TestCt);
        Assert.Contains(tags, tag => tag.TagType == "client" && tag.Value == "clienta" && !tag.Automatic);
        Assert.NotEqual(imported, Assert.Single(tags, tag => tag.TagType == FileTagTypes.Imported).Value);
        Assert.Equal(FileTagTypes.FormatImported(copy.Timestamp), Assert.Single(tags, tag => tag.TagType == FileTagTypes.Imported).Value);
    }

    [Fact]
    public async Task Transfer_PreserveBytes_CopiesTagsAndKeepsImported()
    {
        using var sourceSession = new IOTempSession();
        using var destinationSession = new IOTempSession();
        using var source = new LocalFileStorageService(new() { RootDirectoryPath = sourceSession.SessionDirectory });
        using var destination = new LocalFileStorageService(new() { RootDirectoryPath = destinationSession.SessionDirectory });
        var saved = await source.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await source.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        var imported = Assert.Single(await source.GetTagsAsync(saved.Id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value;
        var keys = new LocalKeyStore();
        await FileStorageTransfer.TransferAsync(
            new(source, source.MetadataStore, keys), new(destination, destination.MetadataStore, keys), new(FileStorageTransferScope.Everything, FileStorageKeyMode.PreserveKeys),
            TestCt);

        var tags = await destination.GetTagsAsync(saved.Id, TestCt);
        Assert.Contains(tags, tag => tag.TagType == "client" && tag.Value == "clienta");
        Assert.Equal(imported, Assert.Single(tags, tag => tag.TagType == FileTagTypes.Imported).Value);
    }

    [Fact]
    public async Task ListGroup_ClientThenGroup_NestsFiles()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", pathPrefix: "reports/2024", ct: TestCt);
        await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        await service.AddTagAsync(saved.Id, "group", "groupa", ct: TestCt);
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value), new("group", FileTagGrain.Value) };
        var leaf = await service.ListTagGroupAsync(keys, "clienta/groupa", ct: TestCt);
        Assert.Equal(saved.Id, Assert.Single(leaf.Entries).FileId);
        Assert.Equal("reports/2024", (await service.GetMetadataAsync(saved.Id, TestCt)).PathPrefix);
    }

    [Fact]
    public async Task ListGroup_ImportedYearMonthDay_NestsByCalendar()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        var stamp = saved.Timestamp.ToUniversalTime();
        var keys = new FileTagGroupKey[] {
            new(FileTagTypes.Imported, FileTagGrain.Year), new(FileTagTypes.Imported, FileTagGrain.Month), new(FileTagTypes.Imported, FileTagGrain.Day)
        };

        var path = $"{stamp:yyyy}/{stamp:MM}/{stamp:dd}";
        Assert.Equal(saved.Id, Assert.Single((await service.ListTagGroupAsync(keys, path, ct: TestCt)).Entries).FileId);
    }

    [Fact]
    public async Task ListGroup_ImportedMonthFirst_UsesYearMonth()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        var stamp = saved.Timestamp.ToUniversalTime();
        var keys = new FileTagGroupKey[] { new(FileTagTypes.Imported, FileTagGrain.Month) };
        var folder = Assert.Single((await service.ListTagGroupAsync(keys, null, ct: TestCt)).Entries);
        Assert.Equal($"{stamp:yyyy-MM}", folder.Name);
    }

    [Fact]
    public async Task ListGroup_MissingInnerTag_FileSitsInParent()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value), new("group", FileTagGrain.Value) };
        var parent = await service.ListTagGroupAsync(keys, "clienta", ct: TestCt);
        var stored = Assert.Single(parent.Entries);
        Assert.False(stored.IsDirectory);
        Assert.Equal(saved.Id, stored.FileId);
    }

    [Fact]
    public async Task ListGroup_SoftDeletedFile_IsOmitted()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        Assert.True(await service.DeleteFileAsync(saved.Id, FileDeletionMode.RemoveObjectAndTombstoneMetadata, TestCt));
        var keys = new FileTagGroupKey[] { new("client", FileTagGrain.Value) };
        Assert.Empty((await service.ListTagGroupAsync(keys, null, ct: TestCt)).Entries);
    }

    [Fact]
    public async Task GetTagTypes_IncludesBuiltInsAndManualTypes()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        var types = await service.GetTagTypesAsync(TestCt);
        Assert.Equal(FileTagTypes.FileType, types[0].TagType);
        Assert.Equal(FileTagValueKind.String, types[0].ValueKind);
        Assert.Equal(FileTagSort.Ordinal, types[0].Sort);
        Assert.Equal(FileTagTypes.Imported, types[1].TagType);
        Assert.Equal(FileTagSort.Chronological, types[1].Sort);
        Assert.Contains(FileTagGrain.Year, types[1].Grains);
        Assert.Equal("client", types[2].TagType);
        Assert.Equal(FileTagValueKind.String, types[2].ValueKind);
        Assert.False(types[2].Automatic);
    }

    [Fact]
    public async Task GetTagTypes_Imported_IsDateTimeYearMonthDay()
    {
        using var service = Create();
        var types = await service.GetTagTypesAsync(TestCt);
        var imported = Assert.Single(types, type => type.TagType == FileTagTypes.Imported);
        Assert.Equal(FileTagValueKind.DateTime, imported.ValueKind);
        Assert.Equal(FileTagSort.Chronological, imported.Sort);
        Assert.Equal([FileTagGrain.Year, FileTagGrain.Month, FileTagGrain.Day], imported.Grains);
    }

    [Fact]
    public async Task StampAutomaticTags_UnstampedRow_AddsFileTypeAndImported()
    {
        using var service = Create();
        var id = LyoGuid.CreateV7();
        var stamp = new DateTime(2024, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var hash = new byte[] { 1, 2, 3, 4 };
        var row = new FileStoreResult(id, "report.PDF", 8, hash, id.ToString("N"), 8, hash, false, null, null, null, false, null, null, null, null, null, null, null, null, stamp);
        await service.MetadataStore.SaveMetadataAsync(id, row, TestCt);
        Assert.Empty(await service.GetTagsAsync(id, TestCt));
        Assert.Equal(1, await service.StampAutomaticTagsAsync(TestCt));
        var tags = await service.GetTagsAsync(id, TestCt);
        Assert.Equal("pdf", Assert.Single(tags, tag => tag.TagType == FileTagTypes.FileType).Value);
        Assert.Equal(FileTagTypes.FormatImported(stamp), Assert.Single(tags, tag => tag.TagType == FileTagTypes.Imported).Value);
        Assert.Equal(1, await service.StampAutomaticTagsAsync(TestCt));
        Assert.Equal(FileTagTypes.FormatImported(stamp), Assert.Single(await service.GetTagsAsync(id, TestCt), tag => tag.TagType == FileTagTypes.Imported).Value);
    }

    [Fact]
    public async Task StampAutomaticTags_PendingDirectUpload_IsSkipped()
    {
        using var service = Create();
        var id = LyoGuid.CreateV7();
        var hash = new byte[] { 1, 2, 3, 4 };
        var row = new FileStoreResult(
            id, "pending.bin", 8, hash, id.ToString("N"), 8, hash, false, null, null, null, false, null, null, null, null, null, null, null, null,
            new(2024, 3, 2, 8, 0, 0, DateTimeKind.Utc), Availability: FileAvailability.PendingDirectUpload);

        await service.MetadataStore.SaveMetadataAsync(id, row, TestCt);
        Assert.Equal(0, await service.StampAutomaticTagsAsync(TestCt));
        Assert.Empty(await service.GetTagsAsync(id, TestCt));
    }

    [Fact]
    public async Task Purge_RemovesTagRows()
    {
        using var service = Create();
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        await service.AddTagAsync(saved.Id, "client", "clienta", ct: TestCt);
        Assert.True(await service.DeleteFileAsync(saved.Id, FileDeletionMode.RemoveObjectAndPurgeMetadata, TestCt));
        Assert.Empty(await service.MetadataStore.ListFileTagsAsync(saved.Id, TestCt));
    }

    [Fact]
    public async Task LocalFileStorage_WithCallerMetadataStore_StampsTags()
    {
        var metadata = new LocalFileMetadataStore(_temp.SessionDirectory);
        using var service = new LocalFileStorageService(new() { RootDirectoryPath = _temp.SessionDirectory }, metadataService: metadata);
        var saved = await service.SaveFileAsync("pdf"u8.ToArray(), "report.PDF", ct: TestCt);
        var tags = await metadata.ListFileTagsAsync(saved.Id, TestCt);
        Assert.Contains(tags, tag => tag.Automatic && tag.TagType == FileTagTypes.FileType && tag.Value == "pdf");
        Assert.Contains(tags, tag => tag.Automatic && tag.TagType == FileTagTypes.Imported && tag.Value == FileTagTypes.FormatImported(saved.Timestamp));
    }

    private LocalFileStorageService Create() => new(new() { RootDirectoryPath = _temp.SessionDirectory });
}