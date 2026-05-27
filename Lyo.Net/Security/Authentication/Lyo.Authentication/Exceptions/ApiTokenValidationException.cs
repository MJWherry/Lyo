using System;

namespace Lyo.Authentication.Exceptions;

/// <summary>Thrown when a token cannot be validated and the caller has explicitly opted into exception-on-fail mode (the default path returns <c>null</c> instead).</summary>
public sealed class ApiTokenValidationException : Exception
{
    /// <summary>Categorical failure reason (short, stable code such as <c>UnknownToken</c>, <c>SecretMismatch</c>, <c>Revoked</c>). Safe to log and emit as an audit event; do not surface to end users.</summary>
    public string FailureReason { get; }

    /// <summary>Creates a new validation exception.</summary>
    /// <param name="failureReason">A short, stable code (e.g. <c>UnknownToken</c>, <c>SecretMismatch</c>, <c>Revoked</c>).</param>
    /// <param name="message">Operator-facing message. Never returned to the caller.</param>
    public ApiTokenValidationException(string failureReason, string message)
        : base(message) => FailureReason = failureReason;
}
