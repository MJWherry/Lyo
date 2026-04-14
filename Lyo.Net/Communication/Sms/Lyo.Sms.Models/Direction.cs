using Lyo.Common.Attributes;

namespace Lyo.Sms.Models;

public enum Direction
{
    Unknown,

    [StringValue("inbound")]
    Inbound, [StringValue("outbound-api")]
    OutboundApi, [StringValue("outbound-call")]
    OutboundCall, [StringValue("outbound-reply")]
    OutboundReply
}