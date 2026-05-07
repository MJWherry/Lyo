namespace Lyo.Web.Automation.Abstractions;

/// <summary>Uploads local files produced during a plan run to a backing storage service.</summary>
public interface IAutomationPlanFileStorage
{
    /// <summary>Uploads all files under a directory and returns stored locations/keys.</summary>
    Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct);
}