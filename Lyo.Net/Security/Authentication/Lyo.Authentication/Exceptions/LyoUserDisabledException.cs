using Lyo.Common.Core.Extensions;

namespace Lyo.Authentication.Exceptions;

/// <summary>Thrown when issuance is attempted for a user whose <c>disabled_timestamp</c> is set (Option C kill-switch).</summary>
public sealed class LyoUserDisabledException : Exception
{
    /// <summary>Disabled user's id.</summary>
    public Guid UserId { get; }

    /// <summary>Builds a new exception.</summary>
    public LyoUserDisabledException(Guid userId, string? reason)
        : base($"Lyo user '{userId}' is disabled{(reason.IsNullOrEmpty() ? "." : $": {reason}")}")
        => UserId = userId;
}