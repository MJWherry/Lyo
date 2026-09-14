using Lyo.FileMetadataStore;
using Lyo.FileStorage.Tests.Support;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.Tests;

public class FileStoragePhysicalAndReconcileTests
{
    [Fact]
    public void LocalService_ExposesPhysicalFileSystem_NotCatalog()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var physical = Assert.IsAssignableFrom<IFileStoragePhysical>(scope.Storage);
        Assert.IsType<LocalFileSystem>(physical.Physical);
        Assert.DoesNotContain(typeof(IFileSystem), scope.Storage.GetType().GetInterfaces());
    }

    [Fact]
    public async Task Reconcile_BothStoreAndPhysical()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var saved = await scope.Storage.SaveFileAsync("x"u8.ToArray(), "keep.txt", pathPrefix: "reports", ct: TestContext.Current.CancellationToken);
        var physical = ((IFileStoragePhysical)scope.Storage).Physical;
        var metadata = new LocalFileMetadataStore(scope.Options.RootDirectoryPath);
        var both = await FileStorageReconcile.ReconcileAsync(metadata, physical, ct: TestContext.Current.CancellationToken);
        Assert.Contains(both, e => e.Presence == FileStoragePresence.Both && e.FileId == saved.Id);

        var bothEntry = Assert.Single(both, e => e.Presence == FileStoragePresence.Both && e.FileId == saved.Id);
        Assert.False(string.IsNullOrEmpty(bothEntry.PhysicalKey));
        physical.DeleteFile(Path.Combine(physical.RootPath, bothEntry.PhysicalKey!.Replace('/', Path.DirectorySeparatorChar)));
        var storeOnly = await FileStorageReconcile.ReconcileAsync(metadata, physical, ct: TestContext.Current.CancellationToken);
        Assert.Contains(storeOnly, e => e.Presence == FileStoragePresence.Store && e.FileId == saved.Id);

        physical.WriteAllBytes(Path.Combine(physical.RootPath, "orphan.bin"), "z"u8.ToArray());
        var after = await FileStorageReconcile.ReconcileAsync(metadata, physical, ct: TestContext.Current.CancellationToken);
        Assert.Contains(after, e => e.Presence == FileStoragePresence.Physical);
    }

    [Fact]
    public async Task Reconcile_SkipsHealthAndMetaSidecars()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var saved = await scope.Storage.SaveFileAsync("x"u8.ToArray(), "keep.txt", pathPrefix: "reports", ct: TestContext.Current.CancellationToken);
        var physical = ((IFileStoragePhysical)scope.Storage).Physical;
        physical.WriteAllBytes(Path.Combine(physical.RootPath, ".lyo-health"), "h"u8.ToArray());
        physical.WriteAllBytes(Path.Combine(physical.RootPath, ".lyo-fs-health-write.tmp"), "p"u8.ToArray());
        physical.WriteAllBytes(Path.Combine(physical.RootPath, saved.Id.ToString("N") + ".meta"), "{}"u8.ToArray());
        physical.WriteAllBytes(Path.Combine(physical.RootPath, ".partial-upload"), "t"u8.ToArray());
        var metadata = new LocalFileMetadataStore(scope.Options.RootDirectoryPath);
        var rows = await FileStorageReconcile.ReconcileAsync(metadata, physical, ct: TestContext.Current.CancellationToken);
        Assert.DoesNotContain(rows, e => e.PhysicalKey != null && FileStorageReconcile.IsSkippedPhysicalKey(e.PhysicalKey));
        Assert.Contains(rows, e => e.FileId == saved.Id && e.Presence == FileStoragePresence.Both);
    }

    [Fact]
    public async Task FolderList_ImmediateFilesAndChildFolder()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var a = await scope.Storage.SaveFileAsync("hello"u8.ToArray(), "dup.txt", pathPrefix: "reports", ct: TestContext.Current.CancellationToken);
        await scope.Storage.SaveFileAsync("world"u8.ToArray(), "nested.pdf", pathPrefix: "reports/2024", ct: TestContext.Current.CancellationToken);
        var metadata = new LocalFileMetadataStore(scope.Options.RootDirectoryPath);
        var (entries, _) = await FileStorageFolderList.ListAsync(metadata, scope.Storage, "reports", ct: TestContext.Current.CancellationToken);
        Assert.Contains(entries, e => e is { IsDirectory: true, Name: "2024" });
        Assert.Contains(entries, e => e is { IsDirectory: false, FileId: var id, Presence: FileStoragePresence.Both } && id == a.Id);
    }

    [Fact]
    public async Task FolderList_PhysicalOnlyOrphan_AtRoot()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var physical = ((IFileStoragePhysical)scope.Storage).Physical;
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        physical.WriteAllBytes(Path.Combine(physical.RootPath, fileId.ToString("N") + ".bin"), "z"u8.ToArray());
        var metadata = new LocalFileMetadataStore(scope.Options.RootDirectoryPath);
        var (entries, _) = await FileStorageFolderList.ListAsync(metadata, physical, null, ct: TestContext.Current.CancellationToken);
        Assert.Contains(entries, e => e is { IsDirectory: false, Presence: FileStoragePresence.Physical, FileId: var id } && id == fileId);
    }

    [Fact]
    public async Task FolderList_StoreOnlyAfterPhysicalDelete_StaysStore()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var saved = await scope.Storage.SaveFileAsync("x"u8.ToArray(), "gone.txt", pathPrefix: "reports", ct: TestContext.Current.CancellationToken);
        var physical = ((IFileStoragePhysical)scope.Storage).Physical;
        var metadata = new LocalFileMetadataStore(scope.Options.RootDirectoryPath);
        var both = await FileStorageReconcile.ReconcileAsync(metadata, physical, pathPrefix: "reports", ct: TestContext.Current.CancellationToken);
        var key = Assert.Single(both, e => e.FileId == saved.Id).PhysicalKey;
        physical.DeleteFile(Path.Combine(physical.RootPath, key!.Replace('/', Path.DirectorySeparatorChar)));
        var (entries, _) = await FileStorageFolderList.ListAsync(metadata, physical, "reports", ct: TestContext.Current.CancellationToken);
        Assert.Contains(entries, e => e is { IsDirectory: false, FileId: var id, Presence: FileStoragePresence.Store } && id == saved.Id);
    }

    [Fact]
    public async Task FolderList_ChildDirectoryFromPhysicalOrphan_HasNullPhysicalKey()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var physical = ((IFileStoragePhysical)scope.Storage).Physical;
        physical.CreateDirectory(Path.Combine(physical.RootPath, "reports", "2024"));
        physical.WriteAllBytes(Path.Combine(physical.RootPath, "reports", "2024", "orphan.bin"), "z"u8.ToArray());
        var metadata = new LocalFileMetadataStore(scope.Options.RootDirectoryPath);
        var (entries, _) = await FileStorageFolderList.ListAsync(metadata, physical, "reports", ct: TestContext.Current.CancellationToken);
        var dir = Assert.Single(entries, e => e is { IsDirectory: true, Name: "2024" });
        Assert.Null(dir.PhysicalKey);
        Assert.Equal(FileStoragePresence.Physical, dir.Presence);
    }

    [Fact]
    public async Task PhysicalRead_OpensOrphanWithoutCatalog()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var physical = ((IFileStoragePhysical)scope.Storage).Physical;
        physical.WriteAllBytes(Path.Combine(physical.RootPath, "orphan.bin"), "hello"u8.ToArray());
        var (stream, fileName) = await FileStoragePhysicalRead.OpenAsync(scope.Storage, "orphan.bin", TestContext.Current.CancellationToken);
        await using (stream) {
            using var reader = new StreamReader(stream);
            Assert.Equal("hello", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
        }

        Assert.Equal("orphan.bin", fileName);
    }

    [Fact]
    public async Task PhysicalRead_RejectsTraversal()
    {
        using var scope = LocalFileStorageTestScope.Create();
        await Assert.ThrowsAsync<ArgumentException>(
            () => FileStoragePhysicalRead.OpenAsync(scope.Storage, "../etc/passwd", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PhysicalRead_MissingFile_Throws()
    {
        using var scope = LocalFileStorageTestScope.Create();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => FileStoragePhysicalRead.OpenAsync(scope.Storage, "nope.bin", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void PresenceFlags_StorePhysicalBoth()
    {
        Assert.Equal(0, (int)FileStoragePresence.None);
        Assert.Equal(1, (int)FileStoragePresence.Store);
        Assert.Equal(2, (int)FileStoragePresence.Physical);
        Assert.Equal(3, (int)FileStoragePresence.Both);
        Assert.Equal(FileStoragePresence.Both, FileStoragePresence.Store | FileStoragePresence.Physical);
    }
}
