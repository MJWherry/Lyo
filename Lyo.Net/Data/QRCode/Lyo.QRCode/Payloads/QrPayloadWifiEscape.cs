using System.Text;
using Lyo.Exceptions;

namespace Lyo.QRCode.Payloads;

/// <summary>Escapes <c>S</c> and <c>P</c> values in <c>WIFI:</c> strings per common Android/iOS rules (<c>\</c>, <c>;</c>, <c>,</c>, <c>"</c>).</summary>
public static class QrPayloadWifiEscape
{
    /// <summary>Escapes characters that would break <c>WIFI:</c> field parsing.</summary>
    public static string EscapeFieldValue(string value)
    {
        ArgumentHelpers.ThrowIfNull(value);
        if (value.Length == 0)
            return value;

        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value) {
            switch (c) {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case ';':
                    sb.Append(@"\;");
                    break;
                case ',':
                    sb.Append(@"\,");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
