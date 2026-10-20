using Lyo.Api.FileStorage.Models;
using FileStorageManagementComponent = Lyo.FileStorage.Web.Components.FileStorageManagement.FileStorageManagement;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStorageStoreRoutesTests
{
    [Fact]
    public void Prefix_SelectedStore_ChangesTheFileRouteAndLeavesMetadataAlone()
    {
        var stores = new FileStorageStoreResponse[] { new("a", "FileStorage/a", null), new("b", "FileStorage/b", null) };
        var component = new FileStorageManagementComponent { FileMetadataQueryRoute = "FileStorage/FileMetadata", FileStorageApiRoutePrefix = "FileStorage" };
        component.UseStores(stores);
        component.SelectStore("b");

        Assert.Equal("FileStorage/b/files/folder", component.FilesApi("files/folder"));
        Assert.Equal("FileStorage/b/health", component.FilesApi("health"));
        Assert.StartsWith("FileStorage/b/files/save-stream", component.BuildSaveStreamUri(null, false, false, null, null, null));
        Assert.Equal("FileStorage/FileMetadata", component.FileMetadataQueryRoute);
    }

    [Fact]
    public void Prefix_OneStore_UsesThatRoute()
    {
        var component = new FileStorageManagementComponent { FileStorageApiRoutePrefix = "FileStorage" };
        component.UseStores([new("only", "FileStorage/only", null)]);
        Assert.Equal("FileStorage/only/files/folder", component.FilesApi("files/folder"));
    }

    [Fact]
    public void Prefix_FailedList_LeavesTheParameterPrefix()
    {
        var component = new FileStorageManagementComponent { FileStorageApiRoutePrefix = "FileStorage", StreamUploadRelativePath = "upload/file" };
        component.UseStores(null);
        Assert.Equal("FileStorage/files/folder", component.FilesApi("files/folder"));
        Assert.StartsWith("upload/file", component.BuildSaveStreamUri(null, false, false, null, null, null));
    }
}
