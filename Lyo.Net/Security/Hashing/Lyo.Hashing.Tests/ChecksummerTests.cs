using System.Text;
using Lyo.Common.Enums;

namespace Lyo.Hashing.Tests;

public sealed class ChecksummerTests
{
    private static readonly byte[] CheckVector = "123456789"u8.ToArray();

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32, 0xCBF43926UL)]
    [InlineData(ChecksumAlgorithm.Crc32C, 0xE3069283UL)]
    [InlineData(ChecksumAlgorithm.Crc64, 0x6C40DF5F0B497347UL)]
    [InlineData(ChecksumAlgorithm.Adler32, 0x091E01DEUL)]
    public void ComputeValue_matches_known_check_vector(ChecksumAlgorithm algorithm, ulong expected)
        => Assert.Equal(expected, Checksummer.ComputeValue(algorithm, CheckVector));

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32, 0x00000000UL)]
    [InlineData(ChecksumAlgorithm.Crc32C, 0x00000000UL)]
    [InlineData(ChecksumAlgorithm.Crc64, 0x0000000000000000UL)]
    [InlineData(ChecksumAlgorithm.Adler32, 0x00000001UL)]
    public void ComputeValue_empty_input_matches_known_initial(ChecksumAlgorithm algorithm, ulong expected)
        => Assert.Equal(expected, Checksummer.ComputeValue(algorithm, ReadOnlySpan<byte>.Empty));

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32, "CBF43926")]
    [InlineData(ChecksumAlgorithm.Crc32C, "E3069283")]
    [InlineData(ChecksumAlgorithm.Crc64, "6C40DF5F0B497347")]
    [InlineData(ChecksumAlgorithm.Adler32, "091E01DE")]
    public void Compute_returns_big_endian_bytes(ChecksumAlgorithm algorithm, string expectedHex)
    {
        var bytes = Checksummer.Compute(algorithm, CheckVector);
        Assert.Equal(expectedHex.Length / 2, bytes.Length);
        Assert.Equal(expectedHex, HexEncoding.ToHexString(bytes, TextLetterCase.Upper));
    }

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32)]
    [InlineData(ChecksumAlgorithm.Crc32C)]
    [InlineData(ChecksumAlgorithm.Crc64)]
    [InlineData(ChecksumAlgorithm.Adler32)]
    public void Span_byte_array_and_stream_paths_agree(ChecksumAlgorithm algorithm)
    {
        var data = Encoding.UTF8.GetBytes("the quick brown fox jumps over the lazy dog");
        var fromSpan = Checksummer.ComputeValue(algorithm, data.AsSpan());
        var fromArray = Checksummer.ComputeValue(algorithm, data);
        using var ms = new MemoryStream(data);
        var fromStream = Checksummer.ComputeValue(algorithm, ms);
        Assert.Equal(fromSpan, fromArray);
        Assert.Equal(fromSpan, fromStream);
        Assert.Equal(ms.Length, ms.Position);
    }

    [Fact]
    public void Convenience_methods_match_enum_overloads()
    {
        Assert.Equal(Checksummer.ComputeValue(ChecksumAlgorithm.Crc32, CheckVector), Checksummer.ComputeCrc32(CheckVector));
        Assert.Equal(Checksummer.ComputeValue(ChecksumAlgorithm.Crc32C, CheckVector), Checksummer.ComputeCrc32C(CheckVector));
        Assert.Equal(Checksummer.ComputeValue(ChecksumAlgorithm.Crc64, CheckVector), Checksummer.ComputeCrc64(CheckVector));
        Assert.Equal(Checksummer.ComputeValue(ChecksumAlgorithm.Adler32, CheckVector), Checksummer.ComputeAdler32(CheckVector));
    }

    [Fact]
    public void Adler32_large_buffer_matches_incremental_chunks()
    {
        // Exercises the 5552-byte modulo-deferral loop and confirms it equals byte-at-a-time accumulation.
        var data = new byte[20000];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i * 31 + 7);

        var oneShot = Checksummer.ComputeValue(ChecksumAlgorithm.Adler32, data);

        using var ms = new MemoryStream();
        using var incremental = new ChecksumStream(ms, ChecksumAlgorithm.Adler32);
        foreach (var b in data)
            incremental.Write([b], 0, 1);

        Assert.Equal(oneShot, incremental.GetChecksumValue());
    }

    [Fact]
    public void Compute_null_array_throws()
        => Assert.Throws<ArgumentNullException>(() => Checksummer.Compute(ChecksumAlgorithm.Crc32, (byte[])null!));

    [Fact]
    public void ComputeValue_unknown_algorithm_throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Checksummer.ComputeValue((ChecksumAlgorithm)999, CheckVector));
}
