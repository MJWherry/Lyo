using Lyo.Common.Core.Identifiers;
using Lyo.FileStorage;

namespace Lyo.FileManager.Tests;

public class FileManagerConnectionTests
{
    [Fact]
    public async Task Connect_SecondServer_LeavesTheFirstConnectionOpen()
    {
        var one = new RecordingFileManagerApi();
        var two = new RecordingFileManagerApi();
        var session = new FileManagerSession(server => server.Name == "one" ? one : two);
        var first = session.AddRemote("one", "http://one");
        var second = session.AddRemote("two", "http://two");
        one.Folders[""] = new([RecordingFileManagerApi.Stored("from-one", null, LyoGuid.CreateV7())], false);
        two.Folders[""] = new([RecordingFileManagerApi.Stored("from-two", null, LyoGuid.CreateV7())], false);
        session.Select(first.Id);
        await session.ConnectAsync(TestCt);
        Assert.Equal("from-one", session.Entries[0].Name);
        session.Select(second.Id);
        await session.ConnectAsync(TestCt);
        Assert.Equal("from-two", session.Entries[0].Name);
        Assert.Equal(2, session.Connections.Count);
        session.Select(first.Id);
        Assert.Equal("from-one", session.Entries[0].Name);
        Assert.Equal(first.Id, session.Active!.Id);
    }

