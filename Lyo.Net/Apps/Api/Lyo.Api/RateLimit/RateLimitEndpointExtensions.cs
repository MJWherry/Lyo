using System.Security.Claims;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.RateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api;

/// <summary>Opt-in quota for one minimal-API route. A route without this call is not limited.</summary>
public static class RateLimitEndpointExtensions
{
    /// <summary>
    /// Counts this route against <paramref name="policy" /> unless it is on <paramref name="whitelist" />. A denial throws
    /// <see cref="RateLimitExceededException" /> so <c>LoggingMiddleware</c> can write <c>Retry-After</c>. Runs as an endpoint filter, after authentication.
    /// </summary>
    /// <param name="builder">Route being limited.</param>
    /// <param name="policy">Quota name. Routes that share a name share one bucket.</param>
    /// <param name="permitLimit">Permits per <paramref name="window" />.</param>
    /// <param name="window">Restore period.</param>
    /// <param name="pattern">Window rule.</param>
    /// <param name="partition">Caller key. Null uses <see cref="RateLimitPartitions.Caller" />.</param>
    /// <param name="whitelist">Endpoint names or <c>METHOD /route</c> values that skip this policy.</param>
    public static RouteHandlerBuilder RequireRateLimit(
        this RouteHandlerBuilder builder,
        string policy,
        int permitLimit,
        TimeSpan window,
        RateLimitWindow pattern,
        Func<HttpContext, string>? partition = null,
        IReadOnlyCollection<string>? whitelist = null)
    {
        ArgumentHelpers.ThrowIfNull(builder);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(policy);
        ArgumentHelpers.ThrowIf(permitLimit < 1, "PermitLimit must be at least 1.");
        ArgumentHelpers.ThrowIf(window.TotalMilliseconds < 1, "Window must be at least 1ms.");
        var metadata = new RateLimitEndpointMetadata(policy, permitLimit, window, pattern, partition ?? RateLimitPartitions.Caller, whitelist);
        builder.WithMetadata(metadata);
        builder.AddEndpointFilter(async (context, next) => {
            var http = context.HttpContext;
            var endpoint = http.GetEndpoint();
            var name = endpoint?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
            var template = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
            if (RateLimitEndpointMatcher.IsWhitelisted(metadata.Whitelist, name, http.Request.Method, template, http.Request.Path.Value))
                return await next(context);

            var service = http.RequestServices.GetService<IRateLimitService>()
                ?? throw new InvalidOperationException("Rate limiting is enabled but IRateLimitService is not registered. Call AddLocalRateLimit or AddRedisRateLimit.");
            var key = metadata.Partition(http);
            var decision = await service.TryAcquireAsync(
                new RateLimitRequest(metadata.Policy, key, metadata.PermitLimit, metadata.Window, metadata.Pattern),
                http.RequestAborted);
            if (!decision.Allowed)
                throw new RateLimitExceededException(decision.RetryAfter, metadata.PermitLimit, metadata.Window);

            return await next(context);
        });
        return builder;
    }
}

/// <summary>Partition keys for an inbound quota. Empty claim values are skipped.</summary>
public static class RateLimitPartitions
{
    /// <summary>
    /// <c>lyo:user</c>, else <c>lyo:token_id</c>, else <c>sub</c>, else <see cref="ClaimTypes.NameIdentifier" />, else <see cref="ClaimsIdentity.Name" />,
    /// else the remote IP, else <c>anonymous</c>.
    /// </summary>
    public static string Caller(HttpContext http)
    {
        ArgumentHelpers.ThrowIfNull(http);
        var user = http.User;
        return First(user, "lyo:user")
            ?? First(user, "lyo:token_id")
            ?? First(user, "sub")
            ?? First(user, ClaimTypes.NameIdentifier)
            ?? NullIfEmpty(user.Identity?.Name)
            ?? http.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
    }

    private static string? First(ClaimsPrincipal user, string claimType) => NullIfEmpty(user.FindFirst(claimType)?.Value);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

/// <summary>Quota attached to one route.</summary>
public sealed class RateLimitEndpointMetadata(
    string policy,
    int permitLimit,
    TimeSpan window,
    RateLimitWindow pattern,
    Func<HttpContext, string> partition,
    IReadOnlyCollection<string>? whitelist)
{
    /// <summary>Quota name.</summary>
    public string Policy { get; } = policy;

    /// <summary>Permits per <see cref="Window" />.</summary>
    public int PermitLimit { get; } = permitLimit;

    /// <summary>Restore period.</summary>
    public TimeSpan Window { get; } = window;

    /// <summary>Window rule.</summary>
    public RateLimitWindow Pattern { get; } = pattern;

    /// <summary>Caller key for this route.</summary>
    public Func<HttpContext, string> Partition { get; } = partition;

    /// <summary>Endpoints that skip <see cref="Policy" />.</summary>
    public IReadOnlyCollection<string>? Whitelist { get; } = whitelist;
}
