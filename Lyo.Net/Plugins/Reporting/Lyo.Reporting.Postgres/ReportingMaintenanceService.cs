using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Lyo.Reporting.Postgres;

/// <summary>
/// Opt-in hosted worker (<c>AddReportingMaintenanceWorker</c>) that runs <see cref="ReportRetentionService.CleanupAsync(CancellationToken)" /> (stuck-run recovery plus
/// retention cleanup) every <see cref="PostgresReportingOptions.MaintenanceInterval" />. Hosts that already schedule cleanup themselves (for example via Lyo.Scheduler or a
/// Lyo.Job interval job) do not need this.
/// </summary>
public sealed class ReportingMaintenanceService(
    IServiceScopeFactory scopeFactory,
    PostgresReportingOptions options,
    ILogger<ReportingMaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.MaintenanceInterval;
        logger.LogInformation("Reporting maintenance worker started (interval {Interval})", interval);
        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await using var scope = scopeFactory.CreateAsyncScope();
                var retention = scope.ServiceProvider.GetRequiredService<ReportRetentionService>();
                await retention.CleanupAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) {
                logger.LogError(ex, "Reporting maintenance pass failed; retrying next interval");
            }

            try {
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                    break;
            }
            catch (OperationCanceledException) {
                break;
            }
        }
    }
}