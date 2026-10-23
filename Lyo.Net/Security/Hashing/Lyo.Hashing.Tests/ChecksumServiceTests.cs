using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Hashing.Tests;

public sealed class ChecksumServiceTests
{
    private static readonly byte[] CheckVector = "123456789"u8.ToArray();

    [Fact]
    public void Checksum_Matches_Static_Checksummer()
    {
        var svc = HashingService.Shared;
        Assert.Equal(Checksummer.Compute(ChecksumAlgorithm.Crc32, CheckVector), svc.Checksum(ChecksumAlgorithm.Crc32, CheckVector));
        Assert.Equal(Checksummer.Compute(ChecksumAlgorithm.Crc32, CheckVector), svc.Checksum(ChecksumAlgorithm.Crc32, CheckVector.AsSpan()));
        Assert.Equal(0xCBF43926UL, svc.ChecksumValue(ChecksumAlgorithm.Crc32, CheckVector));
    }

    [Fact]
    public void Checksum_Stream_Matches_Buffer()
    {
        var svc = HashingService.Shared;
        using var ms = new MemoryStream(CheckVector);
        Assert.Equal(svc.Checksum(ChecksumAlgorithm.Crc64, CheckVector), svc.Checksum(ChecksumAlgorithm.Crc64, ms));
    }

    [Fact]
    public async Task ChecksumFileAsync_Matches_Buffer()
    {
        var svc = HashingService.Shared;
        var path = Path.GetTempFileName();
        try {
            await File.WriteAllBytesAsync(path, CheckVector, TestCt);
            var fromFile = await svc.ChecksumFileAsync(ChecksumAlgorithm.Crc32C, path, TestCt);
            Assert.Equal(svc.Checksum(ChecksumAlgorithm.Crc32C, CheckVector), fromFile);
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ChecksumFileAsync_Missing_File_Throws()
        => await Assert.ThrowsAsync<FileNotFoundException>(() => HashingService.Shared.ChecksumFileAsync(
            ChecksumAlgorithm.Crc32, Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7().ToString("N")), TestCt));

    [Fact]
    public void CreateChecksumStream_Wraps_Underlying_Stream()
    {
        using var inner = new MemoryStream();
        using var cs = HashingService.Shared.CreateChecksumStream(inner, ChecksumAlgorithm.Crc32);
        cs.Write(CheckVector, 0, CheckVector.Length);
        Assert.Equal(0xCBF43926UL, cs.GetChecksumValue());
    }

    [Fact]
    public void Hex_Casing_Honors_Options()
    {
        var lower = new HashingService(new() { DefaultHexLetterCase = TextLetterCase.Lower });
        var bytes = lower.Checksum(ChecksumAlgorithm.Crc32, CheckVector);
        Assert.Equal("cbf43926", lower.ToHex(bytes));
    }
}