    [Fact]
    public async Task Transfer_Copy_CopiesOnlyTheSelectedFiles_SourceStaysReadable()
    {
        var sourceRoot = TempDir();
        var destRoot = TempDir();
        var keepPath = TempFile("keep-me");
        var otherPath = TempFile("leave-me");
        var download = TempFile("out");
        try {
            using var session = new FileManagerSession();
            var source = session.AddLocal("source", sourceRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            await session.UploadAsync(keepPath, new(), TestCt);
            await session.UploadAsync(otherPath, new(), TestCt);
            var keep = session.Entries.Single(entry => entry.Name == Path.GetFileName(keepPath));
            var other = session.Entries.Single(entry => entry.Name == Path.GetFileName(otherPath));
            var destination = session.AddLocal("dest", destRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            session.Select(source.Id);
            var result = await session.TransferAsync(destination.Id, [keep], FileStorageTransferDisposition.Copy, ct: TestCt);
            Assert.Equal(1, result.FilesCopied);
            Assert.Equal(0, result.FilesRemoved);
            Assert.Equal(0, result.FilesFailed);
            session.Select(source.Id);
            await session.RefreshAsync(TestCt);
            Assert.Contains(session.Entries, entry => entry.FileId == keep.FileId);
            Assert.Contains(session.Entries, entry => entry.FileId == other.FileId);
            session.Select(destination.Id);
            await session.RefreshAsync(TestCt);
            Assert.Contains(session.Entries, entry => entry.FileId == keep.FileId);
            Assert.DoesNotContain(session.Entries, entry => entry.FileId == other.FileId);
            var copied = session.Entries.Single(entry => entry.FileId == keep.FileId);
            await session.DownloadAsync(copied, download, TestCt);
            Assert.Equal("keep-me", await File.ReadAllTextAsync(download, TestCt));
        }
        finally {
            DeleteTree(sourceRoot);
            DeleteTree(destRoot);
            DeleteFile(keepPath);
            DeleteFile(otherPath);
            DeleteFile(download);
        }
    }

    [Fact]
    public async Task Transfer_Copy_TargetPrefix_LandsInThatFolder()
    {
        var sourceRoot = TempDir();
        var destRoot = TempDir();
        var payload = TempFile("moved-into-inbox");
        var download = TempFile("inbox-out");
        try {
            using var session = new FileManagerSession();
            var source = session.AddLocal("source", sourceRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            await session.UploadAsync(payload, new(), TestCt);
            var file = session.Entries.Single(entry => entry.FileId != null);
            var destination = session.AddLocal("dest", destRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            session.Select(source.Id);
            await session.TransferAsync(destination.Id, [file], FileStorageTransferDisposition.Copy, destinationPrefix: "inbox", ct: TestCt);
            session.Select(destination.Id);
            await session.OpenAsync("inbox", TestCt);
            var copied = session.Entries.Single(entry => entry.FileId == file.FileId);
            Assert.Equal("inbox", copied.PathPrefix);
            await session.DownloadAsync(copied, download, TestCt);
            Assert.Equal("moved-into-inbox", await File.ReadAllTextAsync(download, TestCt));
        }
        finally {
            DeleteTree(sourceRoot);
            DeleteTree(destRoot);
            DeleteFile(payload);
            DeleteFile(download);
        }
    }

    [Fact]
    public async Task Transfer_Move_AfterHashMatch_RemovesSource()
    {
        var sourceRoot = TempDir();
        var destRoot = TempDir();
        var payload = TempFile("moved-body");
        var download = TempFile("moved-out");
        try {
            using var session = new FileManagerSession();
            var source = session.AddLocal("source", sourceRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            await session.UploadAsync(payload, new(), TestCt);
            var file = session.Entries.Single(entry => entry.FileId != null);
            var destination = session.AddLocal("dest", destRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            session.Select(source.Id);
            var result = await session.TransferAsync(destination.Id, [file], FileStorageTransferDisposition.Move, ct: TestCt);
            Assert.Equal(1, result.FilesCopied);
            Assert.Equal(1, result.FilesRemoved);
            session.Select(source.Id);
            await session.RefreshAsync(TestCt);
            Assert.DoesNotContain(session.Entries, entry => entry.FileId == file.FileId);
            session.Select(destination.Id);
            await session.RefreshAsync(TestCt);
            var copied = session.Entries.Single(entry => entry.FileId == file.FileId);
            await session.DownloadAsync(copied, download, TestCt);
            Assert.Equal("moved-body", await File.ReadAllTextAsync(download, TestCt));
        }
        finally {
            DeleteTree(sourceRoot);
            DeleteTree(destRoot);
            DeleteFile(payload);
            DeleteFile(download);
        }
    }

    [Fact]
    public async Task Transfer_RemoteDestination_ThrowsBeforeWrites()
    {
        var sourceRoot = TempDir();
        var payload = TempFile("stay");
        try {
            var session = new FileManagerSession(server => server.Kind == FileManagerServerKind.Local ? FileStorageLocalHost.Open(server) : new RecordingFileManagerApi());
            var source = session.AddLocal("source", sourceRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            await session.UploadAsync(payload, new(), TestCt);
            var file = session.Entries.Single(entry => entry.FileId != null);
            var remote = session.AddRemote("remote", "http://localhost");
            await session.ConnectAsync(TestCt);
            session.Select(source.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.TransferAsync(remote.Id, [file], FileStorageTransferDisposition.Move, ct: TestCt));
            await session.RefreshAsync(TestCt);
            Assert.Contains(session.Entries, entry => entry.FileId == file.FileId);
        }
        finally {
            DeleteTree(sourceRoot);
            DeleteFile(payload);
        }
    }

    [Fact]
    public async Task Connect_SameServerDifferentKeyStore_KeepsBothOpen()
    {
        var comic = new FileManagerKeyStore { Name = "comic" };
        var desk = new FileManagerKeyStore { Name = "desk" };
        var session = new FileManagerSession(server => {
            var api = new RecordingFileManagerApi();
            var label = server.KeyStoreId == comic.Id ? "from-comic" : "from-desk";
            api.Folders[""] = new([RecordingFileManagerApi.Stored(label, null, LyoGuid.CreateV7())], false);
            return api;
        });

        session.ReplaceKeyStores([comic, desk]);
        var server = session.AddLocal("photos", Path.Combine(Path.GetTempPath(), "lyo-fm-tabs"), FileManagerMetadataKind.Sqlite);
        server.KeyStoreId = comic.Id;
        await session.ConnectAsync(TestCt);
        Assert.Equal("from-comic", Assert.Single(session.Entries).Name);
        Assert.Equal("comic + photos", Assert.Single(session.Connections).Title);
        server.KeyStoreId = desk.Id;
        await session.ConnectAsync(TestCt);
        Assert.Equal(2, session.Connections.Count);
        Assert.Equal("from-desk", Assert.Single(session.Entries).Name);
        Assert.Equal("desk + photos", session.Connections[1].Title);
        server.KeyStoreId = comic.Id;
        session.Select(server.Id);
        Assert.Equal("from-comic", Assert.Single(session.Entries).Name);
        Assert.Equal(2, session.Connections.Count);
        await session.ConnectAsync(TestCt);
        Assert.Equal(2, session.Connections.Count);
        session.CloseConnection(server.Id, comic.Id);
        Assert.Equal(desk.Id, Assert.Single(session.Connections).KeyStoreId);
        Assert.Equal("from-desk", Assert.Single(session.Entries).Name);
    }

    [Fact]
    public async Task Transfer_SameConnection_ThrowsBeforeWrites()
    {
        var sourceRoot = TempDir();
        var payload = TempFile("same");
        try {
            using var session = new FileManagerSession();
            var source = session.AddLocal("source", sourceRoot, FileManagerMetadataKind.Sqlite);
            await session.ConnectAsync(TestCt);
            await session.UploadAsync(payload, new(), TestCt);
            var file = session.Entries.Single(entry => entry.FileId != null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.TransferAsync(source.Id, [file], FileStorageTransferDisposition.Move, ct: TestCt));
            await session.RefreshAsync(TestCt);
            Assert.Contains(session.Entries, entry => entry.FileId == file.FileId);
        }
        finally {
            DeleteTree(sourceRoot);
            DeleteFile(payload);
        }
    }

    private static string TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string TempFile(string text)
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N") + ".txt");
        File.WriteAllText(path, text);
        return path;
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void DeleteTree(string root)
    {
        var database = FileManagerPaths.DefaultSqliteFile(root);
        if (File.Exists(database))
            File.Delete(database);

        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }
}