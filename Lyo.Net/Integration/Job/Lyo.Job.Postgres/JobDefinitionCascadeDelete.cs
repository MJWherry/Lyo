using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Job.Postgres;

/// <summary>
/// Removes all rows that reference a <see cref="JobDefinition" /> so the definition itself can be deleted without violating PostgreSQL foreign keys (<c>ClientSetNull</c> /
/// restrict semantics).
/// </summary>
public static class JobDefinitionCascadeDelete
{
    public static void RemoveDependents(JobContext db, Guid definitionId)
    {
        ArgumentNullException.ThrowIfNull(db);
        var workflowStepIds = db.JobWorkflowSteps.Where(s => s.JobDefinitionId == definitionId).Select(s => s.Id).ToList();
        var runs = db.JobRuns.Where(r => r.JobDefinitionId == definitionId)
            .Include(r => r.JobRunLogs)
            .Include(r => r.JobRunParameters)
            .Include(r => r.JobRunResults)
            .Include(r => r.InverseReRanFromJobRun)
            .Include(r => r.InverseTriggeredByJobRun)
            .Include(r => r.InverseParentJobRun)
            .ToList();

        var runIds = runs.Select(r => r.Id).ToHashSet();

        // Workflow run-steps reference runs and workflow steps — delete before either.
        if (runIds.Count > 0 || workflowStepIds.Count > 0) {
            var workflowRunSteps = db.JobWorkflowRunSteps.Where(s => (s.JobRunId != null && runIds.Contains(s.JobRunId.Value)) || workflowStepIds.Contains(s.JobWorkflowStepId))
                .ToList();

            db.JobWorkflowRunSteps.RemoveRange(workflowRunSteps);
        }

        foreach (var jobRun in runs) {
            foreach (var x in jobRun.InverseReRanFromJobRun)
                x.ReRanFromJobRunId = null;

            foreach (var x in jobRun.InverseTriggeredByJobRun)
                x.TriggeredByJobRunId = null;

            foreach (var x in jobRun.InverseParentJobRun)
                x.ParentJobRunId = null;

            db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
            db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
            db.JobRunResults.RemoveRange(jobRun.JobRunResults);
        }

        db.JobRuns.RemoveRange(runs);
        var schedules = db.JobSchedules.Where(s => s.JobDefinitionId == definitionId).Include(s => s.JobScheduleParameters).ToList();
        var calendarIds = schedules.Where(s => s.JobBlackoutCalendarId.HasValue).Select(s => s.JobBlackoutCalendarId!.Value).Distinct().ToList();
        foreach (var schedule in schedules)
            db.JobScheduleParameters.RemoveRange(schedule.JobScheduleParameters);

        var deletedScheduleIds = schedules.Select(s => s.Id).ToHashSet();
        db.JobSchedules.RemoveRange(schedules);

        // GC blackout calendars that no longer have any schedule references (exclude schedules pending delete —
        // they still exist in SQL until SaveChanges).
        foreach (var calendarId in calendarIds) {
            if (db.JobSchedules.Any(s => s.JobBlackoutCalendarId == calendarId && !deletedScheduleIds.Contains(s.Id)))
                continue;

            var windows = db.JobBlackoutWindows.Where(w => w.JobBlackoutCalendarId == calendarId).ToList();
            db.JobBlackoutWindows.RemoveRange(windows);
            var calendar = db.JobBlackoutCalendars.Find(calendarId);
            if (calendar is not null)
                db.JobBlackoutCalendars.Remove(calendar);
        }

        db.JobParameters.RemoveRange(db.JobParameters.Where(p => p.JobDefinitionId == definitionId).ToList());
        var triggers = db.JobTriggers.Where(t => t.JobDefinitionId == definitionId || t.TriggersJobDefinitionId == definitionId).Include(t => t.JobTriggerParameters).ToList();
        var triggerIds = triggers.Select(t => t.Id).ToHashSet();
        foreach (var run in db.JobRuns.Where(r => r.JobTriggerId != null && triggerIds.Contains(r.JobTriggerId.Value)).ToList())
            run.JobTriggerId = null;

        foreach (var trigger in triggers)
            db.JobTriggerParameters.RemoveRange(trigger.JobTriggerParameters);

        db.JobTriggers.RemoveRange(triggers);
        db.JobParallelRestrictions.RemoveRange(db.JobParallelRestrictions.Where(r => r.BaseJobDefinitionId == definitionId || r.OtherJobDefinitionId == definitionId).ToList());
        db.JobWorkflowSteps.RemoveRange(db.JobWorkflowSteps.Where(s => s.JobDefinitionId == definitionId).ToList());
    }
}