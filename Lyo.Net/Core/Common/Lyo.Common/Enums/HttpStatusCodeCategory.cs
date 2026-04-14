namespace Lyo.Common.Enums;

/// <summary>Represents HTTP status code categories for grouping status codes.</summary>
public enum HttpStatusCodeCategory
{
    /// <summary>Unknown or unsupported HTTP status code category</summary>
    Unknown = 0,

    /// <summary>Informational responses (1xx)</summary>
    Informational,

    /// <summary>Successful responses (2xx)</summary>
    Success,

    /// <summary>Redirection responses (3xx)</summary>
    Redirection,

    /// <summary>Client error responses (4xx)</summary>
    ClientError,

    /// <summary>Server error responses (5xx)</summary>
    ServerError
}