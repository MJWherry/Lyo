namespace Lyo.Streams.Tests;

public sealed class DeterministicPayloadStreamTests
{
    [Fact]
    public void Same_seed_and_length_produce_identical_bytes()
    {
        const int seed = 0x4C594F42;
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
    public void Length_and_eof_are_honoured()
    {
        using var stream = new DeterministicPayloadStream(5, seed: 1);
        Assert.Equal(5, stream.Length);
        var buf = new byte[8];
        Assert.Equal(5, stream.Read(buf, 0, 8));
        Assert.Equal(0, stream.Read(buf, 0, 8));
        Assert.Equal(5, stream.Position);
    }

    [Fact]
    public void Seek_zero_replays_sequence()
    {
        using var stream = new DeterministicPayloadStream(64, seed: 7);
        var first = new byte[64];
        var second = new byte[64];
        _ = stream.Read(first, 0, 64);
        stream.Position = 0;
        _ = stream.Read(second, 0, 64);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Write_throws()
    {
        using var stream = new DeterministicPayloadStream(1, seed: 0);
        Assert.Throws<NotSupportedException>(() => stream.Write([1], 0, 1));
    }

    [Fact]
    public void Negative_length_throws() => Assert.ThrowsAny<ArgumentException>(() => new DeterministicPayloadStream(-1, seed: 0));
}
