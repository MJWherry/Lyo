using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Common;
using Lyo.Exceptions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Middleware;

/// <summary>
/// Source of truth for API error responses and failure logging. Catches intentional failures (<see cref="ApiErrorException" />, <see cref="HttpException" />,
/// <see cref="ValidationException" />) and logs them at Warn; unhandled exceptions at Error. Writes <see cref="LyoProblemDetails" /> as <c>application/problem+json</c>.
/// Completed 4xx/5xx responses (including auth challenges that already started the response) are always logged at Warn; empty bodies are filled when possible.
/// </summary>
public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger, IHostEnvironment environment)
{
    private const string ProblemJsonContentType = "application/problem+json";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(LyoJsonSerializerOptions.Create()) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

    /// <summary>Invokes the next middleware, wrapping logging and exception handling.</summary>
    public async Task Invoke(HttpContext context)
    {
        _ = environment;
        using (BeginRequestScope(context)) {
            var sanitizedQueryString = Utilities.SanitizeUri(context.Request.QueryString.Value);
            logger.LogDebug(
                "{Trace} REQUEST {RequestMethod} {RequestPath}{RequestQueryString}", context.TraceIdentifier, context.Request.Method, context.Request.Path, sanitizedQueryString);

            try {
                await next(context);
                await HandleCompletedErrorResponseAsync(context);
            }
            catch (ApiErrorException ex) {
                var error = EnrichProblem(ex.ProblemDetails, context);
                await WriteProblemAsync(context, error);
                LogCaughtFailure(context, error, ex);
            }
            catch (HttpException ex) {
                var error = EnrichProblem(BuildProblem(ex, context).Build(), context);
                SetRetryAfterHeader(context, ex);
                await WriteProblemAsync(context, error);
                LogCaughtFailure(context, error, ex);
            }
            catch (ValidationException ex) {
                var error = EnrichProblem(BuildProblem(ex, context).Build(), context);
                await WriteProblemAsync(context, error);
                LogCaughtFailure(context, error, ex);
            }
            catch (Exception ex) {
                var error = EnrichProblem(BuildProblem(ex, context).WithStatus(500).Build(), context);
                await WriteProblemAsync(context, error);
                LogUnhandledFailure(context, error, ex);
            }

            LogResponse(context);
        }
    }

    private static LyoProblemDetailsBuilder BuildProblem(Exception ex, HttpContext context)
        => LyoProblemDetailsBuilder.FromException(ex)
            .WithTraceId(Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier)
            .WithSpanId(Activity.Current?.SpanId.ToString())
            .WithRoute(context.Request.Path.HasValue ? context.Request.Path.Value : null);

    private static LyoProblemDetails EnrichProblem(LyoProblemDetails error, HttpContext context)
    {
        var instance = error.Instance ?? (context.Request.Path.HasValue ? context.Request.Path.Value : null);
        var traceId = error.TraceId ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var spanId = error.SpanId ?? Activity.Current?.SpanId.ToString();
        return error with { Instance = instance, TraceId = traceId, SpanId = spanId };
    }

    private static void SetRetryAfterHeader(HttpContext context, HttpException ex)
    {
        var retryAfter = ex switch {
            RateLimitExceededException rateLimit => rateLimit.RetryAfter,
            ServiceUnavailableException unavailable => unavailable.RetryAfter,
            var _ => null
        };

        if (retryAfter is not null && !context.Response.HasStarted)
            context.Response.Headers["Retry-After"] = Math.Max(0, (int)Math.Ceiling(retryAfter.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
    }

    private static async Task WriteProblemAsync(HttpContext context, LyoProblemDetails error)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = error.Status;
        context.Response.ContentType = ProblemJsonContentType;
        var json = JsonSerializer.Serialize(error, SerializerOptions);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// After the rest of the pipeline completes: log every 4xx/5xx as Warn (including auth challenges that already started the response), and fill an empty error body with
    /// <see cref="LyoProblemDetails" /> when the response has not started yet.
    /// </summary>
    private async Task HandleCompletedErrorResponseAsync(HttpContext context)
    {
        if (context.Response.StatusCode < 400)
            return;

        var status = context.Response.StatusCode;
        var errorCode = LyoProblemDetails.MapHttpStatusToErrorCode(status);
        var error = EnrichProblem(
            LyoProblemDetailsBuilder.CreateWithActivity()
                .WithStatus(status)
                .WithErrorCode(errorCode)
                .WithTitle(LyoProblemDetails.HttpStatusTitle(status))
                .WithMessage(LyoProblemDetails.HttpStatusTitle(status))
                .WithTraceId(Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier)
                .WithSpanId(Activity.Current?.SpanId.ToString())
                .WithRoute(context.Request.Path.HasValue ? context.Request.Path.Value : null)
                .Build(), context);

        // Auth challenges (401/403) often start the response when writing WWW-Authenticate — still log even if we cannot rewrite the body.
        if (!context.Response.HasStarted && context.Response.ContentLength is not > 0)
            await WriteProblemAsync(context, error);

        LogCaughtFailure(context, error, exception: null);
    }

    private void LogCaughtFailure(HttpContext context, LyoProblemDetails error, Exception? exception)
    {
        var (clientIp, userId, userName, primaryCode, codes) = FailureLogContext(context, error);
        if (exception is null) {
            logger.LogWarning(
                "API failure {Status} {ErrorCode} {ErrorCodes} {ClientIp} {UserId} {UserName} {Detail}", error.Status, primaryCode, codes, clientIp, userId, userName,
                error.GetFullMessage());
            return;
        }

        logger.LogWarning(
            exception, "API failure {Status} {ErrorCode} {ErrorCodes} {ClientIp} {UserId} {UserName} {Detail}", error.Status, primaryCode, codes, clientIp, userId, userName,
            error.GetFullMessage());
    }

    private void LogUnhandledFailure(HttpContext context, LyoProblemDetails error, Exception exception)
    {
        var (clientIp, userId, userName, primaryCode, codes) = FailureLogContext(context, error);
        logger.LogError(
            exception, "Unhandled API exception {Status} {ErrorCode} {ErrorCodes} {ClientIp} {UserId} {UserName} {Detail}", error.Status, primaryCode, codes, clientIp, userId,
            userName, error.GetFullMessage());
    }

    private static (string? ClientIp, string? UserId, string? UserName, string PrimaryCode, string Codes) FailureLogContext(HttpContext context, LyoProblemDetails error)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
        var primaryCode = error.Errors.Count > 0 ? error.Errors[0].Code : LyoProblemDetails.MapHttpStatusToErrorCode(error.Status);
        var codes = error.Errors.Count > 0 ? string.Join(",", error.Errors.Select(e => e.Code).Distinct()) : primaryCode;
        return (clientIp, userId, userName, primaryCode, codes);
    }

    private IDisposable? BeginRequestScope(HttpContext context)
    {
        var sanitizedQueryString = Utilities.SanitizeUri(context.Request.QueryString.Value);
        var userName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        return logger.BeginScope(
            new Dictionary<string, object?> {
                ["Trace"] = context.TraceIdentifier,
                ["RequestHost"] = context.Request.Host.ToString(),
                ["RequestUserAgent"] = context.Request.Headers.UserAgent.ToString(),
                ["ClientIp"] = clientIp,
                ["UserId"] = userId,
                ["UserName"] = userName,
                ["RequestMethod"] = context.Request.Method,
                ["RequestPath"] = context.Request.Path.Value,
                ["RequestQueryString"] = sanitizedQueryString
            });
    }

    private void LogResponse(HttpContext context)
        => logger.LogDebug(
            "{Trace} RESPONSE {RequestMethod} {RequestPath} {ResponseCode} ({ResponseContentType})", context.TraceIdentifier, context.Request.Method, context.Request.Path,
            context.Response.StatusCode, context.Response.ContentType ?? "Unknown");
}
