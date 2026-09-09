namespace Lyo.Email.Models;

/// <summary>Stable error codes emitted by Email services.</summary>
public static class EmailErrorCodes
{
    /// <summary>The email could not be sent.</summary>
    public const string SendFailed = "EMAIL_SEND_FAILED";

    /// <summary>The email message could not be constructed.</summary>
    public const string BuildFailed = "EMAIL_BUILD_FAILED";

    /// <summary>The operation was cancelled before it finished.</summary>
    public const string OperationCancelled = "EMAIL_OPERATION_CANCELLED";
}