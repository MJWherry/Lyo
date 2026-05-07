using Lyo.Common.Identifiers;
using Lyo.Config.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lyo.Config.Api.Endpoints;

internal sealed record RevertRevisionDto(int Revision);

internal static class ConfigEndpoints
{
    private const string PollHeaderName = "X-Config-Poll-Interval-Ms";

    extension(RouteGroupBuilder group)
    {
        internal RouteGroupBuilder MapLyoConfiguredEndpoints()
        {
            group.MapManageEndpoints();
            group.MapPublicEndpoints();
            return group;
        }

        private RouteGroupBuilder MapPublicEndpoints()
        {
            group.MapMethods(
                "/{appKind}/{appId}", [HttpMethods.Get, HttpMethods.Head], async Task<IResult> (
                    HttpContext http,
                    string appKind,
                    string appId,
                    IConfigStore store,
                    IOptions<ConfigApiHostingOptions> hostOptions,
                    CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var errMsg))
                        return TypedResults.BadRequest(errMsg);

                    return await FinishResolve(http, refs, HttpMethods.IsHead(http.Request.Method), store, hostOptions.Value, ct).ConfigureAwait(false);
                });

            group.MapPost(
                "/{appKind}/{appId}", async Task<IResult> (
                    HttpContext http,
                    string appKind,
                    string appId,
                    IConfigStore store,
                    IOptions<ConfigApiHostingOptions> hostOptions,
                    CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var errMsg))
                        return TypedResults.BadRequest(errMsg);

