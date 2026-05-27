using System;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;
using Lyo.Authentication.Services.Opaque;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Api.Security;

/// <summary>One-shot startup helper that mints a <c>svc/live</c> token with <c>[config.admin]</c> the very first time the API boots with a legacy <c>ConfigApiSecurityOptions.ApiKey</c> set. The plaintext is logged once and the legacy key is no longer required afterwards.</summary>
public sealed class ConfigApiLegacyBootstrap(
    IOptions<ConfigApiSecurityOptions> legacy,
    IApiTokenIssuer issuer,
    ILogger<ConfigApiLegacyBootstrap> logger) : IHostedService
{
    private readonly ConfigApiSecurityOptions _legacy = legacy.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_legacy.RequireApiKey || string.IsNullOrWhiteSpace(_legacy.ApiKey))
            return;

        try {
            var issued = await issuer.IssueAsync(
                new ApiTokenIssueRequest(
                    Kind: "svc",
                    DisplayName: "config-api-legacy-bootstrap",
                    Scopes: ["config.admin"]),
                cancellationToken).ConfigureAwait(false);

            logger.LogWarning(
                "Config API legacy ApiKey detected. Minted a one-time svc/live token with [config.admin]. Replace ConfigApiSecurityOptions.ApiKey with this token: {Plaintext}",
                issued.Plaintext);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to mint legacy bootstrap token for Config API.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
