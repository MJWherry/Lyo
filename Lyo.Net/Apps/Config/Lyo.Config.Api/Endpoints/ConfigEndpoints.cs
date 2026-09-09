using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Config;
using Lyo.Config.Api.Infrastructure;
using Lyo.EntityReference.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Config.Api.Endpoints;

internal static class ConfigEndpoints
{
    private const string PollHeaderName = "X-Config-Poll-Interval-Ms";

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
            resolvedValue = await store.LoadConfigAsync(refs, null, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
        }

        var etagQuoted = ComputeQuotedEtag(resolvedValue);
        ApplyWeakCacheHints(http.Response.Headers);
        StampEtag(http.Response.Headers, etagQuoted);
        if (IsNotModifiedViaIfNoneMatch(http.Request.Headers.IfNoneMatch, etagQuoted) || MatchesVersion(http.Request.Query["version"], etagQuoted))
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

    private static EntityRef RequireEntity(string? subjectEntityType, string? subjectEntityId)
        => new(RequireValue(subjectEntityType, "subjectEntityType"), RequireValue(subjectEntityId, "subjectEntityId"));

    private static string RequireValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, $"{name} is required."));

        return value.Trim();
    }

    extension(RouteGroupBuilder group)
    {
        internal RouteGroupBuilder MapLyoConfiguredEndpoints(bool requireAuthentication = true)
        {
            group.MapManageEndpoints(requireAuthentication);
            group.MapPublicEndpoints();
            return group;
        }

        private RouteGroupBuilder MapPublicEndpoints()
        {
            group.MapMethods(
                "/{appKind}/{appId}", [HttpMethods.Get, HttpMethods.Head], async Task<IResult> (
                    HttpContext http, string appKind, string appId, IConfigStore store, [FromServices] ConfigApiHostingOptions hostOptions, CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var errMsg))
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, errMsg));

                    return await FinishResolve(http, refs, HttpMethods.IsHead(http.Request.Method), store, hostOptions, ct).ConfigureAwait(false);
                });

            group.MapPost(
                "/{appKind}/{appId}", async Task<IResult> (
                    HttpContext http, string appKind, string appId, IConfigStore store, [FromServices] ConfigApiHostingOptions hostOptions, CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var errMsg))
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, errMsg));

                    return await FinishResolve(http, refs, false, store, hostOptions, ct).ConfigureAwait(false);
                });

            return group;
        }

        private RouteGroupBuilder MapManageEndpoints(bool requireAuthentication)
        {
            var manage = group.MapGroup("/manage");
            if (requireAuthentication)
                manage.RequireScope("config.write");
            else
                manage.AllowAnonymous();

            void ProtectAdmin(RouteHandlerBuilder builder)
            {
                if (requireAuthentication)
                    builder.RequireScope("config.admin");
            }

            manage.MapGet(
                "/definitions", async Task<Ok<IReadOnlyList<ConfigDefinitionRecord>>> (string? subjectEntityType, IConfigStore store, CancellationToken ct) => {
                    var type = string.IsNullOrWhiteSpace(subjectEntityType) ? AppConfigEntity.AppEntityType : subjectEntityType.Trim();
                    var defs = await store.GetDefinitionsAsync(type, ct).ConfigureAwait(false);
                    return TypedResults.Ok(defs);
                });

            manage.MapGet(
                "/definitions/by-key", async Task<Results<Ok<ConfigDefinitionRecord>, NotFound>> (string? subjectEntityType, string? key, IConfigStore store, CancellationToken ct) => {
                    var existing = await store.GetDefinitionAsync(RequireValue(subjectEntityType, "subjectEntityType"), RequireValue(key, "key"), ct).ConfigureAwait(false);
                    if (existing is null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return TypedResults.Ok(existing);
                });

            manage.MapGet(
                "/definitions/{definitionId:guid}", async Task<Results<Ok<ConfigDefinitionRecord>, NotFound>> (Guid definitionId, IConfigStore store, CancellationToken ct) => {
                    var existing = await store.GetDefinitionByIdAsync(definitionId, ct).ConfigureAwait(false);
                    if (existing is null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return TypedResults.Ok(existing);
                });

            manage.MapPut(
                "/definitions", async Task<Results<Ok<ConfigDefinitionRecord>, ValidationProblem>> (HttpContext http, ConfigDefinitionRecord body, IConfigStore store, CancellationToken ct) => {
                    try {
                        body.Validate();
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentNullException or FormatException) {
                        throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.ValidationFailed).WithMessage("One or more validation errors occurred.").AddApiError(ApiErrorCodes.InvalidField, $"body: {ex.Message}").Build());
                    }

                    if (body.IsEncrypted) {
                        var encryption = http.RequestServices.GetService<IConfigValueEncryptionService>();
                        if (encryption?.IsEncryptionEnabled != true)
                            throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(ApiErrorCodes.ValidationFailed).WithMessage("Encrypted config requires IEncryptionService on the API host.").AddApiError(ApiErrorCodes.InvalidField, "body.IsEncrypted").Build());
                    }

                    await store.SaveDefinitionAsync(body, ct).ConfigureAwait(false);
                    return TypedResults.Ok(body);
                });

            manage.MapGet(
                "/definitions/{definitionId:guid}/revisions",
                async Task<Ok<IReadOnlyList<ConfigDefinitionRevisionRecord>>> (Guid definitionId, IConfigStore store, CancellationToken ct)
                    => TypedResults.Ok(await store.GetDefinitionRevisionsAsync(definitionId, ct).ConfigureAwait(false)));

            manage.MapGet(
                "/definitions/{definitionId:guid}/revisions/{revision:int}",
                async Task<Results<Ok<ConfigDefinitionRevisionRecord>, NotFound>> (Guid definitionId, int revision, IConfigStore store, CancellationToken ct) => {
                    var row = await store.GetDefinitionRevisionAsync(definitionId, revision, ct).ConfigureAwait(false);
                    if (row is null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return TypedResults.Ok(row);
                });

            ProtectAdmin(
                manage.MapPost(
                    "/definitions/{definitionId:guid}/revert",
                    async Task<Results<Ok<ConfigRevertRevisionRequest>, ProblemHttpResult>> (Guid definitionId, ConfigRevertRevisionRequest body, IConfigStore store, CancellationToken ct) => {
                        try {
                            await store.RevertDefinitionToRevisionAsync(definitionId, body.Revision, ct).ConfigureAwait(false);
                        }
                        catch (InvalidOperationException ex) {
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                        }

                        return TypedResults.Ok(body);
                    }));

            ProtectAdmin(
                manage.MapDelete(
                    "/definitions/{definitionId:guid}", async Task<Results<NoContent, NotFound>> (Guid definitionId, IConfigStore store, CancellationToken ct) => {
                        var existing = await store.GetDefinitionByIdAsync(definitionId, ct).ConfigureAwait(false);
                        if (existing is null)
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                        await store.DeleteDefinitionAsync(definitionId, ct).ConfigureAwait(false);
                        return TypedResults.NoContent();
                    }));

            manage.MapGet(
                "/bindings", async Task<Ok<IReadOnlyList<ConfigBindingRecord>>> (string? subjectEntityType, string? subjectEntityId, IConfigStore store, CancellationToken ct) => {
                    var list = await store.GetBindingsAsync(RequireEntity(subjectEntityType, subjectEntityId), null, ct).ConfigureAwait(false);
                    return TypedResults.Ok(list);
                });

            manage.MapGet(
                "/bindings/by-key", async Task<Results<Ok<ConfigBindingRecord>, NotFound>> (
                    string? subjectEntityType, string? subjectEntityId, string? key, IConfigStore store, CancellationToken ct) => {
                    var existing = await store.GetBindingAsync(RequireEntity(subjectEntityType, subjectEntityId), RequireValue(key, "key"), null, ct).ConfigureAwait(false);
                    if (existing is null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return TypedResults.Ok(existing);
                });

            manage.MapGet(
                "/bindings/{bindingId:guid}", async Task<Results<Ok<ConfigBindingRecord>, NotFound>> (Guid bindingId, IConfigStore store, CancellationToken ct) => {
                    var existing = await store.GetBindingByIdAsync(bindingId, null, ct).ConfigureAwait(false);
                    if (existing is null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return TypedResults.Ok(existing);
                });

            manage.MapPut(
                "/bindings", async Task<Results<Ok<ConfigBindingRecord>, ProblemHttpResult>> (ConfigBindingRecord binding, IConfigStore store, CancellationToken ct) => {
                    try {
                        await store.SaveBindingAsync(binding, null, ct).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException ex) {
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                    }

                    return TypedResults.Ok(binding);
                });

            ProtectAdmin(
                manage.MapDelete(
                    "/bindings", async Task<Results<NoContent, ProblemHttpResult>> (string? subjectEntityType, string? subjectEntityId, IConfigStore store, CancellationToken ct) => {
                        try {
                            await store.DeleteBindingsAsync(RequireEntity(subjectEntityType, subjectEntityId), null, ct).ConfigureAwait(false);
                        }
                        catch (InvalidOperationException ex) {
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                        }

                        return TypedResults.NoContent();
                    }));

            ProtectAdmin(
                manage.MapDelete(
                    "/bindings/{bindingId:guid}", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (Guid bindingId, IConfigStore store, CancellationToken ct) => {
                        var existing = await store.GetBindingByIdAsync(bindingId, null, ct).ConfigureAwait(false);
                        if (existing is null)
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                        try {
                            await store.DeleteBindingAsync(bindingId, null, ct).ConfigureAwait(false);
                        }
                        catch (InvalidOperationException ex) {
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                        }

                        return TypedResults.NoContent();
                    }));

            manage.MapGet(
                "/bindings/{bindingId:guid}/revisions",
                async Task<Ok<IReadOnlyList<ConfigBindingRevisionRecord>>> (Guid bindingId, IConfigStore store, CancellationToken ct)
                    => TypedResults.Ok(await store.GetBindingRevisionsAsync(bindingId, null, ct).ConfigureAwait(false)));

            manage.MapGet(
                "/bindings/{bindingId:guid}/revisions/{revision:int}",
                async Task<Results<Ok<ConfigBindingRevisionRecord>, NotFound>> (Guid bindingId, int revision, IConfigStore store, CancellationToken ct) => {
                    var row = await store.GetBindingRevisionAsync(bindingId, revision, null, ct).ConfigureAwait(false);
                    if (row is null)
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Resource was not found."));

                    return TypedResults.Ok(row);
                });

            ProtectAdmin(
                manage.MapPost(
                    "/bindings/{bindingId:guid}/revert",
                    async Task<Results<Ok<ConfigRevertRevisionRequest>, ProblemHttpResult>> (Guid bindingId, ConfigRevertRevisionRequest body, IConfigStore store, CancellationToken ct) => {
                        try {
                            await store.RevertBindingToRevisionAsync(bindingId, body.Revision, null, ct).ConfigureAwait(false);
                        }
                        catch (InvalidOperationException ex) {
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                        }

                        return TypedResults.Ok(body);
                    }));

            manage.MapGet(
                "/resolved", async Task<Results<Ok<ResolvedConfigRecord>, ProblemHttpResult>> (string? subjectEntityType, string? subjectEntityId, IConfigStore store, CancellationToken ct) => {
                    try {
                        var resolved = await store.LoadConfigAsync(RequireEntity(subjectEntityType, subjectEntityId), null, ct).ConfigureAwait(false);
                        return TypedResults.Ok(resolved);
                    }
                    catch (InvalidOperationException ex) {
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                    }
                });

            manage.MapGet(
                "/apps/{appKind}/{appId}/bindings",
                async Task<Results<Ok<IReadOnlyList<ConfigBindingRecord>>, ProblemHttpResult>> (string appKind, string appId, IConfigStore store, CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var msg))
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, msg));

                    var list = await store.GetBindingsAsync(refs, null, ct).ConfigureAwait(false);
                    return TypedResults.Ok(list);
                });

            manage.MapGet(
                "/apps/{appKind}/{appId}/bindings/{key}/revisions", async Task<Results<Ok<IReadOnlyList<ConfigBindingRevisionRecord>>, ProblemHttpResult>> (
                    string appKind, string appId, string key, IConfigStore store, CancellationToken ct) => {
                    if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var msg))
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, msg));

                    if (string.IsNullOrWhiteSpace(key))
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "Key segment is required."));

                    return TypedResults.Ok(await store.GetBindingRevisionsAsync(refs, Uri.UnescapeDataString(key.Trim()), null, ct).ConfigureAwait(false));
                });

            ProtectAdmin(
                manage.MapPost(
                    "/apps/{appKind}/{appId}/bindings/{key}/revert", async Task<Results<Ok<ConfigRevertRevisionRequest>, ProblemHttpResult>> (
                        string appKind, string appId, string key, ConfigRevertRevisionRequest body, IConfigStore store, CancellationToken ct) => {
                        if (!AppConfigEntity.TryCreate(appKind, appId, out var refs, out var msg))
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, msg));

                        if (string.IsNullOrWhiteSpace(key))
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "Key segment is required."));

                        try {
                            await store.RevertBindingToRevisionAsync(refs, Uri.UnescapeDataString(key.Trim()), body.Revision, null, ct).ConfigureAwait(false);
                        }
                        catch (InvalidOperationException ex) {
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Conflict, ex.Message));
                        }

                        return TypedResults.Ok(body);
                    }));

            return manage;
        }
    }
}
