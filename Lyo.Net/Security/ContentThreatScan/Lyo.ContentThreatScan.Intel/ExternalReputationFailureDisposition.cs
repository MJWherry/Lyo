namespace Lyo.ContentThreatScan.Intel;

/// <summary>How HTTP/TCP outages become disposition score.</summary>
public enum ExternalReputationFailureDisposition
{
    /// <summary>Ignore the failure (additive score 0, still log).</summary>
    Ignore = 0,

    /// <summary>Add <see cref="ReputationPipelineOptions.ProviderFailureSuspectBump" /> points.</summary>
    TreatAsSuspect = 1,

    /// <summary>Add a large contribution, capped by the disposition ceiling (forces threat).</summary>
    ImmediateThreatBump = 2
}