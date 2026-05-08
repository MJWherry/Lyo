using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Email.Models;

/// <summary>Result of an email send operation with email-specific properties.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailResult : Result<EmailRequest>
{
    /// <summary>Gets the provider-assigned message identifier, when available.</summary>
    public string? MessageId { get; init; }

    /// <summary>Gets the UTC timestamp when the message was sent, when available.</summary>
    public DateTime? SentDate { get; init; }

    /// <summary>Gets the raw SMTP response returned by the provider, when available.</summary>
    public string? SmtpResponse { get; init; }

    private EmailResult(bool isSuccess, EmailRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Returns a string representation of the email operation result.</summary>
    /// <returns>A string containing message identifier and base result details.</returns>
    public override string ToString() => $"{MessageId} - {base.ToString()}";

    /// <summary>Creates a successful email result from SMTP response data.</summary>
    /// <param name="request">The email request that was sent.</param>
    /// <param name="smtpResponse">The provider response returned by SMTP.</param>
    /// <param name="messageId">Optional provider message identifier.</param>
    /// <param name="sentDate">Optional UTC timestamp when the message was sent.</param>
    /// <returns>A successful <see cref="EmailResult" /> instance.</returns>
    public static EmailResult FromSuccess(EmailRequest request, string smtpResponse, string? messageId = null, DateTime? sentDate = null)
        => new(true, request) { MessageId = messageId, SentDate = sentDate, SmtpResponse = smtpResponse };

    /// <summary>Creates a failed email result from an exception.</summary>
    /// <param name="exception">The exception that caused the send failure.</param>
    /// <param name="request">The email request associated with the failure.</param>
    /// <param name="errorCode">Optional error code to include in the result.</param>
    /// <returns>A failed <see cref="EmailResult" /> instance.</returns>
    public static EmailResult FromException(Exception exception, EmailRequest request, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failed email result with a custom error message.</summary>
    /// <param name="errorMessage">The user-facing error message.</param>
    /// <param name="request">The email request associated with the failure.</param>
    /// <param name="exception">Optional exception associated with the failure.</param>
    /// <returns>A failed <see cref="EmailResult" /> instance.</returns>
    public static EmailResult FromError(string errorMessage, EmailRequest request, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception) : new(errorMessage, EmailErrorCodes.SendFailed);
        return new(false, request, [error]);
    }
}