using Lyo.Common.Extensions;

namespace Lyo.Authentication.Exceptions;

/// <summary>Thrown when an issuance is attempted for a user whose <c>disabled_timestamp</c> is set (Option C kill-switch).</summary>
public sealed class LyoUserDisabledException : Exception
{
    /// <summary>The disabled user's id.</summary>
    public Guid UserId { get; }

    /// <summary>Creates a new exception.</summary>
    public LyoUserDisabledException(Guid userId, string? reason)
        : base($"Lyo user '{userId}' is disabled{(reason.IsNullOrEmpty() ? "." : $": {reason}")}")
        => UserId = userId;
}