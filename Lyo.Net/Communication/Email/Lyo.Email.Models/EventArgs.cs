using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Email.Models;

/// <summary>Args for a connection-test event.</summary>
/// <param name="IsSuccess">Whether the probe succeeded.</param>
/// <param name="ElapsedTime">How long the probe took.</param>
/// <param name="Exception">Failure exception, or <see langword="null" /> on success.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ConnectionTestedEventArgs(bool IsSuccess, TimeSpan ElapsedTime, Exception? Exception = null)
{
    /// <summary>Readable summary of the probe outcome.</summary>
    /// <returns>Text covering success, elapsed time, and any exception.</returns>
    public override string ToString() => $"Success: {IsSuccess}, Elapsed: {ElapsedTime}, Exception: {Exception?.Message ?? "None"}";
}

/// <summary>Raised just before a single send starts.</summary>
/// <param name="EmailRequest">Request that is about to go out.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailSendingEventArgs(EmailRequest EmailRequest)
{
    /// <summary>Readable summary of the request being sent.</summary>
    /// <returns>String form of the associated request.</returns>
    public override string ToString() => EmailRequest.ToString();
}

/// <summary>Raised just before a bulk send starts.</summary>
/// <param name="BulkEmailMessage">Requests that are about to go out.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailBulkSendingEventArgs(IReadOnlyList<EmailRequest> BulkEmailMessage)
{
    /// <summary>Readable summary of the bulk send.</summary>
    /// <returns>Text that includes how many messages are in the batch.</returns>
    public override string ToString() => "Bulk Email Messages Count: " + BulkEmailMessage.Count;
}

/// <summary>Raised after a single send finishes.</summary>
/// <param name="EmailResult">Outcome of that send, including MessageId, SentDate, and SmtpResponse.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailSentEventArgs(EmailResult EmailResult)
{
    /// <summary>Readable summary of the send outcome.</summary>
    /// <returns>String form of the associated result.</returns>
    public override string ToString() => EmailResult.ToString();
}

/// <summary>Raised after a bulk send finishes.</summary>
    /// <param name="BulkEmailResult">Aggregate outcome of the batch.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkEmailSentEventArgs(BulkResult<EmailRequest> BulkEmailResult)
{
    /// <summary>Readable summary of the bulk outcome.</summary>
    /// <returns>String form of the associated bulk result.</returns>
    public override string ToString() => BulkEmailResult.ToString();
}