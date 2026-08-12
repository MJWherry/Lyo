namespace Lyo.ContentThreatScan.Intel;

/// <summary>How HTTP/TCP outages are translated into disposition score.</summary>
public enum ExternalReputationFailureDisposition
{
    /// <summary>Ignore failure (additive score 0, log).</summary>
    Ignore = 0,

    /// <summary>Add <see cref="ReputationPipelineOptions.ProviderFailureSuspectBump" /> points.</summary>
    TreatAsSuspect = 1,

    /// <summary>Add a large contribution capped by disposition ceiling (forces threat).</summary>
    ImmediateThreatBump = 2
}