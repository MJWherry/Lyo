using Lyo.Job.Models.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Postgres;

/// <summary>
/// Connects <see cref="IJobEventPublisher" /> to the message queue at host startup. Registered automatically by <see cref="Extensions.AddMqJobEventPublisher" /> so API hosts can
/// enqueue job runs without also running <see cref="Lyo.Job.Scheduler.JobScheduler" /> or a worker.
/// </summary>
internal sealed class JobEventPublisherStartupService : IHostedService
{
    private readonly IJobEventPublisher _eventPublisher;
    private readonly ILogger<JobEventPublisherStartupService> _logger;

    public JobEventPublisherStartupService(IJobEventPublisher eventPublisher, ILogger<JobEventPublisherStartupService> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting job event publisher to message queue...");
        await _eventPublisher.SetupAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Job event publisher connected (IsConnected={IsConnected})", _eventPublisher.IsConnected());
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
