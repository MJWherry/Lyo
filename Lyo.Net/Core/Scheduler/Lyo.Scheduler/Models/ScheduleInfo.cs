using Lyo.Schedule.Models;

namespace Lyo.Scheduler.Models;

/// <summary>Schedule metadata: ID, display name, and definition. Excludes the action for safe exposure.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Optional display name.</param>
/// <param name="Definition">The schedule definition.</param>
public sealed record ScheduleInfo(string Id, string? Name, ScheduleDefinition Definition);