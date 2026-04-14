using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>
/// Optional per-patch rules: which JSON property names may be updated for the current user. Pair with
/// <see cref="EndpointAuth"/> on the same endpoint for typical APIs. When <see cref="Custom"/> is set, it replaces
/// <see cref="PolicyAllowedProperties"/>. Otherwise, allowed names are the union of property sets from each
/// <see cref="PolicyAllowedProperties"/> entry whose policy passes <see cref="IAuthorizationService.AuthorizeAsync"/>.
/// Use <c>"*"</c> in a policy's set to allow every key present in <see cref="PatchRequest.Properties"/> when that policy succeeds.
/// </summary>
public sealed record PatchPropertyAuthorization
{
    /// <summary>Maps ASP.NET Core policy name to allowed CLR property names (case-insensitive). Ignored when <see cref="Custom"/> is set.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>>? PolicyAllowedProperties { get; init; }

    /// <summary>When set, runs instead of <see cref="PolicyAllowedProperties"/>.</summary>
    public Func<HttpContext, Type, PatchRequest, CancellationToken, ValueTask<PatchPropertyAuthorizationResult>>? Custom { get; init; }

    /// <summary>Fluent: policy name to allowed property names (or <c>"*"</c>).</summary>
    public static PatchPropertyAuthorization ForPolicies(Action<PatchPropertyAuthorizationBuilder> configure)
    {
        var b = new PatchPropertyAuthorizationBuilder();
        configure(b);
        return b.Build();
    }
}

/// <summary>Fluent builder for <see cref="PatchPropertyAuthorization"/> policy maps.</summary>
public sealed class PatchPropertyAuthorizationBuilder
{
    private readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.Ordinal);

    /// <summary>Adds or merges allowed property names for the given policy.</summary>
    public PatchPropertyAuthorizationBuilder AllowPropertiesForPolicy(string policyName, params string[] propertyNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        if (!_map.TryGetValue(policyName, out var set)) {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _map[policyName] = set;
        }

        foreach (var p in propertyNames)
            set.Add(p);

        return this;
    }

    internal PatchPropertyAuthorization Build()
        => new() {
            PolicyAllowedProperties = _map.ToDictionary(
                static kvp => kvp.Key,
                static kvp => (IReadOnlySet<string>)kvp.Value,
                StringComparer.Ordinal)
        };
}

/// <summary>Result of applying <see cref="PatchPropertyAuthorization"/>.</summary>
public readonly struct PatchPropertyAuthorizationResult
{
    private PatchPropertyAuthorizationResult(bool success, PatchRequest? request, LyoProblemDetails? error)
    {
        Success = success;
        Request = request;
        Error = error;
    }

    public bool Success { get; }

    public PatchRequest? Request { get; }

    public LyoProblemDetails? Error { get; }

    public static PatchPropertyAuthorizationResult Ok(PatchRequest request) => new(true, request, null);

    public static PatchPropertyAuthorizationResult Forbidden(LyoProblemDetails error) => new(false, null, error);
}

/// <summary>Applies <see cref="PatchPropertyAuthorization"/> to a patch request (typed and dynamic endpoints).</summary>
public static class PatchPropertyAuthorizationApplier
{
    private const string Wildcard = "*";

    /// <summary>No rules or empty rules: returns the original request unchanged.</summary>
    public static ValueTask<PatchPropertyAuthorizationResult> ApplyAsync(
        PatchPropertyAuthorization? authorization,
        HttpContext httpContext,
        Type entityType,
        PatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (authorization == null || IsNoOp(authorization))
            return ValueTask.FromResult(PatchPropertyAuthorizationResult.Ok(request));

        if (authorization.Custom != null)
            return authorization.Custom(httpContext, entityType, request, cancellationToken);

        return ApplyPolicyMapAsync(authorization, httpContext, request, cancellationToken);
    }

    private static bool IsNoOp(PatchPropertyAuthorization authorization)
        => authorization.Custom == null && (authorization.PolicyAllowedProperties == null || authorization.PolicyAllowedProperties.Count == 0);

    private static async ValueTask<PatchPropertyAuthorizationResult> ApplyPolicyMapAsync(
        PatchPropertyAuthorization authorization,
        HttpContext httpContext,
        PatchRequest request,
        CancellationToken cancellationToken)
    {
        var map = authorization.PolicyAllowedProperties!;
        var authz = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var user = httpContext.User;

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (policyName, propertySet) in map) {
            var result = await authz.AuthorizeAsync(user, policyName).ConfigureAwait(false);
            if (!result.Succeeded)
                continue;

            if (propertySet.Contains(Wildcard)) {
                foreach (var key in request.Properties.Keys)
                    allowed.Add(key);

                continue;
            }

            foreach (var p in propertySet)
                allowed.Add(p);
        }

        var forbidden = new List<string>();
        foreach (var key in request.Properties.Keys) {
            if (!allowed.Contains(key))
                forbidden.Add(key);
        }

        if (forbidden.Count == 0)
            return PatchPropertyAuthorizationResult.Ok(request);

        forbidden.Sort(StringComparer.OrdinalIgnoreCase);
        var detail = $"Not allowed to patch the following properties: {string.Join(", ", forbidden)}.";
        var error = LyoProblemDetails.FromCode(
            Lyo.Api.Models.Constants.ApiErrorCodes.Forbidden,
            detail,
            DateTime.UtcNow,
            httpContext.TraceIdentifier,
            extensions: new Dictionary<string, object?> { ["disallowedProperties"] = forbidden });

        return PatchPropertyAuthorizationResult.Forbidden(error);
    }

    /// <summary>Tests policy union and forbidden keys without HTTP (for unit tests).</summary>
    public static bool TryGetForbiddenPropertyKeys(
        IReadOnlyDictionary<string, IReadOnlySet<string>> policyMap,
        IReadOnlyDictionary<string, bool> policySucceeded,
        IReadOnlyDictionary<string, object?> requestProperties,
        out List<string> forbidden)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (policyName, propertySet) in policyMap) {
            if (!policySucceeded.TryGetValue(policyName, out var ok) || !ok)
                continue;

            if (propertySet.Contains(Wildcard)) {
                foreach (var key in requestProperties.Keys)
                    allowed.Add(key);

                continue;
            }

            foreach (var p in propertySet)
                allowed.Add(p);
        }

        forbidden = [];
        foreach (var key in requestProperties.Keys) {
            if (!allowed.Contains(key))
                forbidden.Add(key);
        }

        forbidden.Sort(StringComparer.OrdinalIgnoreCase);
        return forbidden.Count > 0;
    }
}
