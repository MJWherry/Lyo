using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Cli.Services;

/// <summary>HTTP execution of Lyo query requests via <see cref="IApiClient" />.</summary>
internal static class CliQueryExecutor
{
    public static string BuildUri(QueryMode mode, string? basePath, string? route)
    {
        var prefix = string.IsNullOrWhiteSpace(basePath) ? "" : basePath.Trim().Trim('/') + "/";
        return mode switch {
            QueryMode.Concrete => $"{prefix}{RequireRoute(route)}/QueryConcrete",
            QueryMode.Project => $"{prefix}{RequireRoute(route)}/QueryProject",
            QueryMode.Root => $"{prefix.TrimEnd('/')}/Query".TrimStart('/'),
            var _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    public static async Task<string> ExecAsync(
        QueryMode mode,
        string baseUrl,
        string? basePath,
        string? route,
        object body,
        string? token,
        IEnumerable<string>? headers,
        bool raw,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(baseUrl);
        var uri = BuildUri(mode, basePath, route);
        var services = new ServiceCollection();
        services.AddLyoApiClient(optionsOverride: o => o.BaseUrl = baseUrl.TrimEnd('/'), propagateCorrelationId: false);
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IApiClient>();
        try {
            var result = await client.PostAsAsync<object, JsonElement>(
                    uri, body, req => {
                        if (!string.IsNullOrWhiteSpace(token))
                            req.Headers.Authorization = new("Bearer", token);

                        foreach (var h in headers ?? []) {
                            var idx = h.IndexOf(':');
                            ArgumentHelpers.ThrowIf(idx <= 0, $"Invalid --header '{h}'. Expected NAME:VALUE.");
                            req.Headers.TryAddWithoutValidation(h[..idx].Trim(), h[(idx + 1)..].Trim());
                        }
                    }, ct)
                .ConfigureAwait(false);

            if (raw)
                return result.GetRawText();

            return JsonSerializer.Serialize(result, LyoJsonSerializerOptions.Create(o => o.WriteIndented = true));
        }
        catch (ApiException ex) {
            await Console.Error.WriteLineAsync($"API error {ex.StatusCode}: {ex.Message}").ConfigureAwait(false);
            if (ex.ProblemDetails is not null)
                await Console.Error.WriteLineAsync(JsonSerializer.Serialize(ex.ProblemDetails, LyoJsonSerializerOptions.Create(o => o.WriteIndented = true))).ConfigureAwait(false);

            throw;
        }
    }

    private static string RequireRoute(string? route)
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(route), "--route is required for concrete/project modes.");
        return route!.Trim().Trim('/');
    }
}