using System;

namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>Thrown by the coordinator when an external login is rejected by the configured <see cref="ExternalLoginPolicy"/>. The wire-level response should be a generic 401.</summary>
public sealed class ExternalLoginRejectedException : Exception
{
    /// <summary>A stable, closed-taxonomy reason code (e.g. <c>OidcStateInvalid</c>, <c>EmailNotVerified</c>, <c>UserDisabled</c>, <c>UserNotProvisioned</c>). Suitable for audit logging and metrics; do not surface to end users.</summary>
    public string Reason { get; }

    /// <summary>Creates a new exception with a stable <paramref name="reason"/> and human-readable <paramref name="message"/>.</summary>
    public ExternalLoginRejectedException(string reason, string message)
        : base(message) =>
        Reason = reason;
}
