using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Sms.Models;

/// <summary>Raised as an SMS is about to go out.</summary>
/// <param name="SmsRequest">Request that is about to be sent.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsSendingEventArgs(SmsRequest SmsRequest)
{
    /// <summary>Readable summary of the request being sent.</summary>
    /// <returns>String form of the associated request.</returns>
    public override string ToString() => SmsRequest.ToString();
}

/// <summary>Raised as a bulk SMS send is about to begin.</summary>
/// <param name="BulkSmsMessage">Requests that are about to go out.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsBulkSendingEventArgs(IReadOnlyList<SmsRequest> BulkSmsMessage)
{
    /// <summary>Readable summary of the bulk send.</summary>
    /// <returns>Text that includes how many messages are in the batch.</returns>
    public override string ToString() => "Bulk SMS Messages Count: " + BulkSmsMessage.Count;
}

/// <summary>Raised after an SMS send finishes.</summary>
/// <param name="SmsResult">Outcome of that send.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsSentEventArgs(Result<SmsRequest> SmsResult)
{
    /// <summary>Readable summary of the send outcome.</summary>
    /// <returns>String form of the associated result.</returns>
    public override string ToString() => SmsResult.ToString();
}

/// <summary>Raised after a bulk SMS send finishes.</summary>
/// <param name="BulkSmsResult">Aggregate outcome of the batch.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkSmsSentEventArgs(BulkResult<SmsRequest> BulkSmsResult)
{
    /// <summary>Readable summary of the bulk outcome.</summary>
    /// <returns>String form of the associated bulk result.</returns>
    public override string ToString() => BulkSmsResult.ToString();
}