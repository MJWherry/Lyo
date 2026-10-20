using Lyo.IO.Temp.Models;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileManager.Tests;

public class FileManagerConfiguredHostTests
{
    [Fact]
    public async Task Open_TwoDisks_DisposingOneScopeLeavesTheOtherUsable()
    {
        using var temp = new IOTempSession(new() { RootDirectory = Path.GetTempPath() });
        var rootA = temp.CreateDirectory("a");
        var rootB = temp.CreateDirectory("b");
        var sourceA = temp.CreateFile("from-a");
        var sourceB = temp.CreateFile("from-b");
        using var host = FileManagerConfiguredHost.Load(Config(
            ("a", rootA),
            ("b", rootB)));
        var left = host.Open(Server(host, "a"));
        var right = host.Open(Server(host, "b"));
        await left.UploadAsync(sourceA, null, new(), TestCt);
        await right.UploadAsync(sourceB, null, new(), TestCt);
        left.Dispose();

        var listed = await right.ListAsync(null, TestCt);
        Assert.Contains(listed.Entries, entry => entry.Name == Path.GetFileName(sourceB));
        Assert.DoesNotContain(listed.Entries, entry => entry.Name == Path.GetFileName(sourceA));
        right.Dispose();
    }

    [Fact]
    public async Task Load_CloudWithoutCatalog_SkipsThatStoreAndOpensTheOthers()
    {
        using var temp = new IOTempSession(new IOTempSessionOptions { RootDirectory = Path.GetTempPath() });
        var root = temp.CreateDirectory("disk");
        var values = new Dictionary<string, string?> {
            ["FileStorage:Stores:disk:Provider"] = "Disk",
            ["FileStorage:Stores:disk:RootDirectoryPath"] = root,
            ["FileStorage:Stores:cloud:Provider"] = "S3",
            ["FileStorage:Stores:cloud:BucketName"] = "bucket"
        };
        using var host = FileManagerConfiguredHost.Load(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        Assert.Equal("disk", Assert.Single(host.Servers).Name);
        Assert.Contains(host.Skipped, note => note.Contains("cloud") && note.Contains("CatalogConnectionString"));
        using var opened = host.Open(host.Servers[0]);
        var listed = await opened.ListAsync(null, TestCt);
        Assert.NotNull(listed);
    }

    [Fact]
    public void Load_MetadataStoreKey_SkipsThatStore()
    {
        using var temp = new IOTempSession(new IOTempSessionOptions { RootDirectory = Path.GetTempPath() });
        var root = temp.CreateDirectory("open");
        var locked = temp.CreateDirectory("locked");
        var values = new Dictionary<string, string?> {
            ["FileStorage:Stores:open:Provider"] = "Disk",
            ["FileStorage:Stores:open:RootDirectoryPath"] = root,
            ["FileStorage:Stores:locked:Provider"] = "Disk",
            ["FileStorage:Stores:locked:RootDirectoryPath"] = locked,
            ["FileStorage:Stores:locked:MetadataStoreKey"] = "other"
        };
        using var host = FileManagerConfiguredHost.Load(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        Assert.Equal("open", Assert.Single(host.Servers).Name);
        Assert.Contains(host.Skipped, note => note.Contains("locked") && note.Contains("MetadataStoreKey") && note.Contains("other"));
    }

    [Fact]
    public void Add_SameNameAsConfiguredStore_Throws()
    {
        using var temp = new IOTempSession(new IOTempSessionOptions { RootDirectory = Path.GetTempPath() });
        var root = temp.CreateDirectory("photos");
        using var configured = FileManagerConfiguredHost.Load(Config(("photos", root)));
        var session = new FileManagerSession();
        session.AddLocal("photos", root, FileManagerMetadataKind.Sqlite);
        var error = Assert.Throws<InvalidOperationException>(() => session.UseConfiguredStores(configured));
        Assert.Contains("photos", error.Message);

        var dialog = new FileManagerSession();
        dialog.UseConfiguredStores(configured);
        var added = Assert.Throws<InvalidOperationException>(() => dialog.Add(new FileManagerServer { Name = "Photos", Kind = FileManagerServerKind.Local, RootDirectory = root }));
        Assert.Contains("Photos", added.Message);
    }

    private static FileManagerServer Server(FileManagerConfiguredHost host, string name)
        => host.Servers.Single(server => server.Name == name);

    private static IConfiguration Config(params (string Name, string Root)[] stores)
    {
        var values = new Dictionary<string, string?>();
        foreach (var (name, root) in stores) {
            values[$"FileStorage:Stores:{name}:Provider"] = "Disk";
            values[$"FileStorage:Stores:{name}:RootDirectoryPath"] = root;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
