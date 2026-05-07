using Lyo.Config.Api.Client;
using Lyo.Config.Api.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Api.Hosting;

public sealed class ConfigApiPollingHostedService : BackgroundService
{
    private readonly IConfigApiClient _client;
    private readonly ConfigApiResolvedLedger _ledger;
    private readonly ILogger _log;
    private readonly IOptions<ConfigApiPollingOptions> _pollingOptions;

    public ConfigApiPollingHostedService(
        IConfigApiClient client,
        IOptions<ConfigApiPollingOptions> pollingOptions,
        ConfigApiResolvedLedger ledger,
        ILogger<ConfigApiPollingHostedService> log)
    {
        _client = client;
        _pollingOptions = pollingOptions;
        _ledger = ledger;
        _log = log;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        var o = _pollingOptions.Value;
        o.ThrowIfMisconfiguredWhenEnabled();
        if (!o.Enabled) {
            await base.StartAsync(ct).ConfigureAwait(false);
            return;
        }

        await EnsureFirstResolveAsync(o, ct).ConfigureAwait(false);
        await base.StartAsync(ct).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = _pollingOptions.Value;
        if (!o.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested) {
            try {
                var result = await _client.ResolveForAppAsync(o.AppKind, o.AppId, _ledger.CurrentEtag, null, false, stoppingToken).ConfigureAwait(false);
                switch (result.Outcome) {
                    case ConfigResolveOutcome.Ok:
                        if (result.Resolved == null) {
                            _log.LogWarning("Config API returned OK without a resolved body; retrying.");
                            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                            break;
                        }

                        _ledger.SetResolved(result.Resolved, result.ETag);
                        break;
                    case ConfigResolveOutcome.NotModified:
                        await DelayWhenNotModified(o, stoppingToken).ConfigureAwait(false);
                        break;
                    case ConfigResolveOutcome.Failed:
                        _log.LogWarning("Config API poll failed: HTTP {Status} {Reason}", result.Failure?.StatusCode, result.Failure?.ReasonPhrase);
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) {
                _log.LogError(ex, "Unhandled error during Config API polling; retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task DelayWhenNotModified(ConfigApiPollingOptions o, CancellationToken stoppingToken)
    {
        var d = o.DelayWhenNotModified;
        await Task.Delay(d <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : d, stoppingToken).ConfigureAwait(false);
    }

    private async Task EnsureFirstResolveAsync(ConfigApiPollingOptions o, CancellationToken ct)
    {
        var deadlineUtc = o.StartupTimeout is { TotalMilliseconds: > 0 } ? DateTime.UtcNow + o.StartupTimeout.Value : (DateTime?)null;
        while (!ct.IsCancellationRequested) {
            if (deadlineUtc is not null && DateTime.UtcNow > deadlineUtc.Value) {
                if (o.RequireSuccessOnStartup) {
                    throw new TimeoutException($"Timed out resolving Config API snapshot before '{nameof(ConfigApiPollingOptions.StartupTimeout)}'.");
                }

                _log.LogWarning(
                    "{StartupTimeoutName} elapsed without a snapshot and {Require} is disabled; continuing without ledger data.", nameof(ConfigApiPollingOptions.StartupTimeout),
                    nameof(ConfigApiPollingOptions.RequireSuccessOnStartup));

                return;
            }

            var result = await _client.ResolveForAppAsync(o.AppKind, o.AppId, _ledger.CurrentEtag, null, false, ct).ConfigureAwait(false);
            switch (result.Outcome) {
                case ConfigResolveOutcome.Ok:
                    if (result.Resolved == null) {
                        _log.LogWarning("Config API returned OK without a resolved body during startup; retrying.");
                        await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                        continue;
                    }

                    _ledger.SetResolved(result.Resolved, result.ETag);
                    return;
                case ConfigResolveOutcome.NotModified:
                    _log.LogDebug("Config API returned 304 during startup; snapshot should already be present if the ledger was primed.");
                    await DelayWhenNotModified(o, ct).ConfigureAwait(false);
                    continue;
                case ConfigResolveOutcome.Failed:
                    _log.LogWarning("Config API startup resolve failed: HTTP {Status} {Reason}", result.Failure?.StatusCode, result.Failure?.ReasonPhrase);
                    await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                    continue;
                default:
                    await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                    continue;
            }
        }

        ct.ThrowIfCancellationRequested();
    }
}