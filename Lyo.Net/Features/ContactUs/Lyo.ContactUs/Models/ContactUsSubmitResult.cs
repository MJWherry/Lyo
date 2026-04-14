using Lyo.Common;

namespace Lyo.ContactUs.Models;

/// <summary>Result of a contact form submission.</summary>
public sealed record ContactUsSubmitResult : Result<Guid?>
{
    /// <summary>The ID of the submitted contact form entry (when successful).</summary>
    public Guid? SubmissionId { get; init; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private ContactUsSubmitResult(bool isSuccess, Guid? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful ContactUsSubmitResult.</summary>
    public static ContactUsSubmitResult FromSuccess(Guid submissionId, string? message = null)
        => new(true, submissionId) { SubmissionId = submissionId, Message = message ?? "Thank you for your message. We will get back to you soon." };

    /// <summary>Creates a failed ContactUsSubmitResult from an exception.</summary>
    public static ContactUsSubmitResult FromException(Exception exception, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]);
    }

    /// <summary>Creates a failed ContactUsSubmitResult with a custom error message.</summary>
    public static ContactUsSubmitResult FromError(string errorMessage, string errorCode, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]);
    }
}