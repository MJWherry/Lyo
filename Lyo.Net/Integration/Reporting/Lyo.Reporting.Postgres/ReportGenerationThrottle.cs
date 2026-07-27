using Lyo.Reporting.Models;
using Microsoft.Extensions.Options;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Process-wide concurrency gate for report generation, sized by <see cref="PostgresReportingOptions.MaxConcurrentGenerations"/>.
/// Registered as a singleton so all scoped <see cref="ReportService"/> instances share the same limit.
/// </summary>
public sealed class ReportGenerationThrottle(IOptions<PostgresReportingOptions> options)
{
    /// <summary>How long generate waits for a slot before failing with <see cref="ReportBusyException"/>. Internal for tests.</summary>
    internal TimeSpan AcquireTimeout { get; init; } = TimeSpan.FromSeconds(10);

    private readonly int _maxConcurrent = options.Value.MaxConcurrentGenerations;
    private readonly SemaphoreSlim? _semaphore = options.Value.MaxConcurrentGenerations > 0
        ? new SemaphoreSlim(options.Value.MaxConcurrentGenerations, options.Value.MaxConcurrentGenerations)
        : null;

    /// <summary>Acquires a generation slot, or returns null when no limit is configured. Dispose the releaser to free the slot.</summary>
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
