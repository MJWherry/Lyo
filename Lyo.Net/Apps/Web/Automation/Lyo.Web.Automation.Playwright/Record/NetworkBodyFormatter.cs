using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>Turns a captured HTTP body into the text or image the network popup shows.</summary>
internal static class NetworkBodyFormatter
{
    internal const int TextLimit = 65_536;

    internal const int ImageLimit = 400_000;

    internal static NetworkBody Format(string? contentType, byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return new("empty", null, null, false);

        var mime = Mime(contentType);
        if (IsRasterImage(mime))
            return ImageBody(mime, bytes);

        var text = Decode(bytes, out var truncated);
        if (text == null)
            return new("binary", bytes.Length + " bytes", null, false);

        if (IsJson(mime, text))
            return new("json", PrettyJson(text), null, truncated);

        if (IsXml(mime, text)) {
            var pretty = PrettyXml(text);
            if (IsSvg(mime) && bytes.Length <= ImageLimit)
                return new("image", pretty, DataUrl(mime ?? "image/svg+xml", bytes), truncated);

            return new(pretty == text && !mime.Contains("xml") ? "text" : "xml", pretty, null, truncated);
        }

        return new("text", text, null, truncated);
    }

    internal static NetworkBody FormatText(string? contentType, string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new("empty", null, null, false);

        return Format(contentType, Encoding.UTF8.GetBytes(text));
    }

    private static NetworkBody ImageBody(string? mime, byte[] bytes)
    {
        var type = string.IsNullOrWhiteSpace(mime) ? "application/octet-stream" : mime!;
        if (bytes.Length > ImageLimit)
            return new("image", bytes.Length + " bytes. Too large to preview.", null, true);

        return new("image", null, DataUrl(type, bytes), false);
    }

    private static string DataUrl(string mime, byte[] bytes) => "data:" + mime + ";base64," + Convert.ToBase64String(bytes);

    private static string? Decode(byte[] bytes, out bool truncated)
    {
        truncated = false;
        var sniff = Math.Min(bytes.Length, 512);
        for (var i = 0; i < sniff; i++) {
            if (bytes[i] == 0)
                return null;
        }

        var length = bytes.Length;
        if (length > TextLimit) {
            length = TextLimit;
            truncated = true;
        }

        var text = Encoding.UTF8.GetString(bytes, 0, length);
        if (text.Length > 0 && text[0] == '\uFEFF')
            text = text.Substring(1);

        return text;
    }

    private static string PrettyJson(string text)
    {
        try {
            using var document = JsonDocument.Parse(text);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                document.WriteTo(writer);

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException) {
            return text;
        }
    }

    private static string PrettyXml(string text)
    {
        try {
            return XDocument.Parse(text).ToString();
        }
        catch (System.Xml.XmlException) {
            return text;
        }
    }

    private static string Mime(string? contentType)
    {
        if (contentType is not string value || string.IsNullOrWhiteSpace(value))
            return "";

        var semi = value.IndexOf(';');
        var mime = (semi < 0 ? value : value.Substring(0, semi)).Trim();
        return mime.ToLowerInvariant();
    }

    private static bool IsRasterImage(string mime) => mime.StartsWith("image/", StringComparison.Ordinal) && !IsSvg(mime);

    private static bool IsSvg(string mime) => mime.IndexOf("svg", StringComparison.Ordinal) >= 0;

    private static bool IsJson(string mime, string text)
    {
        if (mime.IndexOf("json", StringComparison.Ordinal) >= 0)
            return true;

        if (mime.Length > 0 && mime != "text/plain")
            return false;

        var start = text.TrimStart();
        return start.StartsWith("{", StringComparison.Ordinal) || start.StartsWith("[", StringComparison.Ordinal);
    }

    private static bool IsXml(string mime, string text)
    {
        if (mime.IndexOf("xml", StringComparison.Ordinal) >= 0 || IsSvg(mime))
            return true;

        if (mime.Length > 0 && mime != "text/plain")
            return false;

        return text.TrimStart().StartsWith("<", StringComparison.Ordinal);
    }
}

/// <summary>Body prepared for the network popup. <see cref="Kind" /> is <c>json</c>, <c>xml</c>, <c>image</c>, <c>text</c>, <c>binary</c>, or <c>empty</c>.</summary>
internal sealed record NetworkBody(string Kind, string? Text, string? ImageDataUrl, bool Truncated);
