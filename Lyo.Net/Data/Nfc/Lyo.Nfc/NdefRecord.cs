using System.Text;
using Lyo.Exceptions;

namespace Lyo.Nfc;

/// <summary>NFC Forum type-name format in the NDEF record header.</summary>
public enum NdefTypeNameFormat : byte
{
    /// <summary>Empty record.</summary>
    Empty = 0x00,

    /// <summary>NFC Forum well-known type, such as Text (<c>T</c>) or URI (<c>U</c>).</summary>
    WellKnown = 0x01,

    /// <summary>Media type.</summary>
    Media = 0x02,

    /// <summary>Absolute URI.</summary>
    AbsoluteUri = 0x03,

    /// <summary>External type.</summary>
    External = 0x04,

    /// <summary>Unknown type.</summary>
    Unknown = 0x05,

    /// <summary>Unchanged, used by middle and last chunks.</summary>
    Unchanged = 0x06
}

/// <summary>One NDEF record. <see cref="NdefTextRecord"/> and <see cref="NdefUriRecord"/> are the typed forms. Anything else stays a raw record.</summary>
public class NdefRecord
{
    /// <param name="typeNameFormat">Header TNF.</param>
    /// <param name="type">Record type bytes. Empty for an empty record.</param>
    /// <param name="payload">Record payload.</param>
    protected NdefRecord(NdefTypeNameFormat typeNameFormat, byte[] type, byte[] payload)
    {
        TypeNameFormat = typeNameFormat;
        Type = type;
        Payload = payload;
    }

    /// <summary>Type-name format.</summary>
    public NdefTypeNameFormat TypeNameFormat { get; }

    /// <summary>Record type bytes.</summary>
    public byte[] Type { get; }

    /// <summary>Record payload. For a URI record the first byte is the NFC Forum prefix code.</summary>
    public byte[] Payload { get; }

    /// <summary>A record the caller already encoded. Text and URI helpers are preferred when the payload is one of those.</summary>
    public static NdefRecord Raw(NdefTypeNameFormat typeNameFormat, ReadOnlySpan<byte> type, ReadOnlySpan<byte> payload)
    {
        ArgumentHelpers.ThrowIfNotDefined(typeNameFormat);
        return new NdefRecord(typeNameFormat, [.. type], [.. payload]);
    }
}

/// <summary>NFC Forum well-known Text record (<c>T</c>), UTF-8.</summary>
public sealed class NdefTextRecord : NdefRecord
{
    private NdefTextRecord(string language, string text, byte[] payload)
        : base(NdefTypeNameFormat.WellKnown, [(byte)'T'], payload)
    {
        Language = language;
        Text = text;
    }

    /// <summary>IANA language code stored in the status byte, for example <c>en</c>.</summary>
    public string Language { get; }

    /// <summary>Decoded text.</summary>
    public string Text { get; }

    /// <summary>Builds a UTF-8 Text record. <paramref name="language"/> is 1 to 63 ASCII characters.</summary>
    public static NdefTextRecord Create(string language, string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(language);
        ArgumentHelpers.ThrowIfNull(text);
        ArgumentHelpers.ThrowIf(language.Length > 63 || language.Any(static c => c > 127), "Language must be 1 to 63 ASCII characters.");
        var lang = Encoding.ASCII.GetBytes(language);
        var body = Encoding.UTF8.GetBytes(text);
        var payload = new byte[1 + lang.Length + body.Length];
        payload[0] = (byte)lang.Length;
        lang.CopyTo(payload, 1);
        body.CopyTo(payload, 1 + lang.Length);
        return new(language, text, payload);
    }

    internal static NdefTextRecord FromPayload(byte[] payload)
    {
        if (payload.Length < 1)
            throw new NfcException("Text record payload is empty.");

        var status = payload[0];
        var langLength = status & 0x3F;
        if (payload.Length < 1 + langLength)
            throw new NfcException("Text record language code is longer than the payload.");

        var language = Encoding.ASCII.GetString(payload, 1, langLength);
        var utf16 = (status & 0x80) != 0;
        var text = utf16
            ? Encoding.BigEndianUnicode.GetString(payload, 1 + langLength, payload.Length - 1 - langLength)
            : Encoding.UTF8.GetString(payload, 1 + langLength, payload.Length - 1 - langLength);
        return new(language, text, payload);
    }
}

