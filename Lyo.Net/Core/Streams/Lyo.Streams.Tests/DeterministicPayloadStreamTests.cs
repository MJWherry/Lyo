namespace Lyo.Streams.Tests;

public sealed class DeterministicPayloadStreamTests
{
    [Fact]
    public void Same_Seed_And_Length_Produce_Identical_Bytes()
    {
        const int seed = DeterministicPayloadStream.DefaultSeed;
        const int length = 10_000;
        using var a = new DeterministicPayloadStream(length, seed);
        using var b = new DeterministicPayloadStream(length, seed);
        var bufA = new byte[length];
        var bufB = new byte[length];
        Assert.Equal(length, a.Read(bufA, 0, length));
        Assert.Equal(length, b.Read(bufB, 0, length));
        Assert.Equal(bufA, bufB);
    }

    [Fact]
    public void Create_Matches_Stream_Prefix_For_Default_Seed()
    {
        const int length = 4096;
        var buffered = DeterministicPayloadStream.CreateBytes(length);
        using var stream = new DeterministicPayloadStream(length);
        var fromStream = new byte[length];
        Assert.Equal(length, stream.Read(fromStream, 0, length));
        Assert.Equal(buffered, fromStream);
    }

    [Fact]
    public void Length_And_Eof_Are_Honoured()
    {
        using var stream = new DeterministicPayloadStream(5, 1);
        Assert.Equal(5, stream.Length);
        var buf = new byte[8];
        Assert.Equal(5, stream.Read(buf, 0, 8));
        Assert.Equal(0, stream.Read(buf, 0, 8));
        Assert.Equal(5, stream.Position);
    }

    [Fact]
    public void Seek_Zero_Replays_Sequence()
    {
        using var stream = new DeterministicPayloadStream(64, 7);
        var first = new byte[64];
        var second = new byte[64];
        _ = stream.Read(first, 0, 64);
        stream.Position = 0;
        _ = stream.Read(second, 0, 64);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Write_Throws()
    {
        using var stream = new DeterministicPayloadStream(1, 0);
        Assert.Throws<NotSupportedException>(() => stream.Write([1], 0, 1));
    }

    [Fact]
    public void Negative_Length_Throws() => Assert.ThrowsAny<ArgumentException>(() => new DeterministicPayloadStream(-1, 0));
}