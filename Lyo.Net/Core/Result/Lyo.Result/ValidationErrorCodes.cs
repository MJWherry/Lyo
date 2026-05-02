namespace Lyo.Result;

/// <summary>Shared error codes for validation and common operation failures.</summary>
public static class ValidationErrorCodes
{
    // Validation
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string RequiredValue = "REQUIRED_VALUE";
    public const string NullValue = "NULL_VALUE";
    public const string DefaultValue = "DEFAULT_VALUE";
    public const string EmptyValue = "EMPTY_VALUE";
    public const string EmptyString = "EMPTY_STRING";
    public const string EmptyCollection = "EMPTY_COLLECTION";
    public const string WhitespaceString = "WHITESPACE_STRING";
    public const string InvalidLength = "INVALID_LENGTH";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string InvalidEmail = "INVALID_EMAIL";
    public const string InvalidPhone = "INVALID_PHONE";
    public const string InvalidUri = "INVALID_URI";
    public const string OutOfRange = "OUT_OF_RANGE";
    public const string InvalidZip = "INVALID_ZIP";
    public const string MissingItem = "MISSING_ITEM";
    public const string DisallowedItem = "DISALLOWED_ITEM";

    // Resource / operation
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";
    public const string Timeout = "TIMEOUT";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}