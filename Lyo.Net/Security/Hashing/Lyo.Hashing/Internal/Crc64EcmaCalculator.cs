namespace Lyo.Hashing.Internal;

/// <summary>
/// Table-driven CRC-64/ECMA-182, non-reflected (MSB first): poly <c>0x42F0E1EBA9EA3693</c>, start register <c>0</c>, no final XOR. Same as <c>System.IO.Hashing.Crc64</c>;
/// <c>"123456789"</c> yields check value <c>0x6C40DF5F0B497347</c>.
/// </summary>
internal sealed class Crc64EcmaCalculator : ChecksumCalculator
{
    private const ulong Polynomial = 0x42F0E1EBA9EA3693UL;

    private static readonly ulong[] Table = BuildTable();

    private ulong _crc;

    public override int HashSizeInBytes => 8;

    public override void Append(ReadOnlySpan<byte> data)
    {
        var crc = _crc;
        foreach (var b in data)
            crc = Table[(byte)((crc >> 56) ^ b)] ^ (crc << 8);

        _crc = crc;
    }

    public override ulong GetCurrentValue() => _crc;

    private static ulong[] BuildTable()
    {
        var table = new ulong[256];
        for (var i = 0; i < 256; i++) {
            var crc = (ulong)i << 56;
            for (var k = 0; k < 8; k++)
                crc = (crc & 0x8000000000000000UL) != 0 ? (crc << 1) ^ Polynomial : crc << 1;

            table[i] = crc;
        }

        return table;
    }
}