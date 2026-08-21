namespace Lyo.Api.FileStorage.Tests;

public sealed class FileStorageApiOptionsTests
{
    [Fact]
    public void Defaults_MatchWorkbenchRoutes()
    {
        var options = new FileStorageApiOptions();
        Assert.Equal("Workbench/FileStorage", options.Route);
        Assert.Equal("gateway-filestorage", options.ServiceKey);
        Assert.Equal("Workbench/FileStorage/FileMetadata", options.FileMetadataRoute);
        Assert.Equal("upload/file", options.DirectUploadPath);
    }

    [Fact]
    public void Validate_BlankRoute_Throws()
    {
        var options = new FileStorageApiOptions { Route = " " };
        Assert.ThrowsAny<ArgumentException>(options.Validate);
    }
}
