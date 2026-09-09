namespace Lyo.Resilience;

/// <summary>Thrown internally when a result fails the success predicate, which starts a retry.</summary>
/// <remarks>Default pipelines handle this. Application code should not catch or handle it.</remarks>
public sealed class RetryableResultException : Exception
{
    /// <summary>Mints a retryable result exception.</summary>
    public RetryableResultException() { }

    /// <summary>Mints a retryable result exception with a message.</summary>
    public RetryableResultException(string message)
        : base(message) { }

    /// <summary>Mints a retryable result exception with a message and an inner exception.</summary>
    public RetryableResultException(string message, Exception inner)
        : base(message, inner) { }
}