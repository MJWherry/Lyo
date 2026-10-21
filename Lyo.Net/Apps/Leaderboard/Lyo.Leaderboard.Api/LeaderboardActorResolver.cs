using System.Security.Claims;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Users;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Leaderboard.Api.Models;
using Microsoft.AspNetCore.Http;

namespace Lyo.Leaderboard.Api;

internal static class LeaderboardActorResolver
{
    public static async Task<LeaderboardActor> ResolveAsync(HttpContext http, IUserStore users, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(users);
        var user = http.User;
        var raw = user.FindFirst(LyoJwtClaims.LyoUser)?.Value ?? user.FindFirst(LyoJwtClaims.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(raw)) {
            if (user.Identity?.IsAuthenticated == true)
                throw new UnauthorizedException("Missing lyo:user claim.");

            return ResolveAnonymous(http);
        }

        if (raw.StartsWith("lyo_user:", StringComparison.Ordinal))
            raw = raw["lyo_user:".Length..];

        if (!Guid.TryParse(raw, out var userId))
            throw new UnauthorizedException("lyo:user is not a valid user id.");

        var stored = await users.GetByIdAsync(userId, null, ct).ConfigureAwait(false);
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? stored?.Email;
        var name = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value ?? stored?.DisplayName;
        return new(userId, email, name);
    }

    private static LeaderboardActor ResolveAnonymous(HttpContext http)
    {
        var mode = http.Request.Headers[LeaderboardAnonymousActor.HeaderName].ToString();
        if (string.Equals(mode, LeaderboardAnonymousActor.Guest, StringComparison.OrdinalIgnoreCase))
            return LeaderboardAnonymousActor.GuestActor();

        return LeaderboardAnonymousActor.Workbench();
    }
}