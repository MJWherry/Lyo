using Lyo.Common.Records;

namespace Lyo.Compression.Tests;

public class LyoCompressionConstantsAlignWithFileTypeInfoTests
{
    [Fact]
    public void Constants_Data_Extensions_Match_FileTypeInfo_StreamDefaults()
    {
        Assert.Equal(FileTypeInfo.Gz.DefaultExtension, Constants.Data.GZipExtension);
        Assert.Equal(FileTypeInfo.Brotli.DefaultExtension, Constants.Data.BrotliExtension);
        Assert.Equal(FileTypeInfo.LZMAStream.DefaultExtension, Constants.Data.LZMAExtension);
        Assert.Equal(FileTypeInfo.Xz.DefaultExtension, Constants.Data.XZExtension);
    }
}
