using Lyo.Api.FileStorage.Models;
using Lyo.Common.Core.Identifiers;

namespace Lyo.FileManager.Tests;

public class FileManagerWindowTests
{
    [Fact]
    public void DragPress_OnSelectedRow_KeepsTheOtherRows()
    {
        Assert.True(FileManagerDragSelection.Keeps(3, pressedIsSelected: true));
        Assert.False(FileManagerDragSelection.Keeps(3, pressedIsSelected: false));
        Assert.False(FileManagerDragSelection.Keeps(1, pressedIsSelected: true));
    }

    [Fact]
    public void AddServer_BlankName_ReturnsThatError()
    {
        Assert.Equal("Name is required.", FileManagerServerCheck.Validate(" ", FileManagerServerKind.Remote, "http://localhost", null));
        Assert.Equal("File folder is required.", FileManagerServerCheck.Validate("local", FileManagerServerKind.Local, null, " "));
        Assert.Equal("Base URL is required.", FileManagerServerCheck.Validate("remote", FileManagerServerKind.Remote, " ", null));
        Assert.Null(FileManagerServerCheck.Validate("remote", FileManagerServerKind.Remote, "http://localhost", null));
        Assert.Equal(
            "Bucket is required.",
            FileManagerServerCheck.ValidateStore(FileManagerServerKind.S3, " ", null, null, null, null, null, null, null, "/tmp/catalog.db", false));
        Assert.Equal(
            "Catalog database is required.",
            FileManagerServerCheck.ValidateStore(FileManagerServerKind.S3, "bucket", null, null, null, null, null, null, null, " ", false));
        Assert.Null(FileManagerServerCheck.ValidateStore(FileManagerServerKind.S3, "bucket", null, null, null, null, null, null, null, "/tmp/catalog.db", false));
        Assert.Equal(
            "Host key fingerprint is required.",
            FileManagerServerCheck.ValidateStore(FileManagerServerKind.Sftp, null, null, null, "files", "user", "secret", null, " ", "/tmp/catalog.db", false));
    }

    [Fact]
    public void SqliteCatalog_KeepsTheDirectoryAndTheFileName()
    {
        Assert.Equal(Path.GetDirectoryName(Path.Combine("/data", "photos.file-metadata.db")), FileManagerPaths.DefaultSqliteDirectory("/data/photos"));
        Assert.Equal("photos.file-metadata.db", FileManagerPaths.DefaultSqliteFileName("/data/photos"));
        Assert.Equal(Path.Combine("/var/catalog", "comic.db"), FileManagerPaths.CombineSqliteFile(" /var/catalog ", " comic.db "));
    }

    [Fact]
    public void DefaultSqlitePath_IsBesideTheFileFolder()
    {
        Assert.Equal(Path.Combine("/data", "photos.file-metadata.db"), FileManagerPaths.DefaultSqliteFile("/data/photos"));
        Assert.False(string.IsNullOrWhiteSpace(FileManagerPaths.RejectDatabaseInsideRoot("/data/photos", "/data/photos/file-metadata.db")));
        Assert.False(string.IsNullOrWhiteSpace(FileManagerPaths.RejectDatabaseInsideRoot("/data/photos", "Data Source=/data/photos/meta.db")));
        Assert.Null(FileManagerPaths.RejectDatabaseInsideRoot("/data/photos", "/data/photos.file-metadata.db"));
    }

    [Fact]
    public async Task NewFolder_EnterThenUp_StillShowsThePendingFolder()
    {
        var session = new FileManagerSession(_ => new RecordingFileManagerApi());
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        session.NewFolder("inbox");
        var inbox = session.Entries.Single(entry => entry.Pending);
        await session.EnterAsync(inbox, TestCt);
        await session.UpAsync(TestCt);
        Assert.Contains(session.Entries, entry => entry.Pending && entry.PathPrefix == "inbox");
        await session.RefreshAsync(TestCt);
        Assert.DoesNotContain(session.Entries, entry => entry.Pending);
    }

