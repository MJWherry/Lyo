namespace Lyo.Sms;

/// <summary>Error codes used by SMS services.</summary>
public static class SmsErrorCodes
{
    /// <summary>Failed to build the SMS message.</summary>
    public const string BuildFailed = "BUILD_FAILED";

    /// <summary>Message was not built successfully.</summary>
    public const string MessageNotBuilt = "MESSAGE_NOT_BUILT";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "OPERATION_CANCELLED";

    /// <summary>From phone number is required but not provided.</summary>
    public const string MissingFromNumber = "MISSING_FROM_NUMBER";
}