/// <summary>NFC Forum well-known URI record (<c>U</c>).</summary>
public sealed class NdefUriRecord : NdefRecord
{
    private NdefUriRecord(string uri, byte[] payload)
        : base(NdefTypeNameFormat.WellKnown, [(byte)'U'], payload)
    {
        Uri = uri;
    }

    /// <summary>Absolute URI, prefix expanded.</summary>
    public string Uri { get; }

    /// <summary>Builds a URI record. <c>https://</c> uses prefix code <c>0x04</c>.</summary>
    public static NdefUriRecord Create(string uri)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri);
        var (code, suffix) = NdefUriPrefixes.Split(uri);
        var restBytes = Encoding.UTF8.GetBytes(suffix);
        var payload = new byte[1 + restBytes.Length];
        payload[0] = code;
        restBytes.CopyTo(payload, 1);
        return new NdefUriRecord(uri, payload);
    }

    internal static NdefUriRecord FromPayload(byte[] payload)
    {
        if (payload.Length < 1)
            throw new NfcException("URI record payload is empty.");

        var prefix = NdefUriPrefixes.Expand(payload[0]);
        var rest = Encoding.UTF8.GetString(payload, 1, payload.Length - 1);
        return new NdefUriRecord(prefix + rest, payload);
    }
}

/// <summary>One or more NDEF records.</summary>
public sealed class NdefMessage
{
    private NdefMessage(NdefRecord[] records) => Records = records;

    /// <summary>Records in on-tag order.</summary>
    public IReadOnlyList<NdefRecord> Records { get; }

    /// <summary>A message that owns <paramref name="records"/>.</summary>
    public static NdefMessage Of(params NdefRecord[] records)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(records);
        return new(records);
    }

    internal static NdefMessage FromParsed(NdefRecord[] records) => new(records);
}

internal static class NdefUriPrefixes
{
    private readonly struct Prefix(byte code, string value)
    {
        public byte Code { get; } = code;
        public string Value { get; } = value;
    }

    private static readonly Prefix[] Table = [
        new(0x01, "http://www."),
        new(0x02, "https://www."),
        new(0x03, "http://"),
        new(0x04, "https://"),
        new(0x05, "tel:"),
        new(0x06, "mailto:"),
        new(0x07, "ftp://anonymous:anonymous@"),
        new(0x08, "ftp://ftp."),
        new(0x09, "ftps://"),
        new(0x0A, "sftp://"),
        new(0x0B, "smb://"),
        new(0x0C, "nfs://"),
        new(0x0D, "ftp://"),
        new(0x0E, "dav://"),
        new(0x0F, "news:"),
        new(0x10, "telnet://"),
        new(0x11, "imap:"),
        new(0x12, "rtsp://"),
        new(0x13, "urn:"),
        new(0x14, "pop:"),
        new(0x15, "sip:"),
        new(0x16, "sips:"),
        new(0x17, "tftp:"),
        new(0x18, "btspp://"),
        new(0x19, "btl2cap://"),
        new(0x1A, "btgoep://"),
        new(0x1B, "tcpobex://"),
        new(0x1C, "irdaobex://"),
        new(0x1D, "file://"),
        new(0x1E, "urn:epc:id:"),
        new(0x1F, "urn:epc:tag:"),
        new(0x20, "urn:epc:pat:"),
        new(0x21, "urn:epc:raw:"),
        new(0x22, "urn:epc:"),
        new(0x23, "urn:nfc:")
    ];

    private static readonly Prefix[] ByLength = [.. Table.OrderByDescending(static p => p.Value.Length)];

    public static (byte Code, string Suffix) Split(string uri)
    {
        foreach (var prefix in ByLength) {
            if (uri.StartsWith(prefix.Value, StringComparison.Ordinal))
                return (prefix.Code, uri[prefix.Value.Length..]);
        }

        return (0x00, uri);
    }

    public static string Expand(byte code)
    {
        if (code == 0x00)
            return "";

        foreach (var prefix in Table) {
            if (prefix.Code == code)
                return prefix.Value;
        }

        throw new NfcException($"URI prefix code 0x{code:X2} is not in the NFC Forum table.");
    }
}
