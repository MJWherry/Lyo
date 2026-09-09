using System.Reflection;
using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using Lyo.Common.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>
/// Optional per-patch rules: which JSON property names may be updated for the current user. Pair with <see cref="EndpointAuth" /> on the same endpoint for typical APIs. When
/// <see cref="Custom" /> is set, it replaces <see cref="PolicyAllowedProperties" />. Otherwise, allowed names are the union of property sets from each
/// <see cref="PolicyAllowedProperties" /> entry whose policy passes <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, string)" />. Use <c>"*"</c> in a
/// policy's set to allow every key present in <see cref="PatchRequest.Properties" /> when that policy succeeds.
/// </summary>
public sealed record PatchPropertyAuthorization
{
    /// <summary>Maps an ASP.NET Core policy name to allowed CLR property names (case-insensitive). Ignored when <see cref="Custom" /> is set.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>>? PolicyAllowedProperties { get; init; }

    /// <summary>When set, runs in place of <see cref="PolicyAllowedProperties" />.</summary>
    public Func<HttpContext, Type, PatchRequest, CancellationToken, ValueTask<PatchPropertyAuthorizationResult>>? Custom { get; init; }

    /// <summary>Fluent helper: policy name to allowed property names (or <c>"*"</c>).</summary>
    public static PatchPropertyAuthorization ForPolicies(Action<PatchPropertyAuthorizationBuilder> configure)
    {
        var b = new PatchPropertyAuthorizationBuilder();
        configure(b);
        return b.Build();
    }
}

/// <summary>Fluent builder for <see cref="PatchPropertyAuthorization" /> policy maps.</summary>
public sealed class PatchPropertyAuthorizationBuilder
{
    private readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.Ordinal);

    /// <summary>Adds or merges allowed property names for that policy.</summary>
    public PatchPropertyAuthorizationBuilder AllowPropertiesForPolicy(string policyName, params string[] propertyNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        if (!_map.TryGetValue(policyName, out var set)) {
            set = new(StringComparer.OrdinalIgnoreCase);
            _map[policyName] = set;
        }

        foreach (var p in propertyNames)
            set.Add(p);

        return this;
    }

    internal PatchPropertyAuthorization Build()
        => new() { PolicyAllowedProperties = _map.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlySet<string>)kvp.Value, StringComparer.Ordinal) };
}

/// <summary>Outcome of applying <see cref="PatchPropertyAuthorization" />: either an allowed (possibly filtered) patch or a forbidden problem.</summary>
public readonly struct PatchPropertyAuthorizationResult
{
    private PatchPropertyAuthorizationResult(bool success, PatchRequest? request, LyoProblemDetails? error)
    {
        Success = success;
        Request = request;
        Error = error;
    }

    /// <summary>True when the patch may proceed (possibly with some properties removed).</summary>
    public bool Success { get; }

    /// <summary>When <see cref="Success" /> is true, the patch body that will be applied.</summary>
    public PatchRequest? Request { get; }

    /// <summary>When <see cref="Success" /> is false, the problem returned to the client (usually 403).</summary>
    public LyoProblemDetails? Error { get; }

    /// <summary>Allows the given patch request without changing it.</summary>
    public static PatchPropertyAuthorizationResult Ok(PatchRequest request) => new(true, request, null);

    /// <summary>Rejects the patch with a structured error payload.</summary>
    public static PatchPropertyAuthorizationResult Forbidden(LyoProblemDetails error) => new(false, null, error);
}

/// <summary>Applies <see cref="PatchPropertyAuthorization" /> to a patch request (typed and dynamic endpoints).</summary>
public static class PatchPropertyAuthorizationApplier
{
    private const string Wildcard = "*";

