using Lyo.Exceptions;

namespace Lyo.Web.Automation.Api;

/// <summary>Limits for in-memory automation runs. Plans are not stored. Artifacts live under <see cref="ArtifactRoot" /> until <see cref="RunTtl" /> elapses.</summary>
public sealed class AutomationApiOptions
{
    /// <summary>Configuration section. <c>WebAutomation:Api</c>.</summary>
    public const string SectionName = "WebAutomation:Api";

    /// <summary>How many queued or running plans the process accepts. A further <c>POST</c> returns 429. Starts at 1.</summary>
    public int MaxConcurrentRuns { get; set; } = 1;

    /// <summary>How long a finished run's log and snapshots stay readable. Starts at 30 minutes.</summary>
    public TimeSpan RunTtl { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Directory that receives a copy of <c>logs/</c> and <c>snapshots/</c> before the browser session is disposed.</summary>
    public string ArtifactRoot { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-automation-api");

    /// <summary>Rejects a non-positive cap, a non-positive TTL, or a blank artifact root.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIf(MaxConcurrentRuns < 1, "MaxConcurrentRuns must be at least 1.", nameof(MaxConcurrentRuns));
        ArgumentHelpers.ThrowIf(RunTtl <= TimeSpan.Zero, "RunTtl must be positive.", nameof(RunTtl));
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(ArtifactRoot), "ArtifactRoot is required.", nameof(ArtifactRoot));
    }
}