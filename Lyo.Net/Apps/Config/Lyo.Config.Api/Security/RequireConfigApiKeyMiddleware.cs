using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lyo.Config.Api.Security;

/// <summary>Registers shared-secret checks for centrally hosted config endpoints.</summary>
public sealed class ConfigApiSecurityOptions
{
    public const string SectionName = "ConfigApiSecurity";

    /// <summary>When true, requests under /api/config must authenticate using <see cref="ApiKey" />.</summary>
    public bool RequireApiKey { get; set; }

    /// <summary>Secret supplied via <c>X-Api-Key</c> or <c>Authorization: Bearer</c>.</summary>
    public string? ApiKey { get; set; }
}

/// <summary>Checks opt-in secrets before map groups under /api/config succeed.</summary>
public sealed class RequireConfigApiKeyMiddleware(RequestDelegate next, IOptions<ConfigApiSecurityOptions> optionsAccessor)
{
    private readonly RequestDelegate _next = next;
    private readonly ConfigApiSecurityOptions _options = optionsAccessor.Value;

    public Task Invoke(HttpContext ctx) => !_options.RequireApiKey ? _next(ctx) : InvokeProtectedAsync(ctx);

    private async Task InvokeProtectedAsync(HttpContext ctx)
    {
        if (!ctx.Request.Path.StartsWithSegments("/api/config", StringComparison.OrdinalIgnoreCase)) {
            await _next(ctx).ConfigureAwait(false);
            return;
        }

        var expected = (_options.ApiKey ?? string.Empty).Trim();
        if (expected.Length == 0) {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { detail = "API key enforcement is enabled but no server key has been configured." }).ConfigureAwait(false);
            return;
        }

        if (!TryExtractCredentialUtf8(ctx.Request.Headers, out var credential)) {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var baseline = Encoding.UTF8.GetBytes(expected);
        var credentialBytes = credential;
        var matches = baseline.Length == credentialBytes.Length && CryptographicOperations.FixedTimeEquals(baseline, credentialBytes);
        CryptographicOperations.ZeroMemory(credentialBytes);
        if (!matches) {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(ctx).ConfigureAwait(false);
    }

    private static bool TryExtractCredentialUtf8(IHeaderDictionary headers, [NotNullWhen(true)] out byte[]? credential)
    {
        if (headers.TryGetValue("X-Api-Key", out var headerValue)) {
            var trimmed = TrimHeaderSecret(headerValue);
            if (!string.IsNullOrEmpty(trimmed)) {
                credential = Encoding.UTF8.GetBytes(trimmed);
                return true;
            }
        }

        var auth = headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth)) {
            credential = null;
            return false;
        }

        if (!AuthenticationHeaderValue.TryParse(auth, out var parsed) || !string.Equals(parsed.Scheme.Trim(), "Bearer", StringComparison.OrdinalIgnoreCase)) {
            credential = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(parsed.Parameter)) {
            credential = null;
            return false;
        }

        credential = Encoding.UTF8.GetBytes(parsed.Parameter.Trim());
        return credential.Length != 0;
    }

    private static string TrimHeaderSecret(StringValues supplied) => supplied.ToString().Trim();
}