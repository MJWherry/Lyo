namespace Lyo.Common.Enums;

/// <summary>Categorises the nature of an error to support consistent handling at boundaries (e.g. HTTP mapping).</summary>
public enum ErrorType
{
    /// <summary>General-purpose error with no specific category.</summary>
    Generic = 0,

    /// <summary>Input failed one or more validation rules.</summary>
    Validation = 1,

    /// <summary>The requested resource or entity could not be found.</summary>
    NotFound = 2,

    /// <summary>The operation conflicts with current state (e.g. duplicate, concurrency violation).</summary>
    Conflict = 3,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized = 4,

    /// <summary>The caller is authenticated but does not have permission.</summary>
    Forbidden = 5,

    /// <summary>An unexpected internal error occurred.</summary>
    InternalError = 6,

    /// <summary>The operation exceeded an allowed time limit.</summary>
    Timeout = 7,

    /// <summary>A downstream service or dependency is unavailable.</summary>
    ServiceUnavailable = 8
}
