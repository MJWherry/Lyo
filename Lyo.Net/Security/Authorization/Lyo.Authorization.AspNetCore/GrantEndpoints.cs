using System.Security.Claims;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authorization.AspNetCore;

/// <summary>
/// Grant HTTP API for browser clients. Reads <c>lyo:user</c> or else <c>sub</c> from the authenticated principal. Does not parse tokens and does not reference
/// Lyo.Authentication.
/// </summary>
public static class GrantEndpoints
{
    /// <summary>Policy a host registers when save, revoke, and cross-subject reads should be limited.</summary>
    public const string ManagePolicyName = "authorization.grants.manage";

    /// <summary>
    /// Maps list, save, revoke, and check under <paramref name="prefix" />. Every route requires an authenticated user. When <paramref name="managePolicy" /> is set, save,
    /// revoke, listing another subject, and checking another subject require that policy. Omitting the subject on list returns every grant when the caller may manage, and only the
    /// caller's grants otherwise.
    /// </summary>
    public static IEndpointRouteBuilder MapGrantEndpoints(this IEndpointRouteBuilder endpoints, string? managePolicy = null, string prefix = "/authorization/grants")
    {
        ArgumentHelpers.ThrowIfNull(endpoints);
        var group = endpoints.MapGroup(prefix).RequireAuthorization();
        group.MapGet(
            "",
            (HttpContext http, IGrantStore store, string? subject, bool? includeRevoked, bool? includeExpired, int? take, CancellationToken ct) => ListAsync(
                http, store, managePolicy, subject, includeRevoked ?? false, includeExpired ?? false, take, ct));

        group.MapPost(
            "", (HttpContext http, GrantWrite write, IResourceAuthorization authorization, CancellationToken ct) => SaveAsync(http, write, authorization, managePolicy, ct));

        group.MapPost(
            "/{id:guid}/revoke", (HttpContext http, Guid id, IResourceAuthorization authorization, CancellationToken ct) => RevokeAsync(http, id, authorization, managePolicy, ct));

        group.MapPost(
            "/check", (HttpContext http, GrantCheck check, IResourceAuthorization authorization, CancellationToken ct) => CheckAsync(http, check, authorization, managePolicy, ct));

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext http,
        IGrantStore store,
        string? managePolicy,
        string? subject,
        bool includeRevoked,
        bool includeExpired,
        int? take,
        CancellationToken ct)
    {
        var caller = Caller(http.User);
        if (caller is null)
            return Results.BadRequest("authorization_subject_required");

        string? filter;
        if (string.IsNullOrWhiteSpace(subject))
            filter = await CanManageAsync(http, managePolicy) ? null : caller;
        else {
            filter = subject.Trim();
            if (!string.Equals(filter, caller, StringComparison.Ordinal) && !await CanManageAsync(http, managePolicy))
                return Results.Forbid();
        }

        var rows = await store.ListAsync(
                new() {
                    Subject = filter,
                    IncludeRevoked = includeRevoked,
                    IncludeExpired = includeExpired,
                    Take = take ?? GrantListQuery.DefaultTake
                }, ct)
            .ConfigureAwait(false);

        return Results.Ok(rows);
    }

    private static async Task<IResult> SaveAsync(HttpContext http, GrantWrite write, IResourceAuthorization authorization, string? managePolicy, CancellationToken ct)
    {
        if (!await CanManageAsync(http, managePolicy))
            return Results.Forbid();

        try {
            var saved = await authorization.SaveAsync(write, ct).ConfigureAwait(false);
            return Results.Ok(saved);
        }
        catch (Exception ex) {
            return Failure(ex);
        }
    }

    private static async Task<IResult> RevokeAsync(HttpContext http, Guid id, IResourceAuthorization authorization, string? managePolicy, CancellationToken ct)
    {
        if (!await CanManageAsync(http, managePolicy))
            return Results.Forbid();

        try {
            var revoked = await authorization.RevokeAsync(id, ct).ConfigureAwait(false);
            return Results.Ok(revoked);
        }
        catch (Exception ex) {
            return Failure(ex);
        }
    }

    private static async Task<IResult> CheckAsync(HttpContext http, GrantCheck check, IResourceAuthorization authorization, string? managePolicy, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(check);
        var caller = Caller(http.User);
        if (caller is null)
            return Results.BadRequest("authorization_subject_required");

        if (!string.Equals(check.Subject?.Trim(), caller, StringComparison.Ordinal) && !await CanManageAsync(http, managePolicy))
            return Results.Forbid();

        try {
            var allowed = await authorization.IsAllowedAsync(check, ct).ConfigureAwait(false);
            return Results.Ok(new GrantCheckResponse(allowed));
        }
        catch (ArgumentException ex) {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception) {
            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<bool> CanManageAsync(HttpContext http, string? managePolicy)
    {
        if (string.IsNullOrWhiteSpace(managePolicy))
            return true;

        var authorization = http.RequestServices.GetRequiredService<IAuthorizationService>();
        var result = await authorization.AuthorizeAsync(http.User, managePolicy).ConfigureAwait(false);
        return result.Succeeded;
    }

    private static string? Caller(ClaimsPrincipal user)
    {
        var lyoUser = user.FindFirst("lyo:user")?.Value;
        if (!string.IsNullOrWhiteSpace(lyoUser))
            return lyoUser;

        var sub = user.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(sub) ? null : sub;
    }

    private static IResult Failure(Exception ex)
        => ex switch {
            ConflictException conflict => Results.Conflict(conflict.Message),
            NotFoundException missing => Results.NotFound(missing.Message),
            ArgumentException invalid => Results.BadRequest(invalid.Message),
            var _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };

    /// <summary>Check response. A store failure is not returned as <c>allowed: false</c>.</summary>
    public sealed record GrantCheckResponse(bool Allowed);
}