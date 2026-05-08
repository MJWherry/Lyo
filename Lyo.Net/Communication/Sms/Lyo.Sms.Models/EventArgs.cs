using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Sms.Models;

/// <summary>Event arguments for SMS sending events.</summary>
/// <param name="SmsRequest">The SMS request about to be sent.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsSendingEventArgs(SmsRequest SmsRequest)
{
    /// <summary>Returns a readable summary of the SMS request being sent.</summary>
    /// <returns>A string representation of the associated SMS request.</returns>
    public override string ToString() => SmsRequest.ToString();
}

/// <summary>Event arguments for bulk SMS sending events.</summary>
/// <param name="BulkSmsMessage">The collection of SMS requests about to be sent.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsBulkSendingEventArgs(IReadOnlyList<SmsRequest> BulkSmsMessage)
{
    /// <summary>Returns a readable summary of the bulk SMS operation.</summary>
    /// <returns>A string containing the number of messages in the bulk operation.</returns>
    public override string ToString() => "Bulk SMS Messages Count: " + BulkSmsMessage.Count;
}

/// <summary>Event arguments for SMS sent events.</summary>
/// <param name="SmsResult">The result of the SMS send operation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsSentEventArgs(Result<SmsRequest> SmsResult)
{
    /// <summary>Returns a readable summary of the SMS send result.</summary>
    /// <returns>A string representation of the associated SMS send result.</returns>
    public override string ToString() => SmsResult.ToString();
}

/// <summary>Event arguments for bulk SMS sent events.</summary>
/// <param name="BulkSmsResult">The aggregate result of the bulk SMS send operation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkSmsSentEventArgs(BulkResult<SmsRequest> BulkSmsResult)
{
    /// <summary>Returns a readable summary of the bulk SMS send result.</summary>
    /// <returns>A string representation of the associated bulk result.</returns>
    public override string ToString() => BulkSmsResult.ToString();
}