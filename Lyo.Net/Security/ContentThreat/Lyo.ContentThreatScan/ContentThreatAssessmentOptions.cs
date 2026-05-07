namespace Lyo.ContentThreatScan;

/// <summary>Assessment-level options shared by heuristic/external composition.</summary>
public sealed class ContentThreatAssessmentOptions
{
    /// <summary>Applied to heuristic+external summed points before thresholds. Less than zero disables capping.</summary>
    public decimal DispositionScoreCap { get; set; } = 200m;

    /// <summary>Scores at or above this imply suspect band when confirming intel mapping is inactive.</summary>
    public decimal SuspectThreshold { get; set; } = 25m;

    /// <summary>Scores at or above this imply threat band when not overridden by confirmation rules.</summary>
    public decimal ThreatThreshold { get; set; } = 70m;

    /// <summary>When intel marks a sample as knowingly malicious maps into threat irrespective of summed score unless disabled.</summary>
    public bool ForceThreatOnConfirmedIntel { get; set; } = true;

    /// <summary>Contribution points added when a reputation/clam contributor fails (HTTP timeout, quota, disconnect).</summary>
    public decimal FailureBumpPoints { get; set; }

    /// <summary>Contribution rule id appended on failure bumps (auditable).</summary>
    public string FailureContributionRuleId { get; set; } = "external.failure";
}