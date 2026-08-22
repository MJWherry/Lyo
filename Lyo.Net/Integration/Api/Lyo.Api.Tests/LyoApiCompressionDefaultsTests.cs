using Lyo.Api;
using Lyo.Common.Records;

namespace Lyo.Api.Tests;

public sealed class LyoApiCompressionDefaultsTests
{
    [Fact]
    public void MimeTypes_IsWildcard()
    {
        Assert.Equal(["*/*"], LyoApiCompressionDefaults.MimeTypes);
    }

    [Fact]
    public void ExcludedMimeTypes_OmitsOctetStreamAndCompressibleText()
    {
        var excluded = LyoApiCompressionDefaults.ExcludedMimeTypes;
        Assert.DoesNotContain(FileTypeInfo.Unknown.MimeType, excluded);
        Assert.DoesNotContain(FileTypeInfo.Csv.MimeType, excluded);
        Assert.DoesNotContain(FileTypeInfo.Json.MimeType, excluded);
        Assert.DoesNotContain(FileTypeInfo.Html.MimeType, excluded);
        Assert.DoesNotContain(FileTypeInfo.Svg.MimeType, excluded);
    }

    [Fact]
    public void ExcludedMimeTypes_IncludesAlreadyCompressedTypes()
    {
        var excluded = LyoApiCompressionDefaults.ExcludedMimeTypes;
        Assert.Contains(FileTypeInfo.Jpeg.MimeType, excluded);
        Assert.Contains(FileTypeInfo.Png.MimeType, excluded);
        Assert.Contains(FileTypeInfo.Zip.MimeType, excluded);
        Assert.Contains(FileTypeInfo.Gz.MimeType, excluded);
        Assert.Contains(FileTypeInfo.Xlsx.MimeType, excluded);
        Assert.Contains(FileTypeInfo.Pdf.MimeType, excluded);
        Assert.Contains(FileTypeInfo.Mp3.MimeType, excluded);
        Assert.Contains("image/jpg", excluded);
    }
}
