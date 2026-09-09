using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class FileTypeInfoRegistryTests
{
    [Fact]
    public void StreamCompressionAlgorithmDefaultExtensions_MatchesCompressionConstantsCardinality()
    {
        Assert.Equal(10, FileTypeInfo.StreamCompressionAlgorithmDefaultExtensions.Count);
        Assert.Contains(FileTypeInfo.Gz.DefaultExtension, FileTypeInfo.StreamCompressionAlgorithmDefaultExtensions);
        Assert.Contains(FileTypeInfo.LZMAStream.DefaultExtension, FileTypeInfo.StreamCompressionAlgorithmDefaultExtensions);
    }

    [Fact]
    public void CommonStorageResolutionSuffixes_IncludesCompressionAndEncryption()
    {
        Assert.Contains(FileTypeInfo.Brotli.DefaultExtension, FileTypeInfo.CommonStorageResolutionSuffixes);
        Assert.Contains(FileTypeInfo.LyoAesGcm.DefaultExtension, FileTypeInfo.CommonStorageResolutionSuffixes);
        Assert.Contains(FileTypeInfo.LyoTwoKeyEnvelope.DefaultExtension, FileTypeInfo.CommonStorageResolutionSuffixes);
    }

    [Fact]
    public void FileTypeInfo_FromExtension_LyoAesGcm_Resolves()
    {
        var t = FileTypeInfo.FromExtension(FileTypeInfo.LyoAesGcm.DefaultExtension);
        Assert.Same(FileTypeInfo.LyoAesGcm, t);
        Assert.Equal(FileTypeCategory.Encrypted, t.Category);
    }

    [Fact]
    public void FileTypeInfo_FromExtension_LzmaStream_Resolves()
    {
        var t = FileTypeInfo.FromExtension(FileTypeInfo.LZMAStream.DefaultExtension);
        Assert.Same(FileTypeInfo.LZMAStream, t);
        Assert.Equal(FileTypeCategory.Compressed, t.Category);
    }

    [Fact]
    public void FileTypeInfo_FromExtension_LyoTwoKeyEnvelope_MapsMultipleExtensions()
    {
        Assert.Same(FileTypeInfo.LyoTwoKeyEnvelope, FileTypeInfo.FromExtension(FileTypeInfo.LyoTwoKeyEnvelope.DefaultExtension));
        Assert.Same(FileTypeInfo.LyoTwoKeyEnvelope, FileTypeInfo.FromExtension(FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix));
    }

    [Fact]
    public void FileTypeInfo_FromMimeType_LyoCiphertextAesGcm_Resolves()
    {
        var t = FileTypeInfo.FromMimeType(FileTypeInfo.LyoAesGcm.MimeType);
        Assert.Same(FileTypeInfo.LyoAesGcm, t);
    }

    [Fact]
    public void FileTypeInfo_Ico_ByExtension_And_LegacyMime_Resolve()
    {
        Assert.Same(FileTypeInfo.Ico, FileTypeInfo.FromExtension(".ico"));
        Assert.Same(FileTypeInfo.Ico, FileTypeInfo.FromMimeType(FileTypeInfo.Ico.MimeType));
        Assert.Same(FileTypeInfo.Ico, FileTypeInfo.FromMimeType("image/x-icon"));
    }

    [Fact]
    public void FileTypeInfo_FromMimeType_CommonAliases_Resolve()
    {
        Assert.Same(FileTypeInfo.Xml, FileTypeInfo.FromMimeType("text/xml"));
        Assert.Same(FileTypeInfo.Zip, FileTypeInfo.FromMimeType("application/x-zip-compressed"));
        Assert.Same(FileTypeInfo.Json, FileTypeInfo.FromMimeType("text/json"));
        Assert.Same(FileTypeInfo.Jpeg, FileTypeInfo.FromMimeType("image/jpg"));
        Assert.Same(FileTypeInfo.Wav, FileTypeInfo.FromMimeType("audio/x-wav"));
        Assert.Same(FileTypeInfo.Gz, FileTypeInfo.FromMimeType("application/x-gzip"));
        Assert.Same(FileTypeInfo.Rar, FileTypeInfo.FromMimeType("application/vnd.rar"));
        Assert.Same(FileTypeInfo.Csv, FileTypeInfo.FromMimeType("text/comma-separated-values"));
        Assert.Same(FileTypeInfo.Csv, FileTypeInfo.FromMimeType("application/csv"));
        Assert.Same(FileTypeInfo.JavaScript, FileTypeInfo.FromMimeType("application/javascript"));
        Assert.Same(FileTypeInfo.JavaScript, FileTypeInfo.FromMimeType("application/x-javascript"));
        Assert.Same(FileTypeInfo.Graphql, FileTypeInfo.FromMimeType("application/graphql"));
        Assert.Same(FileTypeInfo.WwwFormUrlEncoded, FileTypeInfo.FromMimeType("application/x-www-form-urlencoded"));
    }

    [Fact]
    public void FileTypeInfo_PackageManager_FromExtension_And_Category()
    {
        Assert.Same(FileTypeInfo.NuGetPackage, FileTypeInfo.FromExtension(".nupkg"));
        Assert.Same(FileTypeInfo.JavaJar, FileTypeInfo.FromExtension(".jar"));
        Assert.Same(FileTypeInfo.WindowsInstallerMsi, FileTypeInfo.FromExtension(".msi"));
        Assert.Equal(FileTypeCategory.PackageManager, FileTypeInfo.NuGetPackage.Category);
        Assert.Equal(FileTypeCategory.PackageManager, FileTypeInfo.RpmPackage.Category);
    }

    [Fact]
    public void FileTypeInfo_RpmPackage_FromMimeType_Aliases_Resolve()
    {
        Assert.Same(FileTypeInfo.RpmPackage, FileTypeInfo.FromMimeType(FileTypeInfo.RpmPackage.MimeType));
        Assert.Same(FileTypeInfo.RpmPackage, FileTypeInfo.FromMimeType("application/redhat-package-manager"));
        Assert.Same(FileTypeInfo.RpmPackage, FileTypeInfo.FromMimeType("application/x-redhat-package-manager"));
    }

    [Fact]
    public void FileTypeInfo_DebianPackage_FromMimeType_Alias_Resolves() => Assert.Same(FileTypeInfo.DebianPackage, FileTypeInfo.FromMimeType("application/x-debian-package"));

    [Theory]
    [InlineData(".mkv", "video/x-matroska")]
    [InlineData(".avi", "video/x-msvideo")]
    [InlineData(".mpeg", "video/mpeg")]
    [InlineData(".mpg", "video/mpeg")]
    [InlineData(".ts", "video/mp2t")]
    [InlineData(".m2ts", "video/mp2t")]
    [InlineData(".3gp", "video/3gpp")]
    [InlineData(".m3u8", "application/vnd.apple.mpegurl")]
    [InlineData(".m3u", "application/vnd.apple.mpegurl")]
    public void FileTypeInfo_Video_FromExtension_Resolves(string extension, string mime)
    {
        var t = FileTypeInfo.FromExtension(extension);
        Assert.Equal(FileTypeCategory.Video, t.Category);
        Assert.Equal(mime, t.MimeType);
        Assert.Same(t.Mime, MimeTypeInfo.FromValue(mime));
    }

    [Fact]
    public void FileTypeInfo_FromMimeType_VideoAliases_Resolve()
    {
        Assert.Same(FileTypeInfo.Mkv, FileTypeInfo.FromMimeType("video/matroska"));
        Assert.Same(FileTypeInfo.Avi, FileTypeInfo.FromMimeType("video/avi"));
        Assert.Same(FileTypeInfo.Hls, FileTypeInfo.FromMimeType("application/x-mpegurl"));
        Assert.Same(FileTypeInfo.ThreeGp, FileTypeInfo.FromMimeType("video/3gp"));
    }

    [Fact]
    public void FileTypeInfo_FromMimeType_StripsCharsetParameter()
        => Assert.Same(FileTypeInfo.Txt, FileTypeInfo.FromMimeType("text/plain; charset=utf-8"));

    [Fact]
    public void FileTypeInfo_FromMime_SharedOctetStream_IsUnknown()
        => Assert.Same(FileTypeInfo.Unknown, FileTypeInfo.FromMime(MimeTypeInfo.Unknown));

    [Fact]
    public void GetMimeTypeFromExtension_Mp4_ReturnsVideoMp4()
    {
        var mime = "clip.mp4".GetMimeTypeFromExtension();
        Assert.Same(MimeTypeInfo.Mp4, mime);
        Assert.Equal("video/mp4", mime.Value);
    }
}