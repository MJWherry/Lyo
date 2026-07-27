namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// xunit collection for tests that invoke <c>JobMaintenanceService.RunMaintenanceAsync</c> (or seed runs that maintenance passes mutate). Maintenance scans and updates
/// runs/definitions globally, so two maintenance passes running in parallel — or a pass racing another test's writes — throw
/// <c>DbUpdateConcurrencyException</c>. Grouping these classes serializes them against each other while the rest of the suite still runs in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class JobMaintenanceCollection
{
    public const string Name = "JobMaintenance";
}
