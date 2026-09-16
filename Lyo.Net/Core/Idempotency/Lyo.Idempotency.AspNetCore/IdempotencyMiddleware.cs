using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.Idempotency.AspNetCore;

/// <summary>
/// Claims an idempotency key before the endpoint runs and replays the stored response on a later call.
/// Register it after <c>UseRouting</c> so endpoint metadata is visible, and after authentication so the subject claim is set.
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    private const string ReplayedHeader = "Idempotency-Replayed";

    /// <summary>Reads the key, claims the row, and either replays or lets the rest of the pipeline run.</summary>
    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
    {
        ArgumentHelpers.ThrowIfNull(context);
        ArgumentHelpers.ThrowIfNull(store);
        if (IsSafe(context.Request.Method)) {
            await next(context).ConfigureAwait(false);
            return;
        }

        var key = ReadKey(context.Request);
        var required = context.GetEndpoint()?.Metadata.GetMetadata<RequireIdempotencyKeyAttribute>() is not null;
        if (string.IsNullOrWhiteSpace(key)) {
            if (!required) {
                await next(context).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("idempotency_key_required", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var subject = Subject(context.User);
        if (subject is null) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("idempotency_subject_required", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        context.Request.EnableBuffering();
        var body = await ReadBodyAsync(context.Request, context.RequestAborted).ConfigureAwait(false);
        var hash = IdempotencyHash.OfRequest(context.Request.QueryString.Value, body);
        var scope = "api|" + subject + "|" + context.Request.Method + "|" + context.Request.Path.Value;
        IdempotencyBeginResult begin;
        try {
            begin = await store.TryBeginAsync(scope, key, hash, ct: context.RequestAborted).ConfigureAwait(false);
        }
        catch (ArgumentException) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (begin.Outcome == IdempotencyOutcome.Replay) {
            await WriteReplayAsync(context, begin).ConfigureAwait(false);
            return;
        }

        if (begin.Outcome is IdempotencyOutcome.InProgress or IdempotencyOutcome.Conflict) {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        var original = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try {
            await next(context).ConfigureAwait(false);
            var captured = buffer.ToArray();
            await store.CompleteAsync(begin.Id, begin.LeaseToken!.Value, context.Response.StatusCode, captured, context.Response.ContentType, context.RequestAborted).ConfigureAwait(false);
            buffer.Position = 0;
            context.Response.Body = original;
            await buffer.CopyToAsync(original, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception) {
            var captured = buffer.ToArray();
            context.Response.Body = original;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await store.FailAsync(begin.Id, begin.LeaseToken!.Value, StatusCodes.Status500InternalServerError, captured, context.Response.ContentType, context.RequestAborted).ConfigureAwait(false);
            if (captured.Length > 0)
                await original.WriteAsync(captured, context.RequestAborted).ConfigureAwait(false);
        }
    }

    private static bool IsSafe(string method)
        => HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

    private static string? ReadKey(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HttpHeaderInfo.IdempotencyKey.Name, out var primary) && !string.IsNullOrWhiteSpace(primary))
            return primary.ToString();

        var alias = HttpHeaderInfo.IdempotencyKey.Aliases.FirstOrDefault();
        if (alias is not null && request.Headers.TryGetValue(alias, out var secondary) && !string.IsNullOrWhiteSpace(secondary))
            return secondary.ToString();

        return null;
    }

    private static string? Subject(System.Security.Claims.ClaimsPrincipal user)
    {
        var lyoUser = user.FindFirst("lyo:user")?.Value;
        if (!string.IsNullOrWhiteSpace(lyoUser))
            return lyoUser;

        var sub = user.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(sub) ? null : sub;
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken ct)
    {
        using var copy = new MemoryStream();
        await request.Body.CopyToAsync(copy, ct).ConfigureAwait(false);
        request.Body.Position = 0;
        return copy.ToArray();
    }

    private static async Task WriteReplayAsync(HttpContext context, IdempotencyBeginResult begin)
    {
        context.Response.StatusCode = begin.ResponseStatus ?? StatusCodes.Status200OK;
        context.Response.Headers[ReplayedHeader] = "true";
        if (!string.IsNullOrWhiteSpace(begin.ResponseContentType))
            context.Response.ContentType = begin.ResponseContentType;

        if (begin.ResponseBody is { Length: > 0 })
            await context.Response.Body.WriteAsync(begin.ResponseBody, context.RequestAborted).ConfigureAwait(false);
    }
}
