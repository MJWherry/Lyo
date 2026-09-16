using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageServiceBaseOptionsTests
{
    [Fact]
    public void ValidateUploadFilters_BothFileNameLists_Throws()
    {
        var options = Options();
        options.AllowedFileNamePatterns = [".*"];
        options.DeniedFileNamePatterns = [".*"];
        Assert.Throws<ArgumentException>(options.ValidateUploadFilters);
    }

    [Fact]
    public void ValidateUploadFilters_BothContentTypeLists_Throws()
    {
        var options = Options();
        options.AllowedContentTypes = ["application/pdf"];
        options.DeniedContentTypes = ["application/java-archive"];
        Assert.Throws<ArgumentException>(options.ValidateUploadFilters);
    }

    [Fact]
    public void ValidateUploadFilters_BothSniffedTypeLists_Throws()
    {
        var options = Options();
        options.AllowedSniffedFileTypes = ["PDF"];
        options.DeniedSniffedFileTypes = ["ZIP"];
        Assert.Throws<ArgumentException>(options.ValidateUploadFilters);
    }

    [Fact]
    public void ValidateUploadFilters_OneSideOfEachPair_DoesNotThrow()
    {
        var options = Options();
        options.AllowedContentTypes = ["application/pdf"];
        options.DeniedFileNamePatterns = ["\\.exe$"];
        options.AllowedSniffedFileTypes = ["PDF"];
        options.ValidateUploadFilters();
    }

    [Fact]
    public void ValidateUploadFilters_InvalidFileNamePattern_Throws()
    {
        var options = Options();
        options.AllowedFileNamePatterns = ["("];
        Assert.Throws<ArgumentException>(options.ValidateUploadFilters);
    }

    [Fact]
    public void ValidateUploadFilters_UnknownSniffedTypeName_Throws()
    {
        var options = Options();
        options.DeniedSniffedFileTypes = ["NotARealType"];
        Assert.Throws<ArgumentException>(options.ValidateUploadFilters);
    }

    private static DiskFileStorageOptions Options() => new() { RootDirectoryPath = "unused" };
}