    [Fact]
    public async Task Upload_EncryptWithoutKeyOrPassphrase_ThrowsNoKeyConfigured()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        var source = Path.Combine(Path.GetTempPath(), "lyo-fm-src-" + LyoGuid.CreateV7().ToString("N") + ".txt");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(source, "hello", TestCt);
        var database = FileManagerPaths.DefaultSqliteFile(root);
        try {
            var server = new FileManagerServer { Kind = FileManagerServerKind.Local, Name = "local", RootDirectory = root, MetadataKind = FileManagerMetadataKind.Sqlite };
            using var host = FileStorageLocalHost.Open(server);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.UploadAsync(source, "docs", new() { Encrypt = true }, TestCt));
            Assert.Equal("No key is configured.", error.Message);
        }
        finally {
            if (File.Exists(source))
                File.Delete(source);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Drop_FileOntoFolder_UsesThatPrefix()
    {
        var file = RecordingFileManagerApi.Stored("note.txt", "docs", Guid.NewGuid());
        var physical = new FileManagerEntry("scan.bin", false, "docs", null, FileStoragePresence.Physical, 4, "docs/scan.bin");
        var plan = FileManagerDrop.Plan([file, physical], FileManagerDrop.Target(RecordingFileManagerApi.Dir("archive", "archive"), "docs"));
        Assert.Equal("archive", plan.Destination);
        Assert.Equal(file, Assert.Single(plan.Files));
        Assert.Equal(physical, Assert.Single(plan.Physical));
        Assert.Empty(plan.Folders);
    }

    [Fact]
    public void Drop_FolderOntoFolder_NestsUnderTheTarget()
    {
        var folder = RecordingFileManagerApi.Dir("inbox", "docs/inbox");
        var plan = FileManagerDrop.Plan([folder], "archive");
        Assert.Equal("archive", plan.Destination);
        Assert.Equal(folder, Assert.Single(plan.Folders));
        Assert.Equal("archive/inbox", plan.FolderPrefix(folder));
    }

    [Fact]
    public void Drop_FolderOntoItselfOrChild_IsRejected()
    {
        var folder = RecordingFileManagerApi.Dir("docs", "docs");
        var ontoSelf = FileManagerDrop.Plan([folder], "docs");
        var ontoChild = FileManagerDrop.Plan([folder], "docs/a");
        Assert.Empty(ontoSelf.Folders);
        Assert.Equal("docs", Assert.Single(ontoSelf.Rejected));
        Assert.Empty(ontoChild.Folders);
        Assert.Equal("docs", Assert.Single(ontoChild.Rejected));
    }

    [Fact]
    public void Drop_OntoFile_UsesThatFilesFolder()
    {
        var hovered = RecordingFileManagerApi.Stored("note.txt", "inbox", Guid.NewGuid());
        var dragged = RecordingFileManagerApi.Stored("other.txt", "docs", Guid.NewGuid());
        var plan = FileManagerDrop.Plan([dragged], FileManagerDrop.Target(hovered, "docs"));
        Assert.Equal("inbox", plan.Destination);
        Assert.Equal(dragged, Assert.Single(plan.Files));
        var already = FileManagerDrop.Plan([hovered], "inbox");
        Assert.Empty(already.Files);
        Assert.Empty(already.Rejected);
    }

    [Fact]
    public void Log_WritesTheLineToTheFileAndConsole()
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-log-" + LyoGuid.CreateV7().ToString("N") + ".log");
        var console = new StringWriter();
        var previous = Console.Error;
        Console.SetError(console);
        try {
            FileManagerLog.Write(path, "connect failed");
            Assert.Contains("connect failed", File.ReadAllText(path), StringComparison.Ordinal);
            Assert.Contains("connect failed", console.ToString(), StringComparison.Ordinal);
        }
        finally {
            Console.SetError(previous);
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TreeFilter_HidesNonMatchingBranches()
    {
        var root = new FolderTreeNode {
            Name = "/",
            Children = [
                new() { Name = "photos" },
                new() { Name = "reports" }
            ]
        };
        var filtered = FolderTreeFilter.Apply([root], "rep");
        var kept = Assert.Single(filtered);
        Assert.Equal("/", kept.Name);
        var child = Assert.Single(kept.Children);
        Assert.Equal("reports", child.Name);
        var restored = FolderTreeFilter.Apply([root], "");
        Assert.Equal(2, Assert.Single(restored).Children.Count);
    }

    [Fact]
    public void Layout_RoundTripsDock()
    {
        var layout = FileManagerLayout.CreateDefault();
        Assert.Equal(FileManagerDockEdge.Left, layout.Edge(FileManagerTool.Tree));
        Assert.Equal(FileManagerDockEdge.Right, layout.Edge(FileManagerTool.Metadata));
        Assert.Equal(FileManagerDockEdge.Bottom, layout.Edge(FileManagerTool.Log));
        Assert.False(layout.IsVisible(FileManagerTool.Log));
        Assert.Equal(3, layout.Tools.Count);

        layout.Move(FileManagerTool.Log, FileManagerDockEdge.Right);
        Assert.Equal(FileManagerTool.Log, layout.Window(FileManagerTool.Log).Tool);
        Assert.Equal(FileManagerDockEdge.Right, layout.Edge(FileManagerTool.Log));
        Assert.Equal(3, layout.Tools.Count);

        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-settings-" + LyoGuid.CreateV7().ToString("N") + ".json");
        try {
            FileManagerSettingsStore.Save(path, [new FileManagerServer { Name = "home", BaseUrl = "http://localhost" }], layout);
            var loaded = FileManagerSettingsStore.LoadDocument(path);
            Assert.Equal("home", loaded.Servers[0].Name);
            Assert.Equal(FileManagerDockEdge.Right, loaded.Layout.Edge(FileManagerTool.Log));
            Assert.Equal(3, loaded.Layout.Tools.Count);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
