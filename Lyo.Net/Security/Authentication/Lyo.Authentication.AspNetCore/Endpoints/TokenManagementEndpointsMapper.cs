using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Opaque;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Authentication.AspNetCore.Endpoints;

/// <summary>Maps the per-user API-token management endpoints (<c>/tokens</c>) used by token-management UIs.</summary>
public static class TokenManagementEndpointsMapper
{
    /// <summary>The default scope required to mint a PAT or list/revoke your own tokens (<c>auth.tokens.write</c>).</summary>
    public const string WriteScope = "auth.tokens.write";

    /// <summary>The default scope required to read your own tokens (<c>auth.tokens.read</c>).</summary>
    public const string ReadScope = "auth.tokens.read";

    /// <summary>Per-kind privileged scope prefix. Issuing a token of kind <c>svc</c> requires <c>auth.tokens.write.svc</c> in addition to <see cref="WriteScope" />.</summary>
    public const string KindScopePrefix = "auth.tokens.write.";

    /// <summary>Kinds that may be issued through this endpoint. <see cref="ApiTokenKind.Internal" /> is server-issued only and is never exposed to user-facing UIs.</summary>
    public static readonly IReadOnlyList<string> IssuableKinds = [ApiTokenKind.Pat, ApiTokenKind.Svc, ApiTokenKind.Cli, ApiTokenKind.Webhook];

    /// <summary>Maps the token management endpoints at <paramref name="prefix" /> (default <c>/tokens</c>). All endpoints require an authenticated user.</summary>
    public static IEndpointConventionBuilder MapLyoTokenManagementEndpoints(this IEndpointRouteBuilder endpoints, string prefix = "/tokens")
    {
        ArgumentHelpers.ThrowIfNull(endpoints);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(prefix);
        var group = endpoints.MapGroup(prefix).WithTags("LyoTokens");
        group.MapGet("", ListAsync).WithName("LyoTokensList").RequireScope(ReadScope);
        group.MapGet("/kinds", KindsAsync).WithName("LyoTokensListKinds").RequireScope(ReadScope);
        group.MapPost("", CreateAsync).WithName("LyoTokensCreate").RequireScope(WriteScope);
        group.MapDelete("/{id}", RevokeAsync).WithName("LyoTokensRevoke").RequireScope(WriteScope);
        return group;
    }

    private static IResult KindsAsync(HttpContext ctx)
    {
        var callerScopes = ExtractScopes(ctx);
        var kinds = IssuableKinds.Select(k => new TokenKindDescriptor(k, DescribeKind(k), KindIsAllowed(k, callerScopes), k == ApiTokenKind.Pat ? WriteScope : KindScopePrefix + k))
            .ToArray();

        return Results.Json(kinds);
    }

    private static async Task<IResult> ListAsync(HttpContext ctx, IApiTokenStore store, bool? includeRevoked)
    {
        if (!TryResolveUser(ctx, out var userId))
            return Results.Unauthorized();

        var tokens = await store.ListForUserAsync(userId, includeRevoked ?? false, null, ctx.RequestAborted).ConfigureAwait(false);
        return Results.Json(tokens.Select(MapForDisplay).ToArray());
    }

    private static async Task<IResult> CreateAsync(HttpContext ctx, IApiTokenIssuer issuer, CreateTokenRequest? body)
    {
        if (body is null || body.DisplayName.IsNullOrWhitespace()) {
            return Results.Problem(
                "A display name is required to create a token.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request",
                extensions: new Dictionary<string, object?> { ["error"] = "missing_display_name" });
        }

        if (!TryResolveUser(ctx, out var userId))
            return Results.Unauthorized();

        var kind = NormalizeKind(body.Kind);
        if (!IssuableKinds.Contains(kind, StringComparer.Ordinal)) {
            return Results.Problem(
                $"Token kind '{kind}' is not supported.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request",
                extensions: new Dictionary<string, object?> { ["error"] = "unsupported_kind", ["supported"] = IssuableKinds });
        }

        var callerScopes = ExtractScopes(ctx);
        if (!KindIsAllowed(kind, callerScopes)) {
            return Results.Problem(
                $"Creating '{kind}' tokens requires the '{KindScopePrefix + kind}' scope.", statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
                extensions: new Dictionary<string, object?> { ["error"] = "kind_not_permitted", ["kind"] = kind, ["required_scope"] = KindScopePrefix + kind });
        }

        var requestedScopes = body.Scopes ?? [];
        var grantedScopes = requestedScopes.Where(callerScopes.Contains).ToArray();
        // Webhook tokens are signature-only and conventionally carry no scopes; for every other kind we require at least one grantable scope so the token does something.
        if (grantedScopes.Length == 0 && kind != ApiTokenKind.Webhook) {
            return Results.Problem(
                "None of the requested scopes can be granted by the caller.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request",
                extensions: new Dictionary<string, object?> { ["error"] = "no_grantable_scopes" });
        }

        var issued = await issuer.IssueAsync(
                new(kind, body.DisplayName, grantedScopes, userId, Lifetime: body.LifetimeSeconds is { } s ? TimeSpan.FromSeconds(s) : null, Metadata: body.Metadata),
                ctx.RequestAborted)
            .ConfigureAwait(false);

        return Results.Json(new CreateTokenResponse(issued.Plaintext, MapForDisplay(issued.Record)));
    }

