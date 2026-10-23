using System.Text;

namespace Lyo.Json;

/// <summary>Byte helpers shared by the document libraries.</summary>
public static class DocumentBytes
{
    /// <summary>Decodes <paramref name="data" />, dropping a preamble that matches <paramref name="encoding" />.</summary>
    public static string Decode(Encoding encoding, ReadOnlySpan<byte> data) => JsonRuntime.Decode(encoding, data);
}