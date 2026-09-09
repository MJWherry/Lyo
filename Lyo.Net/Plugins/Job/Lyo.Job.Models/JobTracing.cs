using System.Diagnostics;

namespace Lyo.Job.Models;

/// <summary>Distributed tracing helpers for the job subsystem (<c>ActivitySource</c> name is <c>Lyo.Job</c>).</summary>
public static class JobTracing
{
    /// <summary>Activity source for every job span.</summary>
    public static readonly ActivitySource Source = new("Lyo.Job");

    /// <summary>Starts a span when creating a job run.</summary>
    public static Activity? StartCreateRun(Guid definitionId, Guid? runId = null)
    {
        var activity = Source.StartActivity("JobRun.Create", ActivityKind.Server);
        activity?.SetTag("job.definition.id", definitionId);
        if (runId.HasValue)
            activity?.SetTag("job.run.id", runId.Value);

        return activity;
    }

    /// <summary>Starts a span for moving a run to <c>Running</c>.</summary>
    public static Activity? StartRun(Guid runId) => StartRunActivity("JobRun.Start", runId);

    /// <summary>Starts a span when finishing a run.</summary>
    public static Activity? FinishRun(Guid runId) => StartRunActivity("JobRun.Finish", runId);

    /// <summary>Starts a worker execution span, optionally linked to a parent trace id taken from the queue envelope.</summary>
    public static Activity? StartWorkerExecution(Guid runId, string workerType, string? parentTraceId = null)
    {
        Activity? activity;
        if (TryCreateTraceId(parentTraceId, out var traceId)) {
            var parentContext = new ActivityContext(traceId, default, ActivityTraceFlags.Recorded);
            activity = Source.StartActivity("JobRun.Execute", ActivityKind.Consumer, parentContext);
        }
        else
            activity = Source.StartActivity("JobRun.Execute", ActivityKind.Consumer);

        activity?.SetTag("job.run.id", runId);
        activity?.SetTag("job.worker.type", workerType);
        return activity;
    }

    /// <summary>Builds an <see cref="ActivityContext" /> from a valid W3C trace id string.</summary>
    public static ActivityContext? TryParseParentContext(string? traceId)
    {
        if (!TryCreateTraceId(traceId, out var parsed))
            return null;

        return new ActivityContext(parsed, default, ActivityTraceFlags.Recorded);
    }

    private static bool TryCreateTraceId(string? traceId, out ActivityTraceId parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(traceId))
            return false;

        try {
            parsed = ActivityTraceId.CreateFromString(traceId);
            return true;
        }
        catch (ArgumentOutOfRangeException) {
            return false;
        }
    }

    private static Activity? StartRunActivity(string name, Guid runId)
    {
        var activity = Source.StartActivity(name, ActivityKind.Server);
        activity?.SetTag("job.run.id", runId);
        return activity;
    }
}