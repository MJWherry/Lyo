using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Email.Models;

/// <summary>Event arguments for connection test events.</summary>
/// <param name="IsSuccess">Indicates whether the connection test succeeded.</param>
/// <param name="ElapsedTime">The elapsed time for the connection test.</param>
/// <param name="Exception">The exception when the test failed; otherwise <see langword="null" />.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ConnectionTestedEventArgs(bool IsSuccess, TimeSpan ElapsedTime, Exception? Exception = null)
{
    /// <summary>Returns a readable summary of the connection test outcome.</summary>
    /// <returns>A string containing success state, elapsed time, and exception details.</returns>
    public override string ToString() => $"Success: {IsSuccess}, Elapsed: {ElapsedTime}, Exception: {Exception?.Message ?? "None"}";
}

/// <summary>Event arguments raised before an email send operation begins.</summary>
/// <param name="EmailRequest">The email request about to be sent.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailSendingEventArgs(EmailRequest EmailRequest)
{
    /// <summary>Returns a readable summary of the email request being sent.</summary>
    /// <returns>A string representation of the associated email request.</returns>
    public override string ToString() => EmailRequest.ToString();
}

/// <summary>Event arguments raised before a bulk email send operation begins.</summary>
/// <param name="BulkEmailMessage">The collection of email requests about to be sent.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailBulkSendingEventArgs(IReadOnlyList<EmailRequest> BulkEmailMessage)
{
    /// <summary>Returns a readable summary of the bulk email operation.</summary>
    /// <returns>A string containing the number of messages in the bulk operation.</returns>
    public override string ToString() => "Bulk Email Messages Count: " + BulkEmailMessage.Count;
}

/// <summary>Event arguments raised after a single email send operation completes.</summary>
/// <param name="EmailResult">The result of the email send operation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailSentEventArgs(Result<EmailRequest> EmailResult)
{
    /// <summary>Returns a readable summary of the email send result.</summary>
    /// <returns>A string representation of the associated send result.</returns>
    public override string ToString() => EmailResult.ToString();
}

/// <summary>Event arguments raised after a bulk email send operation completes.</summary>
/// <param name="BulkEmailResult">The aggregate result of the bulk email send operation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkEmailSentEventArgs(BulkResult<EmailRequest> BulkEmailResult)
{
    /// <summary>Returns a readable summary of the bulk email send result.</summary>
    /// <returns>A string representation of the associated bulk result.</returns>
    public override string ToString() => BulkEmailResult.ToString();
}