using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Constants = Lyo.Job.Models.Constants;
using Lyo.Exceptions;

namespace Lyo.Job.Scheduler;

/// <summary>Folds a batch parent's child-run outcomes and counters into the result set the parent finishes with.</summary>
public static class JobBatchResultAggregator
{
    /// <summary>
    /// Folds <paramref name="children" /> into parent results: the worst outcome wins, and the create/update/delete/failed counters are summed (omitted when zero).
    /// </summary>
    /// <param name="children">Completed child runs of one parent, with their results loaded.</param>
    public static IReadOnlyList<JobRunResultReq> Aggregate(IReadOnlyList<JobRunRes> children)
    {
        ArgumentHelpers.ThrowIfNull(children);
        var anyFailure = children.Any(c => JobRunOutcome.IsFailure(c.Result));
        var anyPartial = children.Any(c => c.Result == JobRunResult.PartialSuccess);
        var anyWarning = children.Any(c => c.Result == JobRunResult.SuccessWithWarnings);
        var outcome = anyFailure ? JobRunResult.Failure : anyPartial ? JobRunResult.PartialSuccess : anyWarning ? JobRunResult.SuccessWithWarnings : JobRunResult.Success;
        var results = new List<JobRunResultReq> {
            new(Constants.Data.JobRunResultKey.Result, LyoTypeInfo.String, outcome.ToString()), new("ChildCount", LyoTypeInfo.Int, children.Count)
        };

        AddCounter(results, children, Constants.Data.JobRunResultKey.CreateCount);
        AddCounter(results, children, Constants.Data.JobRunResultKey.UpdateCount);
        AddCounter(results, children, Constants.Data.JobRunResultKey.DeleteCount);
        AddCounter(results, children, Constants.Data.JobRunResultKey.FailedCount);
        return results;
    }

    private static void AddCounter(List<JobRunResultReq> results, IReadOnlyList<JobRunRes> children, string key)
    {
        var total = children.Sum(c => c.GetResultValueAs<int?>(key) ?? 0);
        if (total > 0)
            results.Add(new(key, LyoTypeInfo.Int, total));
    }
}
