using Lyo.FileStorage.Models;
using Lyo.FileStorage.Policy;

namespace Lyo.FileStorage.Tests;

public sealed class FileSniffedTypeFilterTests
{
    private readonly MagicFileTypeSniffer _sniffer = new();

    [Fact]
    public void ThrowIfRejected_DeniedZip_RejectsPkAndAllowsPdf()
    {
        var options = Options(o => o.DeniedSniffedFileTypes = ["ZIP"]);
        Assert.Throws<FilePolicyRejectedException>(() => FileSniffedTypeFilter.ThrowIfRejected(options, _sniffer.Identify(ZipHeader)));
        FileSniffedTypeFilter.ThrowIfRejected(options, _sniffer.Identify("%PDF-1.4"u8));
    }

    [Fact]
    public void ThrowIfRejected_AllowedPdf_AllowsPdfAndRejectsZipAndPlainText()
    {
        var options = Options(o => o.AllowedSniffedFileTypes = ["PDF"]);
        FileSniffedTypeFilter.ThrowIfRejected(options, _sniffer.Identify("%PDF-1.4"u8));
        Assert.Throws<FilePolicyRejectedException>(() => FileSniffedTypeFilter.ThrowIfRejected(options, _sniffer.Identify(ZipHeader)));
        Assert.Throws<FilePolicyRejectedException>(() => FileSniffedTypeFilter.ThrowIfRejected(options, _sniffer.Identify("hello"u8)));
    }

    [Fact]
    public void ThrowIfRejected_DeniedJar_RejectsPkHeader()
    {
        var options = Options(o => o.DeniedSniffedFileTypes = ["JAR"]);
        Assert.Throws<FilePolicyRejectedException>(() => FileSniffedTypeFilter.ThrowIfRejected(options, _sniffer.Identify(ZipHeader)));
    }

    private static readonly byte[] ZipHeader = [0x50, 0x4B, 0x03, 0x04, 0x00];

    private static DiskFileStorageOptions Options(Action<DiskFileStorageOptions> configure)
    {
        var options = new DiskFileStorageOptions { RootDirectoryPath = "unused" };
        configure(options);
        return options;
    }
}
