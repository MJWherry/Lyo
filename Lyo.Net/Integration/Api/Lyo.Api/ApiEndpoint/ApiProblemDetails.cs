using System.Collections;
using Lyo.Api.Models.Error;
using Microsoft.AspNetCore.Http;

namespace Lyo.Api.ApiEndpoint;

public static class ApiErrorResponseFactory
{
    public static LyoProblemDetails CreateForError(HttpContext httpContext, LyoProblemDetails? error, IEnumerable<object?>? keys = null)
    {
        var source = error ?? LyoProblemDetails.FromCode(Models.Constants.ApiErrorCodes.Unknown, "Request failed.");
        var instance = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null;
        var extensions = source.Extensions != null ? new Dictionary<string, object?>(source.Extensions) : new Dictionary<string, object?>();
        AddKeysExtension(extensions, keys);
        return source with { Instance = instance, Extensions = extensions };
    }

    public static LyoProblemDetails CreateNotFound(HttpContext httpContext, IEnumerable<object?>? keys = null, string? detail = null)
    {
        var instance = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null;
        var extensions = new Dictionary<string, object?>();
        AddKeysExtension(extensions, keys);
        return LyoProblemDetails.FromCode(
            Models.Constants.ApiErrorCodes.NotFound,
            detail ?? "Resource was not found.",
            DateTime.UtcNow,
            httpContext.TraceIdentifier,
            instance,
            extensions);
    }

    private static void AddKeysExtension(IDictionary<string, object?> extensions, IEnumerable<object?>? keys)
    {
        if (keys is null)
            return;

        var normalizedKeys = keys.Select(NormalizeKey).Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
        if (normalizedKeys.Length > 0)
            extensions["keys"] = normalizedKeys;
    }

    private static string NormalizeKey(object? key)
    {
        if (key is null)
            return string.Empty;

        if (key is string value)
            return value;

        if (key is IEnumerable enumerable) {
            var values = new List<string>();
            foreach (var item in enumerable)
                values.Add(item?.ToString() ?? "null");

            return values.Count > 0 ? $"[{string.Join(",", values)}]" : string.Empty;
        }

        return key.ToString() ?? string.Empty;
    }
}
