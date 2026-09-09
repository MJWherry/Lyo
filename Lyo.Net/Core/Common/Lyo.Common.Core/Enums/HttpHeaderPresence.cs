using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Which HTTP message a header appears on. Combine <see cref="Request" /> and <see cref="Response" /> for headers that are valid on both.</summary>
[Flags]
public enum HttpHeaderPresence
{
    /// <summary>Presence is not known or not registered.</summary>
    [Description("None")]
    None = 0,

    /// <summary>The header is sent on requests.</summary>
    [Description("Request")]
    Request = 1 << 0,

    /// <summary>The header is sent on responses.</summary>
    [Description("Response")]
    Response = 1 << 1,

    /// <summary>The header is valid on both requests and responses.</summary>
    [Description("Both")]
    Both = Request | Response
}
