using System.Security.Claims;
using Lyo.Authentication.AspNetCore.Claims;
using Lyo.EntityReference.Models;

namespace Lyo.Comic.Api.Services;

/// <summary>Resolves the authenticated Lyo user as an <see cref="EntityRef" /> for enrichment (favorites, etc.).</summary>
internal static class ComicCallerRef
{
    /// <summary>Logical entity type stored on favorite/rating/comment actor rows for Lyo users.</summary>
    public const string EntityType = "LyoUser";

    /// <summary>Returns a caller ref from JWT claims, or null when the request is anonymous.</summary>
    public static EntityRef? From(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var raw = user.FindFirst(LyoClaims.LyoUser)?.Value ?? user.FindFirst(LyoClaims.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (raw.StartsWith("lyo_user:", StringComparison.Ordinal))
            raw = raw["lyo_user:".Length..];

        return Guid.TryParse(raw, out var id) ? EntityRef.ForGuid(EntityType, id) : null;
    }
}
