using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Middleware;

//todo actually read body and log as debug
//todo if problem details from some type of validation, inject our own error
public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger, IHostEnvironment environment)
{
    public async Task Invoke(HttpContext context)
    {
        using (LogRequest(context)) {
            var sanitizedQueryString = Utilities.SanitizeUri(context.Request.QueryString.Value);
            logger.LogDebug(
                "{Trace} REQUEST {RequestMethod} {RequestPath}{RequestQueryString}", context.TraceIdentifier, context.Request.Method, context.Request.Path, sanitizedQueryString);

            try {
                await next(context);
            }
            catch (LFException ex) {
                var apiErrorBuilder = LyoProblemDetailsBuilder.FromException(ex)
                    .WithTraceId(Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier)
                    .WithSpanId(Activity.Current?.SpanId.ToString())
                    .WithRoute(context.Request.Path.HasValue ? context.Request.Path.Value : null);

                var error = apiErrorBuilder.Build();
                context.Response.StatusCode = 500;
                var json = JsonSerializer.Serialize(error,
                    new JsonSerializerOptions {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter() }
                    });
                await context.Response.WriteAsync(json);
                logger.LogWarning(error.ToString());
            }
            catch (Exception ex) {
                var apiErrorBuilder = LyoProblemDetailsBuilder.FromException(ex)
                    .WithTraceId(Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier)
                    .WithSpanId(Activity.Current?.SpanId.ToString())
                    .WithRoute(context.Request.Path.HasValue ? context.Request.Path.Value : null);

                var error = apiErrorBuilder.Build();
                context.Response.StatusCode = 500;
                var json = JsonSerializer.Serialize(error,
                    new JsonSerializerOptions {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter() }
                    });
                await context.Response.WriteAsync(json);
                logger.LogError(ex, "Unmanaged exception caught");
            }

            LogResponse(context);
        }
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