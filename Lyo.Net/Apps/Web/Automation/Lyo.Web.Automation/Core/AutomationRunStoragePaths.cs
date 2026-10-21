namespace Lyo.Web.Automation.Core;

/// <summary>
/// File-storage layout for one automation run. Screenshots and extractions share it. A run file uses <see cref="Run" />. A step file, including the viewport PNG, uses
/// <see cref="Step" />.
/// </summary>
public static class AutomationRunStoragePaths
{
    /// <summary>Prefix for files that belong to the run and not to one step. <c>Runs/{runId}</c>.</summary>
    public static string Run(Guid runId) => $"Runs/{runId:D}";

    /// <summary>Prefix for files that belong to one step execution. <c>Runs/{runId}/steps/{stepId}</c>. <paramref name="stepId" /> is the step execution id.</summary>
    public static string Step(Guid runId, Guid stepId) => $"{Run(runId)}/steps/{stepId:D}";

    /// <summary>Viewport PNG name for a step execution. <c>{stepId}.png</c>.</summary>
    public static string ScreenshotFileName(Guid stepId) => $"{stepId:D}.png";
}