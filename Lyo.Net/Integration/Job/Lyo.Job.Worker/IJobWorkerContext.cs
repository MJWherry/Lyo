using Lyo.Job.Models.Response;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Worker;

/// <summary>
/// Provides all context a worker implementation needs during execution: the fully-loaded run, a structured logger, a cancellation token, and a fluent result builder for
/// reporting outcomes back to the job server.
/// </summary>
public interface IJobWorkerContext
{
    /// <summary>The fully-loaded job run, including parameters, definition, and schedule.</summary>
    JobRunRes Run { get; }

    /// <summary>Structured logger scoped to this run.</summary>
    ILogger Logger { get; }

    /// <summary>
    /// Token that is cancelled when the host is shutting down or a cancellation request is received via
    /// <see cref="Lyo.Job.Models.Events.IJobEventPublisher.SubscribeToRunCancellationsAsync" />.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Fluent builder for collecting output results to report when the run finishes.</summary>
    JobWorkerResultBuilder Results { get; }
}