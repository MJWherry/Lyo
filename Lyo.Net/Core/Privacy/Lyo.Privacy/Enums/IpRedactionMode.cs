namespace Lyo.Privacy.Enums;

public enum IpRedactionMode
{
    Full,

    /// <summary>IPv4: zero last octet. IPv6: not fully supported; full redact.</summary>
    TruncateLastSegment
}