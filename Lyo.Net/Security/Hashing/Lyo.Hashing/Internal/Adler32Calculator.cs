namespace Lyo.Hashing.Internal;

/// <summary>Adler-32 (zlib / RFC 1950). Two 16-bit running sums modulo 65521 combined as <c>(b &lt;&lt; 16) | a</c>. Check value of <c>"123456789"</c> is <c>0x091E01DE</c>.</summary>
internal sealed class Adler32Calculator : ChecksumCalculator
{
    private const uint ModAdler = 65521u;

    // Largest run of bytes that can be folded before the b accumulator can overflow a 32-bit register.
    private const int MaxChunk = 5552;

    private uint _a = 1;

    private uint _b;

    public override int HashSizeInBytes => 4;

    public override void Append(ReadOnlySpan<byte> data)
    {
        var a = _a;
        var b = _b;
        var offset = 0;
        var remaining = data.Length;
        while (remaining > 0) {
            var chunk = remaining < MaxChunk ? remaining : MaxChunk;
            for (var i = 0; i < chunk; i++) {
                a += data[offset + i];
                b += a;
            }

            a %= ModAdler;
            b %= ModAdler;
            offset += chunk;
            remaining -= chunk;
        }

        _a = a;
        _b = b;
    }

    public override ulong GetCurrentValue() => (_b << 16) | _a;
}