                    return await FinishResolve(http, refs, headOnly: false, store, hostOptions.Value, ct).ConfigureAwait(false);
                });

            return group;
        }

        private RouteGroupBuilder MapManageEndpoints()
        {
            var manage = group.MapGroup("/manage");

            manage.MapGet(
                "/definitions", async Task<Ok<IReadOnlyList<ConfigDefinitionRecord>>> (IConfigStore store, CancellationToken ct) => {
                    var defs = await store.GetDefinitionsAsync(AppConfigEntity.AppEntityType, ct).ConfigureAwait(false);
                    return TypedResults.Ok(defs);
                });

            manage.MapPut(
                "/definitions", async Task<Results<NoContent, ValidationProblem>> (HttpContext _, ConfigDefinitionRecord body, IConfigStore store, CancellationToken ct) => {
                    try {
                        body.Validate();
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentNullException or FormatException) {
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["body"] = [ex.Message] });
                    }

                    await store.SaveDefinitionAsync(body, ct).ConfigureAwait(false);
                    return TypedResults.NoContent();
                });

            manage.MapDelete(
                "/definitions/{definitionId:guid}", async Task<Results<NoContent, NotFound>> (Guid definitionId, IConfigStore store, CancellationToken ct) => {
                    var existing = await store.GetDefinitionByIdAsync(definitionId, ct).ConfigureAwait(false);
                    if (existing is null)
                        return TypedResults.NotFound();

                    await store.DeleteDefinitionAsync(definitionId, ct).ConfigureAwait(false);
                    return TypedResults.NoContent();
                });

            manage.MapPut(
                "/bindings", async Task<Results<NoContent, ProblemHttpResult>> (ConfigBindingRecord binding, IConfigStore store, CancellationToken ct) => {
                    try {
                        await store.SaveBindingAsync(binding, ct).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException ex) {
                        return TypedResults.Problem(detail: ex.Message, title: "Binding rejected", statusCode: StatusCodes.Status409Conflict);
                    }

                    return TypedResults.NoContent();
                });

            manage.MapDelete(
                "/bindings/{bindingId:guid}", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (Guid bindingId, IConfigStore store, CancellationToken ct) => {
                    var existing = await store.GetBindingByIdAsync(bindingId, ct).ConfigureAwait(false);
                    if (existing is null)
                        return TypedResults.NotFound();

                    try {
                        await store.DeleteBindingAsync(bindingId, ct).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException ex) {
                        return TypedResults.Problem(detail: ex.Message, title: "Delete rejected", statusCode: StatusCodes.Status409Conflict);
                    }

                    return TypedResults.NoContent();
                });

            manage.MapGet(
                "/bindings/{bindingId:guid}/revisions",
                async Task<Ok<IReadOnlyList<ConfigBindingRevisionRecord>>> (Guid bindingId, IConfigStore store, CancellationToken ct)
                    => TypedResults.Ok(await store.GetBindingRevisionsAsync(bindingId, ct).ConfigureAwait(false)));

            manage.MapPost(
                "/bindings/{bindingId:guid}/revert",
                async Task<Results<NoContent, ProblemHttpResult>> (Guid bindingId, RevertRevisionDto body, IConfigStore store, CancellationToken ct) => {
                    try {
                        await store.RevertBindingToRevisionAsync(bindingId, body.Revision, ct).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException ex) {
                        return TypedResults.Problem(detail: ex.Message, title: "Revert rejected", statusCode: StatusCodes.Status409Conflict);
                    }

                    return TypedResults.NoContent();
                });

            manage.MapGet(
                "/apps/{appKind}/{appId}/bindings",
                async Task<Results<Ok<IReadOnlyList<ConfigBindingRecord>>, BadRequest<string>>> (string appKind, string appId, IConfigStore store, CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var msg))
                        return TypedResults.BadRequest(msg);

                    var list = await store.GetBindingsAsync(refs, ct).ConfigureAwait(false);
                    return TypedResults.Ok(list);
                });

            manage.MapGet(
                "/apps/{appKind}/{appId}/bindings/{key}/revisions", async Task<Results<Ok<IReadOnlyList<ConfigBindingRevisionRecord>>, BadRequest<string>>> (
                    string appKind,
                    string appId,
                    string key,
                    IConfigStore store,
                    CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var msg))
                        return TypedResults.BadRequest(msg);

                    if (string.IsNullOrWhiteSpace(key))
                        return TypedResults.BadRequest("Key segment is required.");

                    return TypedResults.Ok(await store.GetBindingRevisionsAsync(refs, Uri.UnescapeDataString(key.Trim()), ct).ConfigureAwait(false));
                });

            manage.MapPost(
                "/apps/{appKind}/{appId}/bindings/{key}/revert", async Task<Results<NoContent, BadRequest<string>, ProblemHttpResult>> (
                    string appKind,
                    string appId,
                    string key,
                    RevertRevisionDto body,
                    IConfigStore store,
                    CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var msg))
                        return TypedResults.BadRequest(msg);

                    if (string.IsNullOrWhiteSpace(key))
                        return TypedResults.BadRequest("Key segment is required.");

                    try {
                        await store.RevertBindingToRevisionAsync(refs, Uri.UnescapeDataString(key.Trim()), body.Revision, ct).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException ex) {
                        return TypedResults.Problem(detail: ex.Message, title: "Revert rejected", statusCode: StatusCodes.Status409Conflict);
                    }

                    return TypedResults.NoContent();
                });

            return manage;
        }
    }

    private static async Task<IResult> FinishResolve(
        HttpContext http,
        EntityRef refs,
        bool headOnly,
        IConfigStore store,
        ConfigApiHostingOptions hostingOptions,
        CancellationToken ct)
    {
        AppendAdvisoryPollHeader(http.Response.Headers, hostingOptions.PollIntervalAdvisoryMilliseconds);

        ResolvedConfigRecord resolvedValue;
        try {
            resolvedValue = await store.LoadConfigAsync(refs, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Config validation failed.");
        }

        var etagQuoted = ComputeQuotedEtag(resolvedValue);

        ApplyWeakCacheHints(http.Response.Headers);
        StampEtag(http.Response.Headers, etagQuoted);

        if (IsNotModifiedViaIfNoneMatch(http.Request.Headers.IfNoneMatch, etagQuoted) ||
            MatchesVersion(http.Request.Query["version"], etagQuoted))
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);

        if (headOnly)
            return TypedResults.StatusCode(StatusCodes.Status200OK);

        return TypedResults.Json(resolvedValue, ConfigJsonSerializerOptions.Default);
    }

    private static string ComputeQuotedEtag(ResolvedConfigRecord resolved)
    {
        var canonical = ConfigFingerprint.CanonicalUtf8(resolved);
        return ConfigFingerprint.ComputeQuotedStrongEtag(canonical.AsSpan());
    }

    private static void StampEtag(IHeaderDictionary hdr, string quotedEtag) => hdr["ETag"] = quotedEtag;

    private static void ApplyWeakCacheHints(IHeaderDictionary hdr) => hdr.CacheControl = "private, max-age=0";

    private static void AppendAdvisoryPollHeader(IHeaderDictionary hdr, int? advisorMs)
    {
        if (advisorMs is not > 0)
            return;

        hdr[PollHeaderName] = advisorMs.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool MatchesVersion(StringValues versionRaw, string serverEtagQuoted)
    {
        var raw = versionRaw.ToString().Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        var candidate = NormalizeEtagToken(raw.AsSpan());
        var server = NormalizeEtagToken(serverEtagQuoted.AsSpan());
        return !candidate.IsEmpty && !server.IsEmpty && candidate.SequenceEqual(server);
    }

    private static bool IsNotModifiedViaIfNoneMatch(StringValues tokens, string serverEtagQuoted)
    {
        if (tokens.Count == 0)
            return false;

        var serverBare = NormalizeEtagToken(serverEtagQuoted.AsSpan());

        foreach (var t in tokens) {
            var candidate = NormalizeEtagToken(t.AsSpan());
            if (candidate.Length == 1 && candidate[0] == '*')
                return true;

            if (!candidate.IsEmpty && candidate.SequenceEqual(serverBare))
                return true;
        }

        return false;
    }

    private static ReadOnlySpan<char> NormalizeEtagToken(ReadOnlySpan<char> token)
    {
        var trimmed = token.Trim();

        if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..].Trim();

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed.Slice(1, trimmed.Length - 2);

        return trimmed;
    }
}

