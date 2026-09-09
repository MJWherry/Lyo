namespace Lyo.Web.Automation.Abstractions;

/// <summary>Store JSON payloads produced by automation steps (for example DB upsert pipelines).</summary>
public interface IAutomationPlanDataSink
{
    /// <summary>Upserts a single JSON payload into a named target.</summary>
    Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct);
}