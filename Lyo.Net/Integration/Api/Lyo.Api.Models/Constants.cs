namespace Lyo.Api.Models;

/// <summary>Shared API constants.</summary>
public static class Constants
{
    /// <summary>Stable string codes for <c>errors[].code</c> on problem responses (formerly <c>ErrorCodeEnum</c>). Values match legacy JSON enum member names.</summary>
    public static class ApiErrorCodes
    {
        public const string Unknown = "Unknown";
        public const string InvalidOperation = "InvalidOperation";
        public const string InvalidRequest = "InvalidRequest";
        public const string InvalidQuery = "InvalidQuery";
        public const string InvalidField = "InvalidField";
        public const string InvalidSelectField = "InvalidSelectField";
        public const string InvalidComputedField = "InvalidComputedField";
        public const string InvalidInclude = "InvalidInclude";
        public const string InvalidSortByField = "InvalidSortByField";
        public const string InvalidWhereField = "InvalidWhereField";
        public const string InvalidPaging = "InvalidPaging";
        public const string InvalidCreateRequest = "InvalidCreateRequest";
        public const string InvalidUpdateRequest = "InvalidUpdateRequest";
        public const string InvalidUpsertRequest = "InvalidUpsertRequest";
        public const string InvalidPatchRequest = "InvalidPatchRequest";
        public const string InvalidDeleteRequest = "InvalidDeleteRequest";
        public const string SqlException = "SqlException";
        public const string Cancelled = "Cancelled";
        public const string NotFound = "NotFound";
        public const string Forbidden = "Forbidden";
        public const string ExceedMaxBulkSize = "ExceedMaxBulkSize";
        public const string MessageQueueConnectionIssue = "MessageQueueConnectionIssue";
    }
}
