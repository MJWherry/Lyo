using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Worker;

/// <summary>
/// Everything a worker needs while a run executes: the loaded run, a structured logger, a cancellation token, and a fluent result builder for
/// sending outcomes back to the job server.
/// </summary>
public interface IJobWorkerContext
{
    /// <summary>Fully loaded job run, including parameters, definition, and schedule.</summary>
    JobRunRes Run { get; }

    /// <summary>Structured logger scoped to this run.</summary>
    ILogger Logger { get; }

    /// <summary>
    /// Token cancelled when the host shuts down or a cancel request arrives through
    /// <see cref="Lyo.Job.Models.Events.IJobEventPublisher.SubscribeToRunCancellationsAsync" />.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Fluent builder that collects output results to report when the run finishes.</summary>
    JobWorkerResultBuilder Results { get; }

    /// <summary>Sends execution progress to the Job API (percent complete and an optional status message).</summary>
    Task ReportProgressAsync(int percent, string? message = null, CancellationToken ct = default);

    /// <summary>Creates fan-out child runs under the current parent via the Job API.</summary>
    Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(JobCreateChildRunsReq request, CancellationToken ct = default);

    /// <summary>
    /// Adds or replaces a named object in the format bag (for example <c>AddContext("client", client)</c>) and re-formats in-memory
    /// <see cref="LyoTypeInfo.String"/> parameter values from their original templates. Unresolved <c>{tokens}</c> stay as-is.
    /// Json, Xml, and collection parameters are never formatted.
    /// </summary>
    /// <param name="name">Context key used in placeholders such as <c>{client.contact.emailAddress}</c>.</param>
    /// <param name="value">Object, dictionary, or scalar to bind. Null is allowed and replaces a previous value.</param>
    void AddContext(string name, object? value);

    /// <summary>Formats <paramref name="template"/> against the current bag (<c>jobrun</c> plus anything added via <see cref="AddContext"/>).</summary>
    /// <param name="template">SmartFormat or expression template, for example <c>Since {DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}</c>.</param>
    /// <returns>The formatted string. Unresolved tokens stay in the output.</returns>
    string Format(string template);
}