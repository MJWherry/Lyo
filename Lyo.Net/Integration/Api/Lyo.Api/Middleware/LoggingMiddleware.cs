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
/// Logs each request (debug) with trace id, user email (when present), path, and query string; logs response status. Catches unhandled exceptions and writes
/// <see cref="Lyo.Api.Models.Error.LyoProblemDetails" /> as <c>application/problem+json</c>: <see cref="HttpException" /> maps to its <see cref="HttpException.StatusCode" />
/// (with <c>Retry-After</c> for rate limit / unavailable), <see cref="ValidationException" /> maps to 400 with field errors, and anything else maps to 500.
/// Error responses that complete with an empty body (e.g. bare <c>Results.NotFound()</c>) also receive a problem details body.
/// </summary>
//todo actually read body and log as debug
//todo if problem details from some type of validation, inject our own error
public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger, IHostEnvironment environment)
{
    private const string ProblemJsonContentType = "application/problem+json";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(LyoJsonSerializerOptions.Create()) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

    /// <summary>Invokes the next middleware, wrapping logging and exception handling.</summary>
    public async Task Invoke(HttpContext context)
    {
        _ = environment;
        using (LogRequest(context)) {
            var sanitizedQueryString = Utilities.SanitizeUri(context.Request.QueryString.Value);
            logger.LogDebug(
                "{Trace} REQUEST {RequestMethod} {RequestPath}{RequestQueryString}", context.TraceIdentifier, context.Request.Method, context.Request.Path, sanitizedQueryString);

            try {
                await next(context);
                await WriteFallbackProblemForEmptyErrorAsync(context);
            }
            catch (HttpException ex) {
                var error = BuildProblem(ex, context).Build();
                SetRetryAfterHeader(context, ex);
                await WriteProblemAsync(context, error);
                if (error.Status >= 500)
                    logger.LogError(ex, "{Error}", error.ToString());
                else
                    logger.LogWarning("{Error}", error.ToString());
            }
            catch (ValidationException ex) {
                var error = BuildProblem(ex, context).Build();
                await WriteProblemAsync(context, error);
                logger.LogWarning("{Error}", error.ToString());
            }
            catch (Exception ex) {
                var error = BuildProblem(ex, context).WithStatus(500).Build();
                await WriteProblemAsync(context, error);
                logger.LogError(ex, "Unmanaged exception caught");
            }

            LogResponse(context);
        }
    }

    private static LyoProblemDetailsBuilder BuildProblem(Exception ex, HttpContext context)
        => LyoProblemDetailsBuilder.FromException(ex)
            .WithTraceId(Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier)
            .WithSpanId(Activity.Current?.SpanId.ToString())
            .WithRoute(context.Request.Path.HasValue ? context.Request.Path.Value : null);

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

    /// <summary>Writes a problem details body for 4xx/5xx responses that completed with an empty body (e.g. bare <c>Results.NotFound()</c>).</summary>
    private static async Task WriteFallbackProblemForEmptyErrorAsync(HttpContext context)
    {
        if (context.Response.HasStarted || context.Response.StatusCode < 400 || context.Response.ContentLength is > 0)
            return;

        var status = context.Response.StatusCode;
        var errorCode = LyoProblemDetails.MapHttpStatusToErrorCode(status);
        var error = LyoProblemDetailsBuilder.CreateWithActivity()
            .WithStatus(status)
            .WithErrorCode(errorCode)
            .WithTitle(LyoProblemDetails.HttpStatusTitle(status))
            .WithMessage(LyoProblemDetails.HttpStatusTitle(status))
            .WithTraceId(Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier)
            .WithSpanId(Activity.Current?.SpanId.ToString())
            .WithRoute(context.Request.Path.HasValue ? context.Request.Path.Value : null)
            .Build();

        await WriteProblemAsync(context, error);
    }

    private IDisposable? LogRequest(HttpContext context)
    {
        //context.Request.EnableBuffering(); // Important: allows reading the stream multiple times
        //using var reader = new StreamReader(
        //    context.Request.Body,
        //    encoding: Encoding.UTF8,
        //    detectEncodingFromByteOrderMarks: false,
        //    bufferSize: 1024,
        //    leaveOpen: true); // Leave the stream open for the next middleware
        //var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
        //context.Request.Body.Position = 0; // Reset for downstream middleware/controllers
        var sanitizedQueryString = Utilities.SanitizeUri(context.Request.QueryString.Value);
        var email = context.User.FindFirst(ClaimTypes.Name)?.Value ?? null;
        return logger.BeginScope(
            "{Trace} ({RequestHost}/{RequestUserAgent}) {UserEmail} {RequestMethod} {RequestPath}{RequestQueryString}", context.TraceIdentifier, context.Request.Host,
            context.Request.Headers["User-Agent"], email, context.Request.Method, context.Request.Path, sanitizedQueryString);
    }

    private void LogResponse(HttpContext context)
        => logger.LogDebug(
            "{Trace} RESPONSE {RequestMethod} {RequestPath} {ResponseCode} ({ResponseContentType})", context.TraceIdentifier, context.Request.Method, context.Request.Path,
            context.Response.StatusCode, context.Response.ContentType ?? "Unknown");
}
