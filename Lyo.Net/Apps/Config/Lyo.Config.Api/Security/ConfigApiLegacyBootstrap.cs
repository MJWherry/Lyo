using Lyo.Authentication.Services.Opaque;

namespace Lyo.Config.Api.Security;

/// <summary>
/// Startup one-shot: if a legacy <c>ConfigApiSecurityOptions.ApiKey</c> is present on first boot, issues a <c>svc/live</c> token
/// scoped <c>[config.admin]</c>. Logs the plaintext once; after that the old key is unused.
/// </summary>
public sealed class ConfigApiLegacyBootstrap(ConfigApiSecurityOptions legacy, IApiTokenIssuer issuer, ILogger<ConfigApiLegacyBootstrap> logger) : IHostedService
{
    private readonly ConfigApiSecurityOptions _legacy = legacy;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_legacy.RequireApiKey || string.IsNullOrWhiteSpace(_legacy.ApiKey))
            return;

        try {
            var issued = await issuer.IssueAsync(new("svc", "config-api-legacy-bootstrap", ["config.admin"]), cancellationToken).ConfigureAwait(false);
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