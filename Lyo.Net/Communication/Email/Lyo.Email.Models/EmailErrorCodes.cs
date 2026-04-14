namespace Lyo.Email.Models;

/// <summary>Error codes used by Email services.</summary>
public static class EmailErrorCodes
{
    /// <summary>Failed to send email.</summary>
    public const string SendFailed = "EMAIL_SEND_FAILED";

    /// <summary>Failed to build the email message.</summary>
    public const string BuildFailed = "EMAIL_BUILD_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "EMAIL_OPERATION_CANCELLED";
}