using Lyo.Exceptions;

namespace Lyo.Nfc;

/// <summary>
/// Type 2 Tag TLV around an NDEF message: <c>0x03</c>, length, message, <c>0xFE</c>.
/// <see cref="Decode"/> throws <see cref="NfcException"/> when the terminator is missing.
/// </summary>
public static class NdefType2
{
    /// <summary>Wraps <paramref name="message"/> in a Type 2 NDEF TLV.</summary>
    public static byte[] Encode(NdefMessage message)
    {
        ArgumentHelpers.ThrowIfNull(message);
        var ndef = EncodeMessage(message);
        if (ndef.Length < 0xFF) {
            var tlv = new byte[2 + ndef.Length + 1];
            tlv[0] = 0x03;
            tlv[1] = (byte)ndef.Length;
            ndef.CopyTo(tlv, 2);
            tlv[^1] = 0xFE;
            return tlv;
        }

        var longTlv = new byte[4 + ndef.Length + 1];
        longTlv[0] = 0x03;
        longTlv[1] = 0xFF;
        longTlv[2] = (byte)(ndef.Length >> 8);
        longTlv[3] = (byte)ndef.Length;
        ndef.CopyTo(longTlv, 4);
        longTlv[^1] = 0xFE;
        return longTlv;
    }

    /// <summary>Reads the first NDEF TLV in <paramref name="userMemory"/>. Bytes after the terminator are ignored.</summary>
    public static NdefMessage Decode(ReadOnlySpan<byte> userMemory)
    {
        var index = 0;
        while (index < userMemory.Length && userMemory[index] == 0x00)
            index++;

        if (index >= userMemory.Length || userMemory[index] != 0x03)
            throw new NfcException("No NDEF TLV.");

        index++;
        if (index >= userMemory.Length)
            throw new NfcException("NDEF TLV is missing the terminator.");

        int length;
        if (userMemory[index] == 0xFF) {
            if (index + 2 >= userMemory.Length)
                throw new NfcException("NDEF TLV is missing the terminator.");

            length = (userMemory[index + 1] << 8) | userMemory[index + 2];
            index += 3;
        }
        else {
            length = userMemory[index];
            index++;
        }

        if (index + length > userMemory.Length)
            throw new NfcException("NDEF TLV is missing the terminator.");

        var ndef = userMemory.Slice(index, length).ToArray();
        index += length;
        if (index >= userMemory.Length || userMemory[index] != 0xFE)
            throw new NfcException("NDEF TLV is missing the terminator.");

        return DecodeMessage(ndef);
    }

    private static byte[] EncodeMessage(NdefMessage message)
    {
        using var buffer = new MemoryStream();
        for (var i = 0; i < message.Records.Count; i++)
            WriteRecord(buffer, message.Records[i], i == 0, i == message.Records.Count - 1);

        return buffer.ToArray();
    }

    private static void WriteRecord(Stream buffer, NdefRecord record, bool first, bool last)
    {
        var payload = record.Payload;
        var type = record.Type;
        var shortRecord = payload.Length <= 255;
        var header = (byte)record.TypeNameFormat;
        if (first)
            header |= 0x80;
        if (last)
            header |= 0x40;
        if (shortRecord)
            header |= 0x10;

        buffer.WriteByte(header);
        buffer.WriteByte((byte)type.Length);
        if (shortRecord) {
            buffer.WriteByte((byte)payload.Length);
        }
        else {
            buffer.WriteByte((byte)(payload.Length >> 24));
            buffer.WriteByte((byte)(payload.Length >> 16));
            buffer.WriteByte((byte)(payload.Length >> 8));
            buffer.WriteByte((byte)payload.Length);
        }

        buffer.Write(type, 0, type.Length);
        buffer.Write(payload, 0, payload.Length);
    }

    private static NdefMessage DecodeMessage(byte[] ndef)
    {
        var records = new List<NdefRecord>();
        var index = 0;
        while (index < ndef.Length) {
            var (record, next, messageEnd) = ReadRecord(ndef, index);
            records.Add(record);
            index = next;
            if (messageEnd)
                break;
        }

        if (records.Count == 0)
            throw new NfcException("NDEF message has no records.");

        return NdefMessage.FromParsed(records.ToArray());
    }

