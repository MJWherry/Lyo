using System.Diagnostics;
using Lyo.Common;

namespace Lyo.Sms.Models;

/// <summary>Event arguments for SMS sending events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsSendingEventArgs(SmsRequest SmsRequest)
{
    public override string ToString() => SmsRequest.ToString();
}

/// <summary>Event arguments for bulk SMS sending events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsBulkSendingEventArgs(IReadOnlyList<SmsRequest> BulkSmsMessage)
{
    public override string ToString() => "Bulk SMS Messages Count: " + BulkSmsMessage.Count;
}

/// <summary>Event arguments for SMS sent events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsSentEventArgs(Result<SmsRequest> SmsResult)
{
    public override string ToString() => SmsResult.ToString();
}

/// <summary>Event arguments for bulk SMS sent events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkSmsSentEventArgs(BulkResult<SmsRequest> BulkSmsResult)
{
    public override string ToString() => BulkSmsResult.ToString();
}