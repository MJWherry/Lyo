namespace Lyo.Rfid;

/// <summary>ISO 15693 CRC-16 (poly 0x8408, init 0xFFFF), transmitted least-significant byte first.</summary>
public static class Iso15693Crc
{
    /// <summary>Appends the CRC to <paramref name="body" />.</summary>
    public static byte[] Append(ReadOnlySpan<byte> body)
    {
        var crc = Compute(body);
        var frame = new byte[body.Length + 2];
        body.CopyTo(frame);
        frame[^2] = (byte)crc;
        frame[^1] = (byte)(crc >> 8);
        return frame;
    }

    /// <summary>Throws <see cref="RfidException" /> when the trailing CRC does not match the body.</summary>
    public static void Verify(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
            throw new RfidException("ISO 15693 frame is too short to carry a CRC.");

        var body = frame[..^2];
        var crc = Compute(body);
        if (frame[^2] != (byte)crc || frame[^1] != (byte)(crc >> 8))
            throw new RfidException("ISO 15693 CRC does not match.");
    }

    private static ushort Compute(ReadOnlySpan<byte> body)
    {
        var crc = 0xFFFF;
        foreach (var current in body) {
            crc ^= current;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0x8408 : crc >> 1;
        }

        return (ushort)crc;
    }
}