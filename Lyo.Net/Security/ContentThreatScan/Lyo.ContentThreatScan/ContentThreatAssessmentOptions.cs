namespace Lyo.ContentThreatScan;

/// <summary>Assessment knobs shared when heuristic and external scores are composed.</summary>
public sealed class ContentThreatAssessmentOptions
{
    /// <summary>Applied to heuristic+external summed points before thresholds. A value below zero turns the cap off.</summary>
    public decimal DispositionScoreCap { get; set; } = 200m;

    /// <summary>At or above this score, treat as suspect when confirming intel mapping is off.</summary>
    public decimal SuspectThreshold { get; set; } = 25m;

    /// <summary>At or above this score, treat as threat unless confirmation rules override.</summary>
    public decimal ThreatThreshold { get; set; } = 70m;

    /// <summary>When intel flags a sample as known-malicious, map to threat regardless of the summed score unless that path is disabled.</summary>
    public bool ForceThreatOnConfirmedIntel { get; set; } = true;

    /// <summary>Points added when a reputation/clam contributor fails (HTTP timeout, quota, disconnect).</summary>
    public decimal FailureBumpPoints { get; set; }

    /// <summary>Rule id recorded on those failure bumps (for audit).</summary>
    public string FailureContributionRuleId { get; set; } = "external.failure";
}