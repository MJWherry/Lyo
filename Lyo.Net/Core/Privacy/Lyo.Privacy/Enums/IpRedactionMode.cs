namespace Lyo.Privacy.Enums;

public enum IpRedactionMode
{
    Full,

    /// <summary>IPv4: zero the last octet. IPv6: not fully supported; full redact.</summary>
    TruncateLastSegment
}