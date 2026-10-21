using Lyo.Common.Metadata.Records;
using Lyo.FileStorage.Policy;

namespace Lyo.FileStorage.Tests;

public sealed class MagicFileTypeSnifferTests
{
    private readonly MagicFileTypeSniffer _sniffer = new();

    [Fact]
    public void Identify_PdfHeader_ReturnsPdf() => Assert.Equal(FileTypeInfo.Pdf, _sniffer.Identify("%PDF-1.7"u8));

    [Fact]
    public void Identify_ZipHeader_ReturnsZip() => Assert.Equal(FileTypeInfo.Zip, _sniffer.Identify([0x50, 0x4B, 0x03, 0x04, 0x00]));

    [Fact]
    public void Identify_ShortBuffer_ReturnsUnknown() => Assert.Equal(FileTypeInfo.Unknown, _sniffer.Identify([0x01]));

    [Fact]
    public void Identify_PngHeader_ReturnsPng() => Assert.Equal(FileTypeInfo.Png, _sniffer.Identify([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));

    [Fact]
    public void Identify_JpegHeader_ReturnsJpeg() => Assert.Equal(FileTypeInfo.Jpeg, _sniffer.Identify([0xFF, 0xD8, 0xFF, 0xE0]));
}