using Lyo.Authentication.Scopes;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Leaderboard;

/// <summary>Global JWT scopes for the Leaderboard API. Board roles stay in the membership table.</summary>
public static class LeaderboardScopes
{
    /// <summary>Read boards, standings, and approved entries the caller can see.</summary>
    public const string Read = "leaderboard.read";

    /// <summary>Create boards, submit entries, and invite/redeem. Implies <see cref="Read" />.</summary>
    public const string Write = "leaderboard.write";

    /// <summary>Platform-wide administration. Implies <see cref="Write" />.</summary>
    public const string Admin = "leaderboard.admin";

    /// <summary>Registers <see cref="Read" />, <see cref="Write" />, and <see cref="Admin" />. Call after <c>AddLyoAuthentication</c>.</summary>
    public static IServiceCollection AddLeaderboardScopes(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddScope(Read, "View leaderboards, standings, and approved entries.");
        services.AddScope(Write, "Create boards, submit entries, and manage invites.", Read);
        services.AddScope(Admin, "Platform-wide leaderboard administration.", Write);
        return services;
    }
}