namespace Lyo.Common.Core.Enums;

/// <summary>HTTP status-code families used for grouping.</summary>
public enum HttpStatusCodeCategory
{
    /// <summary>Category is not known or not supported</summary>
    Unknown = 0,

    /// <summary>1xx informational responses</summary>
    Informational,

    /// <summary>2xx successful responses</summary>
    Success,

    /// <summary>3xx redirection responses</summary>
    Redirection,

    /// <summary>4xx client-error responses</summary>
    ClientError,

    /// <summary>5xx server-error responses</summary>
    ServerError
}