using System.Buffers.Binary;

namespace Lyo.Cache.Internal;

/// <summary>Binary frame: magic "LYO1", flags, uint32 LE payload length, payload.</summary>
internal static class CachePayloadFrame
{
    internal const int HeaderLength = 9;

    internal const byte FlagCompressed = 0x01;
    internal const byte FlagEncrypted = 0x02;

    private static ReadOnlySpan<byte> Magic => "LYO1"u8;

    internal static byte[] Create(ReadOnlySpan<byte> payload, byte flags)
    {
        var buf = new byte[HeaderLength + payload.Length];
        Magic.CopyTo(buf);
        buf[4] = flags;
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(5, 4), (uint)payload.Length);
        payload.CopyTo(buf.AsSpan(HeaderLength));
        return buf;
    }

    internal static void Parse(ReadOnlySpan<byte> frame, out byte flags, out ReadOnlySpan<byte> payload)
    {
        if (frame.Length < HeaderLength)
            throw new InvalidDataException("Cache payload frame is too short.");

        if (!frame.StartsWith(Magic))
            throw new InvalidDataException("Cache payload frame has invalid magic.");

        flags = frame[4];
        var len = BinaryPrimitives.ReadUInt32LittleEndian(frame.Slice(5, 4));
        if (len > int.MaxValue - HeaderLength)
            throw new InvalidDataException("Cache payload length is invalid.");

        if (frame.Length != HeaderLength + (int)len)
            throw new InvalidDataException("Cache payload frame length does not match header.");

        payload = frame.Slice(HeaderLength, (int)len);
    }
}
