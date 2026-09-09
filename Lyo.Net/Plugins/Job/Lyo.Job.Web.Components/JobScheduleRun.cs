namespace Lyo.Job.Web.Components;

/// <summary>
/// Builds a <see cref="JobRunReq" /> that creates a run from a schedule: the schedule id is set and enabled schedule parameters are copied. The API overlays omitted
/// keys from the stored schedule, then back-fills remaining definition defaults.
/// </summary>
internal static class JobScheduleRun
{
    /// <summary>Default <see cref="JobRunReq.CreatedBy" /> that matches <see cref="RunJobDialog" />.</summary>
    public const string CreatedBy = "gateway";

    /// <summary>Creates a run request for <paramref name="schedule" /> against <paramref name="definitionId" />.</summary>
    public static JobRunReq CreateRequest(Guid definitionId, JobScheduleRes schedule, string createdBy = CreatedBy, bool allowTriggers = true)
        => new(definitionId, createdBy, allowTriggers, scheduleId: schedule.Id) {
            JobRunParameters = [.. (schedule.Parameters ?? []).Where(p => p.Enabled).Select(ToRunParameter)]
        };

    private static JobRunParameterReq ToRunParameter(JobScheduleParameterRes p)
        => new() {
            Key = p.Key,
            Type = p.Type,
            Value = p.Value,
            Description = p.Description,
            EncryptedValue = p.EncryptedValue,
            Enabled = true
        };
}
