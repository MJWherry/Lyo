using Lyo.Reporting.Models;
namespace Lyo.Reporting.Postgres;

/// <summary>
/// Process-wide concurrency gate for report generation, sized by <see cref="PostgresReportingOptions.MaxConcurrentGenerations" />. Registered as a singleton so every scoped
/// <see cref="ReportService" /> instance shares the same limit.
/// </summary>
public sealed class ReportGenerationThrottle(PostgresReportingOptions options)
{
    private readonly int _maxConcurrent = options.MaxConcurrentGenerations;

    private readonly SemaphoreSlim? _semaphore = options.MaxConcurrentGenerations > 0
        ? new SemaphoreSlim(options.MaxConcurrentGenerations, options.MaxConcurrentGenerations)
        : null;

    /// <summary>How long generate waits for a slot before failing with <see cref="ReportBusyException" />. Internal, for tests.</summary>
    internal TimeSpan AcquireTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Acquires a generation slot, or returns null when no limit is configured. Dispose the releaser to release the slot.</summary>
    public async Task<IDisposable?> AcquireAsync(CancellationToken ct)
    {
        if (_semaphore is null)
            return null;

        if (!await _semaphore.WaitAsync(AcquireTimeout, ct).ConfigureAwait(false))
            throw new ReportBusyException($"Report generation is busy: {_maxConcurrent} generation(s) already running. Try again later.");

        return new Releaser(_semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                semaphore.Release();
        }
    }
}