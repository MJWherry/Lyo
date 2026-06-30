namespace Lyo.Hashing.Internal;

/// <summary>
/// Reflected (LSB-first) table-driven CRC-32 covering the IEEE/ISO-HDLC variant (poly <c>0xEDB88320</c>) and CRC-32C/Castagnoli (poly <c>0x82F63B78</c>). Both use an initial
/// register of <c>0xFFFFFFFF</c> and a final XOR of <c>0xFFFFFFFF</c>, matching <c>System.IO.Hashing.Crc32</c> for the IEEE variant.
/// </summary>
internal sealed class ReflectedCrc32Calculator : ChecksumCalculator
{
    internal enum Variant
    {
        Crc32,
        Crc32C
    }

    private const uint InitialState = 0xFFFFFFFFu;

    private static readonly uint[] Crc32Table = BuildTable(0xEDB88320u);

    private static readonly uint[] Crc32CTable = BuildTable(0x82F63B78u);

    private readonly uint[] _table;

    private uint _crc = InitialState;

    public ReflectedCrc32Calculator(Variant variant) => _table = variant == Variant.Crc32C ? Crc32CTable : Crc32Table;

    public override int HashSizeInBytes => 4;

    public override void Append(ReadOnlySpan<byte> data)
    {
        var crc = _crc;
        foreach (var b in data)
            crc = _table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        _crc = crc;
    }

    public override ulong GetCurrentValue() => _crc ^ 0xFFFFFFFFu;

    private static uint[] BuildTable(uint reversedPolynomial)
    {
        var table = new uint[256];
        for (var i = 0u; i < 256u; i++) {
            var c = i;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? reversedPolynomial ^ (c >> 1) : c >> 1;

            table[i] = c;
        }

        return table;
    }
}
