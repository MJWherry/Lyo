using Lyo.Exceptions;

namespace Lyo.ContentThreatScan.ClamAv;

/// <summary>Score and failure policy for <see cref="ClamAvContentThreatScan" />. Bind from section <c>ClamAvContentThreatScan</c>.</summary>
public sealed class ClamAvContentThreatScanOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ClamAvContentThreatScan";

    /// <summary>Points added when the engine reports a signature. Default 118.</summary>
    public decimal DetectionPoints { get; set; } = 118m;

    /// <summary>When true, an infected verdict sets <c>IntelConfirmedMalicious</c>. Default true.</summary>
    public bool MarksIntelConfirmed { get; set; } = true;

    /// <summary>How engine errors and HTTP failures are scored. Default <see cref="ClamAvScanFailureDisposition.Ignore" />.</summary>
    public ClamAvScanFailureDisposition FailureDisposition { get; set; } = ClamAvScanFailureDisposition.Ignore;

    /// <summary>Points used when <see cref="FailureDisposition" /> is <see cref="ClamAvScanFailureDisposition.TreatAsSuspect" />. Default 16.</summary>
    public decimal SuspectBump { get; set; } = 16m;

    /// <summary>Points used when <see cref="FailureDisposition" /> is <see cref="ClamAvScanFailureDisposition.ImmediateThreatBump" />. Default 132.</summary>
    public decimal ThreatBump { get; set; } = 132m;

    /// <summary>Checks point values. Called by <c>AddClamAvContentThreatScan</c>.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIf(DetectionPoints < 0m, "DetectionPoints must be >= 0.");
        ArgumentHelpers.ThrowIf(SuspectBump < 0m, "SuspectBump must be >= 0.");
        ArgumentHelpers.ThrowIf(ThreatBump < 0m, "ThreatBump must be >= 0.");
    }
}
