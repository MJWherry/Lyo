using Lyo.Common.Enums;

namespace Lyo.Hashing.Tests;

public sealed class ChecksumStreamTests
{
    private static readonly byte[] Data = "the quick brown fox jumps over the lazy dog"u8.ToArray();

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32)]
    [InlineData(ChecksumAlgorithm.Crc32C)]
    [InlineData(ChecksumAlgorithm.Crc64)]
    [InlineData(ChecksumAlgorithm.Adler32)]
    public void Write_path_matches_static(ChecksumAlgorithm algorithm)
    {
        var expected = Checksummer.ComputeValue(algorithm, Data);
        using var ms = new MemoryStream();
        using var cs = new ChecksumStream(ms, algorithm);
        cs.Write(Data, 0, Data.Length);
        Assert.Equal(expected, cs.GetChecksumValue());
        Assert.Equal(Checksummer.Compute(algorithm, Data), cs.GetChecksum());
    }

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32)]
    [InlineData(ChecksumAlgorithm.Crc64)]
    public void Read_path_matches_static(ChecksumAlgorithm algorithm)
    {
        var expected = Checksummer.ComputeValue(algorithm, Data);
        using var ms = new MemoryStream(Data);
        using var cs = new ChecksumStream(ms, algorithm);
        var buffer = new byte[7];
        while (cs.Read(buffer, 0, buffer.Length) > 0) { }

        Assert.Equal(expected, cs.GetChecksumValue());
    }

    [Fact]
    public void Chunked_writes_match_single_write()
    {
        var expected = Checksummer.ComputeValue(ChecksumAlgorithm.Crc32, Data);
        using var ms = new MemoryStream();
        using var cs = new ChecksumStream(ms, ChecksumAlgorithm.Crc32);
        foreach (var b in Data)
            cs.Write([b], 0, 1);

        Assert.Equal(expected, cs.GetChecksumValue());
    }

    [Fact]
    public void GetChecksum_can_be_called_repeatedly()
    {
        using var ms = new MemoryStream();
        using var cs = new ChecksumStream(ms, ChecksumAlgorithm.Crc64);
        cs.Write(Data, 0, Data.Length);
        Assert.Equal(cs.GetChecksumValue(), cs.GetChecksumValue());
        Assert.Equal(cs.GetChecksum(), cs.GetChecksum());
    }

    [Fact]
    public void GetChecksumHex_matches_hex_of_bytes()
    {
        using var ms = new MemoryStream();
        using var cs = new ChecksumStream(ms, ChecksumAlgorithm.Crc32);
        cs.Write(Data, 0, Data.Length);
        Assert.Equal(HexEncoding.ToHexString(cs.GetChecksum(), TextLetterCase.Lower), cs.GetChecksumHex(TextLetterCase.Lower));
    }

    [Fact]
    public void Throws_on_null_base_stream() => Assert.Throws<ArgumentNullException>(() => new ChecksumStream(null!, ChecksumAlgorithm.Crc32));
}