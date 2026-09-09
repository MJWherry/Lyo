using System.Text;
using Lyo.Exceptions;

namespace Lyo.Encryption;

/// <summary>
/// Span reader/writer for the length-prefixed string <see cref="BinaryWriter.Write(string)" /> emits: 7-bit encoded UTF-8 length, then the bytes.
/// Envelope framing uses this so a header can be measured and written without a <see cref="BinaryWriter" /> over a stream.
/// </summary>
public static class BinaryWriterString
{
    /// <summary>On-wire byte count of <paramref name="value" />, used to pre-size an output buffer.</summary>
    /// <param name="value">String to measure.</param>
    public static int GetByteCount(string value)
    {
        var utf8Len = Encoding.UTF8.GetByteCount(value);
        return Get7BitEncodedIntByteCount(utf8Len) + utf8Len;
    }

    /// <summary>Writes <paramref name="value" /> into <paramref name="destination" /> and returns how many bytes were written.</summary>
    /// <param name="destination">Buffer with room for at least <see cref="GetByteCount" /> bytes.</param>
    /// <param name="value">String to write.</param>
    public static int Write(Span<byte> destination, string value)
    {
        var utf8 = Encoding.UTF8.GetBytes(value);
        var lenBytes = Write7BitEncodedInt(destination, utf8.Length);
        utf8.CopyTo(destination[lenBytes..]);
        return lenBytes + utf8.Length;
    }

    /// <summary>Reads a length-prefixed string from the start of <paramref name="source" />.</summary>
    /// <param name="source">Buffer sitting on the length prefix.</param>
    /// <param name="bytesConsumed">Bytes consumed, including the prefix.</param>
    /// <exception cref="InvalidDataException">Malformed prefix, or the declared length overruns <paramref name="source" />.</exception>
    public static string Read(ReadOnlySpan<byte> source, out int bytesConsumed)
    {
        var utf8Len = Read7BitEncodedInt(source, out var lenBytes);
        if (utf8Len < 0 || lenBytes + utf8Len > source.Length)
            throw new InvalidDataException("Invalid encrypted data format: truncated BinaryWriter string.");

        bytesConsumed = lenBytes + utf8Len;
        return utf8Len == 0 ? string.Empty : Encoding.UTF8.GetString(source.Slice(lenBytes, utf8Len).ToArray());
    }

    private static int Get7BitEncodedIntByteCount(int value)
    {
        ArgumentHelpers.ThrowIfNegative(value);
        var count = 1;
        var v = (uint)value;
        while (v >= 0x80) {
            v >>= 7;
            count++;
        }

        return count;
    }

    private static int Write7BitEncodedInt(Span<byte> destination, int value)
    {
        ArgumentHelpers.ThrowIfNegative(value);
        var v = (uint)value;
        var written = 0;
        while (v >= 0x80) {
            destination[written++] = (byte)(v | 0x80);
            v >>= 7;
        }

        destination[written++] = (byte)v;
        return written;
    }

    private static int Read7BitEncodedInt(ReadOnlySpan<byte> source, out int bytesConsumed)
    {
        var result = 0;
        var shift = 0;
        bytesConsumed = 0;
        while (shift < 35) {
            if (bytesConsumed >= source.Length)
                throw new InvalidDataException("Invalid encrypted data format: truncated 7-bit encoded integer.");

            var b = source[bytesConsumed++];
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return result;

            shift += 7;
        }

        throw new InvalidDataException("Invalid encrypted data format: malformed 7-bit encoded integer.");
    }
}
