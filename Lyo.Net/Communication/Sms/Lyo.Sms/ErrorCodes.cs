namespace Lyo.Sms;

/// <summary>Stable error codes emitted by SMS services.</summary>
public static class SmsErrorCodes
{
    /// <summary>The SMS message could not be constructed.</summary>
    public const string BuildFailed = "BUILD_FAILED";

    /// <summary>Build finished without a usable message.</summary>
    public const string MessageNotBuilt = "MESSAGE_NOT_BUILT";

    /// <summary>The operation was cancelled before it finished.</summary>
    public const string OperationCancelled = "OPERATION_CANCELLED";

    /// <summary>A From phone number is required and was not supplied.</summary>
    public const string MissingFromNumber = "MISSING_FROM_NUMBER";
}