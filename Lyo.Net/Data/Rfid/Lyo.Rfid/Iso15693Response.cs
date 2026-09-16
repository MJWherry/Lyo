namespace Lyo.Rfid;

/// <summary>
/// Checks an ISO 15693 tag response. The buffer includes the flags byte and the CRC the reader returned.
/// </summary>
public static class Iso15693Response
{
    /// <summary>
    /// Verifies the CRC and rejects a response whose error flag is set.
    /// The returned buffer is the bytes between the flags and the CRC.
    /// </summary>
    public static byte[] Payload(ReadOnlySpan<byte> response)
    {
        if (response.Length < 3)
            throw new RfidException("ISO 15693 response is too short.");

        Iso15693Crc.Verify(response);
        if ((response[0] & 0x01) != 0)
            throw new RfidException($"ISO 15693 error flag set (0x{response[0]:X2}).");

        return [.. response.Slice(1, response.Length - 3)];
    }
}
