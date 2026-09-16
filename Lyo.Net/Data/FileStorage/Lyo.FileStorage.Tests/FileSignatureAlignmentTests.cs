using Lyo.FileStorage.Policy;

namespace Lyo.FileStorage.Tests;

public sealed class FileSignatureAlignmentTests
{
    private readonly MagicFileTypeSniffer _sniffer = new();

    [Fact]
    public void ThrowIfMismatch_PdfBytesWithPdfName_Allows()
        => FileSignatureAlignment.ThrowIfMismatch(_sniffer, "%PDF-1.4"u8, "report.pdf", null);

    [Fact]
    public void ThrowIfMismatch_ZipBytesWithPdfName_Throws()
        => Assert.Throws<FilePolicyRejectedException>(() => FileSignatureAlignment.ThrowIfMismatch(_sniffer, ZipHeader, "report.pdf", null));

    [Fact]
    public void ThrowIfMismatch_ZipBytesWithJarOrDocxName_Allows()
    {
        FileSignatureAlignment.ThrowIfMismatch(_sniffer, ZipHeader, "app.jar", null);
        FileSignatureAlignment.ThrowIfMismatch(_sniffer, ZipHeader, "sheet.docx", null);
    }

    [Fact]
    public void ThrowIfMismatch_PdfBytesWithTxtName_Throws()
        => Assert.Throws<FilePolicyRejectedException>(() => FileSignatureAlignment.ThrowIfMismatch(_sniffer, "%PDF-1.4"u8, "notes.txt", null));

    [Fact]
    public void ThrowIfMismatch_PlainTextWithTxtName_Allows()
        => FileSignatureAlignment.ThrowIfMismatch(_sniffer, "hello"u8, "notes.txt", null);

    private static readonly byte[] ZipHeader = [0x50, 0x4B, 0x03, 0x04, 0x00];
}
