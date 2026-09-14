using Azure.Storage.Blobs;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions.Models;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.AzureBlob.Tests;

public class AzureBlobFileSystemTests
{
    [Fact]
    public void PathStyle_WatchNull_AppendThrows_AndJail()
    {
        var container = new BlobContainerClient("UseDevelopmentStorage=true", "test");
        using var fs = new AzureBlobFileSystem(container, "app-data");
        Assert.Equal(PathStyle.Posix, fs.PathStyle);
        Assert.Equal("/app-data", fs.RootPath);
        Assert.Null(fs.Watch(fs.RootPath));
        Assert.Throws<NotSupportedException>(() => fs.OpenAppend("/app-data/a.bin"));
        Assert.Throws<InvalidFormatException>(() => fs.DirectoryExists("/outside"));
    }
}
