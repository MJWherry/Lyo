using Lyo.Api.FileStorage.Models;
using Lyo.Common.Core.Identifiers;

namespace Lyo.FileManager.Tests;

public class FileManagerSessionTests
{
    [Fact]
    public void AddServer_ThenSelect_UsesThatBaseUrlAndRoute()
    {
        var session = Session(out var _);
        var added = session.AddRemote("home", "http://localhost:5000", "Workbench/FileStorage", "Workbench/FileStorage/FileMetadata");
        session.Select(added.Id);
        Assert.Equal("http://localhost:5000", session.Active!.BaseUrl);
        Assert.Equal("Workbench/FileStorage", session.Active.Route);
        Assert.Equal("Workbench/FileStorage/FileMetadata", session.Active.MetadataRouteOrDefault());
    }

    [Fact]
    public async Task Connect_HealthFailure_SetsError_KeepsTree()
    {
        var session = Session(out var api);
        var file = RecordingFileManagerApi.Stored("a.txt", null, LyoGuid.CreateV7());
        api.Folders[""] = new([file], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        Assert.Single(session.Entries);
        api.Health = new(false, "down");
        await session.ConnectAsync(TestCt);
        Assert.Equal("down", session.Error);
        Assert.Equal(file.FileId, session.Entries[0].FileId);
    }

    [Fact]
    public async Task Connect_StoresCapabilities()
    {
        var session = Session(out var api);
        api.Capabilities = new(true, false, true, false, true, false, true, false, true, false);
        api.Folders[""] = new([], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        Assert.True(session.Capabilities.PresignedRead);
        Assert.False(session.Capabilities.PresignedMultipartParts);
        Assert.True(session.Capabilities.Copy);
        Assert.False(session.Capabilities.Move);
        Assert.False(session.Capabilities.Encryption);
    }

    [Fact]
    public async Task Expand_Directory_ListsThatPrefix()
    {
        var session = Session(out var api);
        api.Folders[""] = new([RecordingFileManagerApi.Dir("docs", "docs")], false);
        api.Folders["docs"] = new([], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        await session.EnterAsync(session.Entries[0], TestCt);
        Assert.Equal("docs", session.Prefix);
        Assert.Equal("docs", api.Lists[^1]);
    }

    [Fact]
    public async Task Up_FromNestedPrefix_ReturnsParent()
    {
        var session = Session(out var api);
        api.Folders[""] = new([RecordingFileManagerApi.Dir("docs", "docs")], false);
        api.Folders["docs"] = new([RecordingFileManagerApi.Dir("a", "docs/a")], false);
        api.Folders["docs/a"] = new([], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        await session.EnterAsync(session.Entries[0], TestCt);
        await session.EnterAsync(session.Entries[0], TestCt);
        Assert.Equal("docs/a", session.Prefix);
        await session.UpAsync(TestCt);
        Assert.Equal("docs", session.Prefix);
        await session.UpAsync(TestCt);
        Assert.Null(session.Prefix);
    }

    [Fact]
    public async Task Rename_Move_Copy_Delete_UseTheSelectedFile_ThenReload()
    {
        var session = Session(out var api);
        var id = LyoGuid.CreateV7();
        var file = RecordingFileManagerApi.Stored("a.txt", "docs", id);
        api.Folders["docs"] = new([file], false);
        api.Folders[""] = new([RecordingFileManagerApi.Dir("docs", "docs")], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        await session.EnterAsync(session.Entries[0], TestCt);
        var lists = api.ListCount;
        await session.RenameAsync(file, "b.txt", TestCt);
        await session.MoveAsync([file], "archive", TestCt);
        await session.CopyAsync([file], "archive", TestCt);
        await session.DeleteAsync([file], TestCt);
        Assert.Equal((id, "b.txt"), api.Renames[0]);
        Assert.Equal((id, "archive"), api.Moves[0]);
        Assert.Equal((id, "archive"), api.Copies[0]);
        Assert.Equal(id, api.Deletes[0]);
        Assert.True(api.ListCount > lists);
    }

    [Fact]
    public async Task MoveFolder_RewritesDescendantPrefixes()
    {
        var session = ConnectedDocs(out var api, out var first, out var second);
        await session.MoveFolderAsync(RecordingFileManagerApi.Dir("docs", "docs"), "archive", TestCt);
        Assert.Equal((first, "archive/a"), api.Moves[0]);
        Assert.Equal((second, "archive/a/b"), api.Moves[1]);
    }

    [Fact]
    public async Task CopyFolder_PostsCopyPerDescendant_WithRewrittenPrefix()
    {
        var session = ConnectedDocs(out var api, out var first, out var second);
        await session.CopyFolderAsync(RecordingFileManagerApi.Dir("docs", "docs"), "archive", TestCt);
        Assert.Equal((first, "archive/a"), api.Copies[0]);
        Assert.Equal((second, "archive/a/b"), api.Copies[1]);
    }

    [Fact]
    public async Task DeleteFolder_DeletesEachCatalogFile()
    {
        var session = ConnectedDocs(out var api, out var first, out var second);
        await session.DeleteFolderAsync(RecordingFileManagerApi.Dir("docs", "docs"), TestCt);
        Assert.Equal([first, second], api.Deletes);
    }

    [Fact]
    public async Task Upload_SendsCompressEncryptAndKeyId()
    {
        var session = Session(out var api);
        api.Folders["docs"] = new([], false);
        api.Folders[""] = new([RecordingFileManagerApi.Dir("docs", "docs")], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        await session.EnterAsync(session.Entries[0], TestCt);
        await session.UploadAsync("note.txt", new() { Compress = true, Encrypt = true, KeyId = "kek" }, TestCt);
        Assert.Equal(("docs", true, true, "kek"), api.Uploads[0]);
    }

    [Fact]
    public async Task MoveFolder_PendingFolder_AppearsUnderTheDestination()
    {
        var session = Session(out var api);
        api.Folders[""] = new([], false);
        api.Folders["archive"] = new([], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        session.NewFolder("inbox");
        var inbox = session.Entries.Single(entry => entry.Pending);
        await session.MoveFolderAsync(inbox, "archive/inbox", TestCt);
        await session.OpenAsync("archive", TestCt);
        Assert.Contains(session.Entries, entry => entry.Pending && entry.PathPrefix == "archive/inbox" && entry.Name == "inbox");
        var listed = await session.ListAsync("archive", TestCt);
        Assert.Contains(listed.Entries, entry => entry.Pending && entry.PathPrefix == "archive/inbox");
    }

    [Fact]
    public async Task Upload_ExplicitPrefix_StoresThereWithoutOpeningIt()
    {
        var session = Session(out var api);
        api.Folders[""] = new([], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        await session.UploadAsync("note.txt", "archive", new(), TestCt);
        Assert.Equal(("archive", false, false, null), api.Uploads[0]);
        Assert.Null(session.Prefix);
    }

    [Fact]
    public async Task NewFolder_ShowsPendingPrefix_UntilRefreshWithNoUpload()
    {
        var session = Session(out var api);
        api.Folders[""] = new([], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        session.NewFolder("inbox");
        Assert.Contains(session.Entries, entry => entry.Pending && entry.PathPrefix == "inbox");
        await session.RefreshAsync(TestCt);
        Assert.DoesNotContain(session.Entries, entry => entry.Pending);
    }

    [Fact]
    public async Task List_Truncated_SetsFlag()
    {
        var session = Session(out var api);
        api.Folders[""] = new([], true);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        Assert.True(session.Truncated);
    }

    [Fact]
    public async Task PartialFailure_ReportsSucceededAndFailed()
    {
        var session = Session(out var api);
        var ok = LyoGuid.CreateV7();
        var bad = LyoGuid.CreateV7();
        api.FailMove = bad;
        api.Folders[""] = new([RecordingFileManagerApi.Stored("a", null, ok), RecordingFileManagerApi.Stored("b", null, bad)], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        var result = await session.MoveAsync(session.Entries.ToArray(), "gone", TestCt);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal("1 succeeded, 1 failed.", session.Error);
    }

    [Fact]
    public async Task PhysicalOnly_MoveRenameDelete_UsePhysicalRoutes()
    {
        var session = Session(out var api);
        var orphan = new FileManagerEntry("orphan.bin", false, null, null, FileStoragePresence.Physical, 1, "loose/orphan.bin");
        api.Folders[""] = new([orphan], false);
        session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        await session.MovePhysicalAsync([orphan], "kept", TestCt);
        await session.RenameAsync(orphan, "renamed.bin", TestCt);
        await session.DeletePhysicalAsync([orphan], TestCt);
        Assert.Equal(("loose/orphan.bin", "kept"), api.PhysicalMoves[0]);
        Assert.Equal(("loose/orphan.bin", "renamed.bin"), api.PhysicalRenames[0]);
        Assert.Equal("loose/orphan.bin", api.PhysicalDeletes[0]);
    }

    [Fact]
    public void FolderMatch_TypingT_SuggestsDirsThatStartWithT()
    {
        Assert.True(FileManagerFolders.Matches("ttt", "t"));
        Assert.True(FileManagerFolders.Matches("tt", "t"));
        Assert.True(FileManagerFolders.Matches("docs/ttt", "t"));
        Assert.False(FileManagerFolders.Matches("docs", "t"));
        Assert.True(FileManagerFolders.Matches("docs/archive", "docs/a"));
    }

    [Fact]
    public async Task FolderPrefixes_WalksDirectories()
    {
        var session = Session(out var api);
        api.Folders[""] = new(
            [RecordingFileManagerApi.Dir("ttt", "ttt"), RecordingFileManagerApi.Dir("docs", "docs"), RecordingFileManagerApi.Stored("note.txt", null, LyoGuid.CreateV7())], false);

        api.Folders["docs"] = new([RecordingFileManagerApi.Dir("archive", "docs/archive")], false);
        api.Folders["ttt"] = new([], false);
        api.Folders["docs/archive"] = new([], false);
        var server = session.AddRemote("home", "http://localhost");
        await session.ConnectAsync(TestCt);
        var folders = await session.FolderPrefixesAsync(server.Id, TestCt);
        Assert.Contains("ttt", folders);
        Assert.Contains("docs", folders);
        Assert.Contains("docs/archive", folders);
        Assert.DoesNotContain("note.txt", folders);
    }

    private static FileManagerSession ConnectedDocs(out RecordingFileManagerApi api, out Guid first, out Guid second)
    {
        var session = Session(out api);
        first = LyoGuid.CreateV7();
        second = LyoGuid.CreateV7();
        api.Folders[""] = new([RecordingFileManagerApi.Dir("docs", "docs")], false);
        api.Folders["docs"] = new([RecordingFileManagerApi.Dir("a", "docs/a")], false);
        api.Folders["docs/a"] = new([RecordingFileManagerApi.Stored("one", "docs/a", first), RecordingFileManagerApi.Dir("b", "docs/a/b")], false);
        api.Folders["docs/a/b"] = new([RecordingFileManagerApi.Stored("two", "docs/a/b", second)], false);
        session.AddRemote("home", "http://localhost");
        session.ConnectAsync(TestCt).GetAwaiter().GetResult();
        return session;
    }

    private static FileManagerSession Session(out RecordingFileManagerApi api)
    {
        var recorded = new RecordingFileManagerApi();
        api = recorded;
        return new(_ => recorded);
    }
}