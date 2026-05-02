using System.Net;
using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

public sealed class IpAddressRedactionRule(IpRedactionMode mode = IpRedactionMode.Full) : IRedactionRule
{
    private static readonly Regex Ipv4 = new(@"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Simplified IPv6 (incl. compressed ::).</summary>
    private static readonly Regex Ipv6 = new(
        @"\b(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}\b|\b(?:[0-9a-f]{1,4}:){1,7}:\b|\b(?:[0-9a-f]{1,4}:){0,6}::(?:[0-9a-f]{1,4}:){0,6}[0-9a-f]{1,4}\b|\b::1\b|\b::\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public IpRedactionMode Mode { get; } = mode;

    public RedactionKind Kind => RedactionKind.IpAddress;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in Ipv4.Matches(input)) {
            if (!m.Success)
                continue;

            if (Mode == IpRedactionMode.TruncateLastSegment && IPAddress.TryParse(m.Value, out var _)) {
                var lastDot = m.Value.LastIndexOf('.');
                if (lastDot > 0)
                    yield return new(m.Index + lastDot + 1, m.Length - lastDot - 1, Kind);

                continue;
            }

            yield return new(m.Index, m.Length, Kind);
        }

        foreach (Match m in Ipv6.Matches(input)) {
            if (!m.Success)
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }
}