    /// <summary>No rules or empty rules: returns the original request without changing it.</summary>
    public static ValueTask<PatchPropertyAuthorizationResult> ApplyAsync(
        PatchPropertyAuthorization? authorization,
        HttpContext httpContext,
        Type entityType,
        PatchRequest request,
        CancellationToken ct = default)
    {
        if (authorization == null || IsNoOp(authorization))
            return ValueTask.FromResult(PatchPropertyAuthorizationResult.Ok(request));

        if (authorization.Custom != null)
            return authorization.Custom(httpContext, entityType, request, ct);

        return ApplyPolicyMapAsync(authorization, httpContext, request, ct);
    }

    /// <summary>
    /// Applies the same rules to a full-replacement write (PUT-style update, or upsert). PATCH names the properties it touches. A replacement carries every property, so the set
    /// being written is derived by diffing <paramref name="incoming" /> against the persisted <paramref name="current" />. Without this, a caller restricted from patching a
    /// property could change it anyway by switching verb.
    /// </summary>
    /// <param name="authorization">The endpoint's property rules; a null or empty rule set approves the write without inspecting it.</param>
    /// <param name="httpContext">The current request, used to resolve the caller for policy evaluation.</param>
    /// <param name="entityType">The entity being written, passed to custom handlers.</param>
    /// <param name="incoming">The replacement payload.</param>
    /// <param name="current">The persisted entity, or null for an insert — in which case every non-default incoming value counts as a write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Null when the write may proceed; otherwise the problem to return (403).</returns>
    public static async ValueTask<LyoProblemDetails?> AuthorizeReplacementAsync(
        PatchPropertyAuthorization? authorization,
        HttpContext httpContext,
        Type entityType,
        object? incoming,
        object? current,
        CancellationToken ct = default)
    {
        if (authorization == null || IsNoOp(authorization) || incoming == null)
            return null;

        var changed = DiffWrittenProperties(incoming, current);
        if (changed.Count == 0)
            return null;

        var synthetic = new PatchRequest { Properties = changed };
        var result = await ApplyAsync(authorization, httpContext, entityType, synthetic, ct).ConfigureAwait(false);

        // A filtered result is meaningless for a replacement. The service writes the whole body regardless, so anything short of full approval has to be a refusal.
        if (!result.Success)
            return result.Error;

        if (result.Request is { } approved && approved.Properties.Count == changed.Count)
            return null;

        return LyoProblemDetails.FromCode(Constants.ApiErrorCodes.Forbidden, "Not allowed to write one or more properties in this request.", DateTime.UtcNow);
    }

    /// <summary>Public properties readable on both sides whose incoming value differs from the persisted one. Read-only and indexed properties are skipped.</summary>
    public static Dictionary<string, object?> DiffWrittenProperties(object incoming, object? current)
    {
        var changed = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var currentType = current?.GetType();
        foreach (var property in incoming.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                continue;

            var incomingValue = property.GetValue(incoming);
            if (current is null) {
                if (incomingValue is not null && !Equals(incomingValue, DefaultOf(property.PropertyType)))
                    changed[property.Name] = incomingValue;

                continue;
            }

            var currentProperty = currentType!.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
            if (currentProperty is null || !currentProperty.CanRead || currentProperty.GetIndexParameters().Length > 0)
                continue;

            if (!Equals(incomingValue, currentProperty.GetValue(current)))
                changed[property.Name] = incomingValue;
        }

        return changed;
    }

    private static object? DefaultOf(Type type) => type.DefaultValue();

    private static bool IsNoOp(PatchPropertyAuthorization authorization)
        => authorization.Custom == null && (authorization.PolicyAllowedProperties == null || authorization.PolicyAllowedProperties.Count == 0);

    private static async ValueTask<PatchPropertyAuthorizationResult> ApplyPolicyMapAsync(
        PatchPropertyAuthorization authorization,
        HttpContext httpContext,
        PatchRequest request,
        CancellationToken ct)
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
            Constants.ApiErrorCodes.Forbidden, detail, DateTime.UtcNow, httpContext.TraceIdentifier, extensions: new() { ["disallowedProperties"] = forbidden });

        return PatchPropertyAuthorizationResult.Forbidden(error);
    }

    /// <summary>Tests policy union and forbidden keys without HTTP (used by unit tests).</summary>
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