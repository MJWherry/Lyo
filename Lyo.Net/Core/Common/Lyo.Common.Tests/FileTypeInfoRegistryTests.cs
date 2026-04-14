using Lyo.Common.Enums;
using Lyo.Common.Records;

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
        Assert.Same(FileTypeInfo.LyoTwoKeyEnvelope, FileTypeInfo.FromExtension(
            FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension + FileTypeInfo.TwoKeyEnvelopeSuffix));
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
    }
}
