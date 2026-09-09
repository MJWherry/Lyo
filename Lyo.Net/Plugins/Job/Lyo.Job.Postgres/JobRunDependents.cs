using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Lyo.Exceptions;

namespace Lyo.Job.Postgres;

/// <summary>
/// Shared primitives that make a <see cref="JobRun" /> deletable. A run is referenced by three child collections and by three self-referencing FKs on other runs. PostgreSQL is
/// mapped with restrict / <c>ClientSetNull</c> semantics, so every delete path (definition cascade, single-run delete endpoint, retention purge) has to detach and remove
/// the same graph.
/// Call <see cref="IncludeDependents" /> when loading the runs, then <see cref="Remove" /> per run before <c>RemoveRange</c> on the runs themselves.
/// Workflow run-steps are deliberately not handled here: the delete paths remove them while retention purge only nulls their run reference.
/// </summary>
public static class JobRunDependents
{
    /// <summary>Loads the child collections and inverse navigations <see cref="Remove" /> needs, as a split query so the six collections do not multiply rows together.</summary>
    /// <param name="query">Run query to expand.</param>
    public static IQueryable<JobRun> IncludeDependents(IQueryable<JobRun> query)
    {
        ArgumentHelpers.ThrowIfNull(query);
        return query.Include(r => r.JobRunLogs)
            .Include(r => r.JobRunParameters)
            .Include(r => r.JobRunResults)
            .Include(r => r.InverseReRanFromJobRun)
            .Include(r => r.InverseTriggeredByJobRun)
            .Include(r => r.InverseParentJobRun)
            .AsSplitQuery();
    }

    /// <summary>Nulls the self-referencing FKs of runs that survive, so deleting <paramref name="run" /> does not violate those FKs.</summary>
    /// <param name="run">Run being deleted. Must have been loaded through <see cref="IncludeDependents" />.</param>
    public static void DetachInverseReferences(JobRun run)
    {
        ArgumentHelpers.ThrowIfNull(run);
        foreach (var child in run.InverseReRanFromJobRun)
            child.ReRanFromJobRunId = null;

        foreach (var child in run.InverseTriggeredByJobRun)
            child.TriggeredByJobRunId = null;

        foreach (var child in run.InverseParentJobRun)
            child.ParentJobRunId = null;
    }

    /// <summary>Marks the run's logs, parameters, and results to be deleted.</summary>
    /// <param name="db">Context tracking <paramref name="run" />.</param>
    /// <param name="run">Run being deleted. Must have been loaded through <see cref="IncludeDependents" />.</param>
    public static void RemoveChildren(JobContext db, JobRun run)
    {
        ArgumentHelpers.ThrowIfNull(db);
        ArgumentHelpers.ThrowIfNull(run);
        db.JobRunLogs.RemoveRange(run.JobRunLogs);
        db.JobRunParameters.RemoveRange(run.JobRunParameters);
        db.JobRunResults.RemoveRange(run.JobRunResults);
    }

    /// <summary>Detaches inverse references and removes children. The caller still has to remove the run itself.</summary>
    /// <param name="db">Context tracking <paramref name="run" />.</param>
    /// <param name="run">Run being deleted. Must have been loaded through <see cref="IncludeDependents" />.</param>
    public static void Remove(JobContext db, JobRun run)
    {
        DetachInverseReferences(run);
        RemoveChildren(db, run);
    }
}
