using System.ComponentModel.DataAnnotations;
using Lyo.Job.Postgres.Database;

namespace Lyo.Job.Postgres;

/// <summary>Write-time check that a schedule has exactly one owner, so the API answers with a validation error instead of a constraint-violation 500.</summary>
internal static class JobScheduleWriteValidator
{
    /// <summary>Throws when <paramref name="schedule" /> fires both a definition and a workflow, or neither.</summary>
    public static void ValidateOwner(JobSchedule schedule)
    {
        var owners = (schedule.JobDefinitionId.HasValue ? 1 : 0) + (schedule.JobWorkflowId.HasValue ? 1 : 0);
        if (owners != 1)
            throw new ValidationException("A schedule must set exactly one of JobDefinitionId and JobWorkflowId.");
    }
}
