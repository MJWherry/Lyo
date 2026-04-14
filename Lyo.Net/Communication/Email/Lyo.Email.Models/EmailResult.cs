using Lyo.Common;

namespace Lyo.Email.Models;

/// <summary>Result of an email send operation with email-specific properties.</summary>
public sealed record EmailResult : Result<EmailRequest>
{
    public string? MessageId { get; init; }

    public DateTime? SentDate { get; init; }

    public string? SmtpResponse { get; init; }

    private EmailResult(bool isSuccess, EmailRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful EmailResult from SMTP response data.</summary>
    public static EmailResult FromSuccess(EmailRequest request, string smtpResponse, string? messageId = null, DateTime? sentDate = null)
        => new(true, request) { MessageId = messageId, SentDate = sentDate, SmtpResponse = smtpResponse };

    /// <summary>Creates a failed EmailResult from an exception.</summary>
    public static EmailResult FromException(Exception exception, EmailRequest request, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failed EmailResult with a custom error message.</summary>
    public static EmailResult FromError(string errorMessage, EmailRequest request, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception) : new(errorMessage, EmailErrorCodes.SendFailed);
        return new(false, request, [error]);
    }
}