using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Worker;

/// <summary>
/// Fluent builder for collecting <see cref="JobRunResultReq"/> entries while a job executes.
/// Call <see cref="Build"/> at the end to get the list to pass to the finish endpoint.
/// </summary>
public sealed class JobWorkerResultBuilder
{
    private readonly List<JobRunResultReq> _results = [];
    private JobRunResult _outcome = JobRunResult.Success;

    /// <summary>
    /// Sets the overall outcome reported to the server. Defaults to <see cref="JobRunResult.Success"/>.
    /// Call <see cref="Fail"/> or <see cref="Cancel"/> instead of setting this manually in most cases.
    /// </summary>
    public JobWorkerResultBuilder SetOutcome(JobRunResult outcome)
    {
        _outcome = outcome;
        return this;
    }

    /// <summary>Marks the run as <see cref="JobRunResult.Failure"/>.</summary>
    public JobWorkerResultBuilder Fail() => SetOutcome(JobRunResult.Failure);

    /// <summary>Marks the run as <see cref="JobRunResult.Cancelled"/>.</summary>
    public JobWorkerResultBuilder Cancel() => SetOutcome(JobRunResult.Cancelled);

    /// <summary>Marks the run as <see cref="JobRunResult.SuccessWithWarnings"/>.</summary>
    public JobWorkerResultBuilder SucceedWithWarnings() => SetOutcome(JobRunResult.SuccessWithWarnings);
    
    /// <summary>Adds an arbitrary key/value result entry.</summary>
    public JobWorkerResultBuilder AddResult(string key, object? value, JobParameterType type = JobParameterType.String)
    {
        _results.Add(new JobRunResultReq(key, type, value));
        return this;
    }

    /// <summary>Adds an integer count result (e.g. <c>CreateCount</c>, <c>UpdateCount</c>).</summary>
    public JobWorkerResultBuilder AddCount(string key, int count)
    {
        _results.Add(new JobRunResultReq(key, count));
        return this;
    }

    /// <summary>Records a human-readable failure reason. Automatically calls <see cref="Fail"/>.</summary>
    public JobWorkerResultBuilder AddError(string reason, int index = -1)
    {
        var key = index >= 0 ? Constants.Data.JobRunResultKey.FailureReason(index) : Constants.Data.JobRunResultKey.FailureReason(0);
        _results.Add(new JobRunResultReq(key, JobParameterType.String, reason));
        return Fail();
    }

    /// <summary>Records a failed item identifier alongside a reason. Automatically calls <see cref="Fail"/>.</summary>
    public JobWorkerResultBuilder AddFailedItem(int index, string item, string? reason = null)
    {
        _results.Add(new JobRunResultReq(Constants.Data.JobRunResultKey.FailedItem(index), JobParameterType.String, item));
        if (reason != null)
            _results.Add(new JobRunResultReq(Constants.Data.JobRunResultKey.FailureReason(index), JobParameterType.String, reason));

        return Fail();
    }

    /// <summary>Records how long an external API call took.</summary>
    public JobWorkerResultBuilder AddApiCallTime(string apiName, long milliseconds)
    {
        _results.Add(new JobRunResultReq(Constants.Data.JobRunResultKey.ApiCallTime(apiName), JobParameterType.Long, milliseconds));
        return this;
    }
    
    /// <summary>Records <c>CreateCount</c>.</summary>
    public JobWorkerResultBuilder AddCreateCount(int count) => AddCount(Constants.Data.JobRunResultKey.CreateCount, count);

    /// <summary>Records <c>UpdateCount</c>.</summary>
    public JobWorkerResultBuilder AddUpdateCount(int count) => AddCount(Constants.Data.JobRunResultKey.UpdateCount, count);

    /// <summary>Records <c>DeleteCount</c>.</summary>
    public JobWorkerResultBuilder AddDeleteCount(int count) => AddCount(Constants.Data.JobRunResultKey.DeleteCount, count);

    /// <summary>Records <c>FailedCount</c>.</summary>
    public JobWorkerResultBuilder AddFailedCount(int count) => AddCount(Constants.Data.JobRunResultKey.FailedCount, count);

    /// <summary>Records <c>NoChangeCount</c>.</summary>
    public JobWorkerResultBuilder AddNoChangeCount(int count) => AddCount(Constants.Data.JobRunResultKey.NoChangeCount, count);
    
    /// <summary>
    /// Finalises the result list. Injects the <c>Result</c> key with the current
    /// <see cref="_outcome"/> value so the server can parse it.
    /// </summary>
    public IReadOnlyList<JobRunResultReq> Build()
    {
        var all = new List<JobRunResultReq>(_results)
        {
            new(Constants.Data.JobRunResultKey.Result, JobParameterType.String, _outcome.ToString())
        };
        return all.AsReadOnly();
    }

    /// <summary>The current outcome that will be written by <see cref="Build"/>.</summary>
    public JobRunResult CurrentOutcome => _outcome;
}
