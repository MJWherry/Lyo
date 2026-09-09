using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Worker;

/// <summary>
/// Fluent builder that collects <see cref="JobRunResultReq" /> entries while a job runs. Call <see cref="Build" /> at the end to get the list for the finish
/// endpoint.
/// </summary>
public sealed class JobWorkerResultBuilder
{
    private readonly List<JobRunResultReq> _results = [];
    private JobRunResult _outcome = JobRunResult.Success;

    /// <summary>Outcome that <see cref="Build" /> will write.</summary>
    public JobRunResult CurrentOutcome => _outcome;

    /// <summary>
    /// Sets the overall outcome sent to the server. Defaults to <see cref="JobRunResult.Success" />. Prefer <see cref="Fail" /> or <see cref="Cancel" /> instead of setting
    /// this by hand in most cases.
    /// </summary>
    public JobWorkerResultBuilder SetOutcome(JobRunResult outcome)
    {
        _outcome = outcome;
        return this;
    }

    /// <summary>Sets the run to <see cref="JobRunResult.Failure" />.</summary>
    public JobWorkerResultBuilder Fail() => SetOutcome(JobRunResult.Failure);

    /// <summary>Sets the run to <see cref="JobRunResult.Cancelled" />.</summary>
    public JobWorkerResultBuilder Cancel() => SetOutcome(JobRunResult.Cancelled);

    /// <summary>Sets the run to <see cref="JobRunResult.SuccessWithWarnings" />.</summary>
    public JobWorkerResultBuilder SucceedWithWarnings() => SetOutcome(JobRunResult.SuccessWithWarnings);

    /// <summary>Adds a key/value result entry.</summary>
    public JobWorkerResultBuilder AddResult(string key, object? value, LyoTypeInfo? type = null)
    {
        _results.Add(new(key, type ?? LyoTypeInfo.String, value));
        return this;
    }

    /// <summary>Adds an integer count result (for example <c>CreateCount</c>, <c>UpdateCount</c>).</summary>
    public JobWorkerResultBuilder AddCount(string key, int count)
    {
        _results.Add(new(key, count));
        return this;
    }

    /// <summary>Records a human-readable failure reason. Also calls <see cref="Fail" />.</summary>
    public JobWorkerResultBuilder AddError(string reason, int index = -1)
    {
        var key = index >= 0 ? Constants.Data.JobRunResultKey.FailureReason(index) : Constants.Data.JobRunResultKey.FailureReason(0);
        _results.Add(new(key, LyoTypeInfo.String, reason));
        return Fail();
    }

    /// <summary>Records a failed item id plus a reason. Also calls <see cref="Fail" />.</summary>
    public JobWorkerResultBuilder AddFailedItem(int index, string item, string? reason = null)
    {
        _results.Add(new(Constants.Data.JobRunResultKey.FailedItem(index), LyoTypeInfo.String, item));
        if (reason != null)
            _results.Add(new(Constants.Data.JobRunResultKey.FailureReason(index), LyoTypeInfo.String, reason));

        return Fail();
    }

    /// <summary>Records how long an external API call lasted.</summary>
    public JobWorkerResultBuilder AddApiCallTime(string apiName, long milliseconds)
    {
        _results.Add(new(Constants.Data.JobRunResultKey.ApiCallTime(apiName), LyoTypeInfo.Long, milliseconds));
        return this;
    }

    /// <summary>Writes <c>CreateCount</c>.</summary>
    public JobWorkerResultBuilder AddCreateCount(int count) => AddCount(Constants.Data.JobRunResultKey.CreateCount, count);

    /// <summary>Writes <c>UpdateCount</c>.</summary>
    public JobWorkerResultBuilder AddUpdateCount(int count) => AddCount(Constants.Data.JobRunResultKey.UpdateCount, count);

    /// <summary>Writes <c>DeleteCount</c>.</summary>
    public JobWorkerResultBuilder AddDeleteCount(int count) => AddCount(Constants.Data.JobRunResultKey.DeleteCount, count);

    /// <summary>Writes <c>FailedCount</c>.</summary>
    public JobWorkerResultBuilder AddFailedCount(int count) => AddCount(Constants.Data.JobRunResultKey.FailedCount, count);

    /// <summary>Writes <c>NoChangeCount</c>.</summary>
    public JobWorkerResultBuilder AddNoChangeCount(int count) => AddCount(Constants.Data.JobRunResultKey.NoChangeCount, count);

    /// <summary>Builds the result list. Adds the <c>Result</c> key with the current <see cref="_outcome" /> so the server can parse it.</summary>
    public IReadOnlyList<JobRunResultReq> Build()
    {
        var all = new List<JobRunResultReq>(_results) { new(Constants.Data.JobRunResultKey.Result, LyoTypeInfo.String, _outcome.ToString()) };
        return all.AsReadOnly();
    }
}