    private static string NormalizeKind(string? raw) => raw.IsNullOrWhitespace() ? ApiTokenKind.Pat : raw.Trim().ToLowerInvariant();

    private static bool KindIsAllowed(string kind, ISet<string> callerScopes)
    {
        // PAT is unlocked by the base `auth.tokens.write` (already enforced by RequireScope on the endpoint).
        if (string.Equals(kind, ApiTokenKind.Pat, StringComparison.Ordinal))
            return true;

        return callerScopes.Contains(KindScopePrefix + kind);
    }

    private static string DescribeKind(string kind)
        => kind switch {
            ApiTokenKind.Pat => "Personal access token. Use from scripts and tools that act on your behalf.",
            ApiTokenKind.Svc => "Service token. Identifies an automated service running under your account.",
            ApiTokenKind.Cli => "CLI token. Long-lived credential for command-line tooling.",
            ApiTokenKind.Webhook => "Webhook signing token. Used by external systems to sign callbacks back to Lyo.",
            var _ => kind
        };

    private static async Task<IResult> RevokeAsync(string id, HttpContext ctx, IApiTokenStore store)
    {
        if (!TryResolveUser(ctx, out var userId))
            return Results.Unauthorized();

        var record = await store.GetByIdAsync(id, null, ctx.RequestAborted).ConfigureAwait(false);
        if (record is null)
            return Results.Problem($"Token '{id}' was not found.", statusCode: StatusCodes.Status404NotFound, title: "Not Found");

        if (record.UserId != userId)
            return Results.Forbid();

        await store.RevokeAsync(id, DateTime.UtcNow, "revoked_by_owner", null, ctx.RequestAborted).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static bool TryResolveUser(HttpContext ctx, out Guid userId)
    {
        userId = Guid.Empty;
        var claim = ctx.User.FindFirst("lyo:user")?.Value ?? ctx.User.FindFirst("sub")?.Value;
        if (claim.IsNullOrEmpty())
            return false;

        var raw = claim.StartsWith("lyo_user:", StringComparison.Ordinal) ? claim["lyo_user:".Length..] : claim;
        return Guid.TryParse(raw, out userId);
    }

    private static ISet<string> ExtractScopes(HttpContext ctx)
        => new HashSet<string>(ctx.User.FindAll("scope").SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)), StringComparer.Ordinal);

    private static TokenDisplay MapForDisplay(ApiTokenRecord record)
        => new(
            record.Id, record.Kind, record.Ring, record.DisplayName, record.Scopes.ToArray(), record.CreatedAt, record.ExpiresAt, record.LastUsedAt, record.RevokedAt,
            record.RevokedReason);

    /// <summary>The JSON body accepted by <c>POST /tokens</c>.</summary>
    public sealed record CreateTokenRequest(
        string? DisplayName,
        IReadOnlyList<string>? Scopes,
        int? LifetimeSeconds,
        IReadOnlyDictionary<string, object?>? Metadata,
        string? Kind = null);

    /// <summary>The JSON shape returned by <c>POST /tokens</c>.</summary>
    public sealed record CreateTokenResponse(string Plaintext, TokenDisplay Record);

    /// <summary>
    /// The JSON shape returned by <c>GET /tokens/kinds</c>: every kind the endpoint understands, paired with whether the caller is permitted to mint it and the scope they would
    /// need.
    /// </summary>
    public sealed record TokenKindDescriptor(string Kind, string Description, bool Allowed, string RequiredScope);

    /// <summary>The display-safe projection of an <see cref="ApiTokenRecord" />.</summary>
    public sealed record TokenDisplay(
        string Id,
        string Kind,
        string Ring,
        string DisplayName,
        string[] Scopes,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? LastUsedAt,
        DateTime? RevokedAt,
        string? RevokedReason);
}