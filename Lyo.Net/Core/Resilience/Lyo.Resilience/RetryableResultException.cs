namespace Lyo.Resilience;

/// <summary>Thrown internally when a result fails the success condition, triggering a retry.</summary>
/// <remarks>Handled by default pipelines. Do not catch or handle this exception in application code.</remarks>
public sealed class RetryableResultException : Exception
{
    /// <summary>Creates a new retryable result exception.</summary>
    public RetryableResultException() { }

    /// <summary>Creates a new retryable result exception with a message.</summary>
    public RetryableResultException(string message)
        : base(message) { }

    /// <summary>Creates a new retryable result exception with a message and inner exception.</summary>
    public RetryableResultException(string message, Exception inner)
        : base(message, inner) { }
}