    private static (NdefRecord Record, int Next, bool MessageEnd) ReadRecord(byte[] ndef, int index)
    {
        if (index >= ndef.Length)
            throw new NfcException("NDEF record header is truncated.");

        var header = ndef[index++];
        if ((header & 0x20) != 0)
            throw new NfcException("Chunked NDEF records are not supported.");

        var messageEnd = (header & 0x40) != 0;
        var shortRecord = (header & 0x10) != 0;
        var idPresent = (header & 0x08) != 0;
        var tnf = (NdefTypeNameFormat)(header & 0x07);
        if (index >= ndef.Length)
            throw new NfcException("NDEF record header is truncated.");

        var typeLength = ndef[index++];
        int payloadLength;
        if (shortRecord) {
            if (index >= ndef.Length)
                throw new NfcException("NDEF record header is truncated.");

            payloadLength = ndef[index++];
        }
        else {
            if (index + 4 > ndef.Length)
                throw new NfcException("NDEF record header is truncated.");

            payloadLength = (ndef[index] << 24) | (ndef[index + 1] << 16) | (ndef[index + 2] << 8) | ndef[index + 3];
            index += 4;
        }

        var idLength = 0;
        if (idPresent) {
            if (index >= ndef.Length)
                throw new NfcException("NDEF record header is truncated.");

            idLength = ndef[index++];
        }

        if (index + typeLength + idLength + payloadLength > ndef.Length)
            throw new NfcException("NDEF record payload is truncated.");

        var type = new byte[typeLength];
        Buffer.BlockCopy(ndef, index, type, 0, typeLength);
        index += typeLength + idLength;
        var payload = new byte[payloadLength];
        Buffer.BlockCopy(ndef, index, payload, 0, payloadLength);
        index += payloadLength;
        return (Materialize(tnf, type, payload), index, messageEnd);
    }

    private static NdefRecord Materialize(NdefTypeNameFormat tnf, byte[] type, byte[] payload)
    {
        if (tnf == NdefTypeNameFormat.WellKnown && type is [(byte)'T'])
            return NdefTextRecord.FromPayload(payload);

        if (tnf == NdefTypeNameFormat.WellKnown && type is [(byte)'U'])
            return NdefUriRecord.FromPayload(payload);

        return NdefRecord.Raw(tnf, type, payload);
    }
}

/// <summary>Reads and writes an NDEF message in the user pages of an NTAG21x tag.</summary>
public static class NfcNdef
{
    /// <summary>
    /// Reads user pages four at a time and decodes the NDEF TLV. Stops at the <c>0xFE</c> terminator, so a short message does not pull the rest of the tag.
    /// </summary>
    public static async Task<NdefMessage> ReadAsync(INfcTag tag, NtagLayout layout, CancellationToken cancellationToken)
    {
        var user = await ReadUserAreaAsync(tag, layout, cancellationToken).ConfigureAwait(false);
        return NdefType2.Decode(user);
    }

    /// <summary>
    /// Reads user pages four at a time. Stops when the NDEF terminator is present. A tag with no complete NDEF TLV returns the whole user area, read once.
    /// </summary>
    public static async Task<byte[]> ReadUserAreaAsync(INfcTag tag, NtagLayout layout, CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(tag);
        ArgumentHelpers.ThrowIfNull(layout);
        var buffer = new byte[layout.UserCapacityBytes];
        var filledPages = 0;
        while (filledPages < layout.UserPageCount) {
            var pages = Math.Min(4, layout.UserPageCount - filledPages);
            var chunk = await tag.ReadPagesAsync(layout.UserStartPage + filledPages, pages, cancellationToken).ConfigureAwait(false);
            chunk.CopyTo(buffer, filledPages * 4);
            filledPages += pages;
            if (ContainsCompleteNdef(buffer.AsSpan(0, filledPages * 4)))
                break;
        }

        if (filledPages * 4 == buffer.Length)
            return buffer;

        var filled = new byte[filledPages * 4];
        Buffer.BlockCopy(buffer, 0, filled, 0, filled.Length);
        return filled;
    }

    private static bool ContainsCompleteNdef(ReadOnlySpan<byte> userMemory)
    {
        var index = 0;
        while (index < userMemory.Length && userMemory[index] == 0x00)
            index++;

        if (index >= userMemory.Length || userMemory[index] != 0x03)
            return false;

        index++;
        if (index >= userMemory.Length)
            return false;

        int length;
        if (userMemory[index] == 0xFF) {
            if (index + 2 >= userMemory.Length)
                return false;

            length = (userMemory[index + 1] << 8) | userMemory[index + 2];
            index += 3;
        }
        else {
            length = userMemory[index];
            index++;
        }

        if (index + length >= userMemory.Length)
            return false;

        return userMemory[index + length] == 0xFE;
    }

    /// <summary>
    /// Encodes <paramref name="message"/> and writes it from page 4. The last page is zero-padded. Config pages are left alone.
    /// </summary>
    public static async Task WriteAsync(INfcTag tag, NtagLayout layout, NdefMessage message, CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(tag);
        ArgumentHelpers.ThrowIfNull(layout);
        ArgumentHelpers.ThrowIfNull(message);
        var tlv = NdefType2.Encode(message);
        if (tlv.Length > layout.UserCapacityBytes)
            throw new NfcException("NDEF message does not fit in the tag user memory.");

        var pageBytes = ((tlv.Length + 3) / 4) * 4;
        var padded = new byte[pageBytes];
        tlv.CopyTo(padded, 0);
        for (var offset = 0; offset < padded.Length; offset += 4) {
            var page = layout.UserStartPage + (offset / 4);
            await tag.WritePageAsync(page, padded.AsMemory(offset, 4), cancellationToken).ConfigureAwait(false);
        }
    }
}
