using System.Diagnostics;
using Lyo.Common;

namespace Lyo.Email.Models;

/// <summary>Event arguments for connection test events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ConnectionTestedEventArgs(bool IsSuccess, TimeSpan ElapsedTime, Exception? Exception = null)
{
    public override string ToString() => $"Success: {IsSuccess}, Elapsed: {ElapsedTime}, Exception: {Exception?.Message ?? "None"}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailSendingEventArgs(EmailRequest EmailRequest)
{
    public override string ToString() => EmailRequest.ToString();
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailBulkSendingEventArgs(IReadOnlyList<EmailRequest> BulkEmailMessage)
{
    public override string ToString() => "Bulk Email Messages Count: " + BulkEmailMessage.Count;
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailSentEventArgs(Result<EmailRequest> EmailResult)
{
    public override string ToString() => EmailResult.ToString();
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkEmailSentEventArgs(BulkResult<EmailRequest> BulkEmailResult)
{
    public override string ToString() => BulkEmailResult.ToString();
}