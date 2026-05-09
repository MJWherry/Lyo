using System.Text;

namespace Lyo.QRCode.Payloads;

/// <summary>vCard 3.0 TEXT property escaping (<c>\</c>, <c>,</c>, <c>;</c>, newlines).</summary>
public static class QrVCardTextEscape
{
    /// <summary>Escapes a vCard 3.0 TEXT value.</summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value) {
            switch (c) {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case ',':
                    sb.Append(@"\,");
                    break;
                case ';':
                    sb.Append(@"\;");
                    break;
                case '\n':
                    sb.Append(@"\n");
                    break;
                case '\r':
                    break; // skip CR; pair handled as part of CRLF if present
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
