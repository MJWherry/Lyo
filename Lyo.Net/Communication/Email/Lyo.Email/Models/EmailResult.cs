using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Email.Models;

/// <summary>Outcome of a send, including fields unique to email.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailResult : Result<EmailRequest>
{
    /// <summary>Provider message id, if the provider supplied one. SMTP uses the MIME Message-Id. SendGrid uses the X-Message-Id header.</summary>
    public string? MessageId { get; init; }

    /// <summary>UTC send time, if known.</summary>
    public DateTime? SentDate { get; init; }

    /// <summary>Provider reply. SMTP stores the SendAsync response string. SendGrid stores the status code and response body.</summary>
    public string? ProviderResponse { get; init; }

    private EmailResult(bool isSuccess, EmailRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>String form of the send outcome.</summary>
    /// <returns>Text that includes the message id and base result details.</returns>
    public override string ToString() => $"{MessageId} - {base.ToString()}";

    /// <summary>Builds a successful outcome.</summary>
    /// <param name="request">Request that was sent. A null From on this request stays null.</param>
    /// <param name="providerResponse">Provider reply text.</param>
    /// <param name="messageId">Optional provider message id.</param>
    /// <param name="sentDate">Optional UTC time the message left.</param>
    /// <returns>A successful <see cref="EmailResult" />.</returns>
    public static EmailResult FromSuccess(EmailRequest request, string providerResponse, string? messageId = null, DateTime? sentDate = null)
        => new(true, request) { MessageId = messageId, SentDate = sentDate, ProviderResponse = providerResponse };

    /// <summary>Builds a failed outcome from an exception.</summary>
    /// <param name="exception">Error that stopped the send.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="errorCode">Optional code to attach to the result.</param>
    /// <returns>A failed <see cref="EmailResult" />.</returns>
    public static EmailResult FromException(Exception exception, EmailRequest request, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Builds a failed outcome with a supplied message.</summary>
    /// <param name="errorMessage">Message suitable for callers.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="exception">Optional related exception. When set, the exception is the error and <paramref name="errorMessage" /> is not stored.</param>
    /// <returns>A failed <see cref="EmailResult" />.</returns>
    public static EmailResult FromError(string errorMessage, EmailRequest request, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception) : new(errorMessage, EmailErrorCodes.SendFailed);
        return new(false, request, [error]);
    }
}
