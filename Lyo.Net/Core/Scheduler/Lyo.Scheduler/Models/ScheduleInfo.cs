using Lyo.Schedule.Models;

namespace Lyo.Scheduler.Models;

/// <summary>Schedule metadata: ID, display name, and definition. Omits the action so it is safe to expose.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Optional display name.</param>
/// <param name="Definition">When the schedule fires.</param>
public sealed record ScheduleInfo(string Id, string? Name, ScheduleDefinition Definition);