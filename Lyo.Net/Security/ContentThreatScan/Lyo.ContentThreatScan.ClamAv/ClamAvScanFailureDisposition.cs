namespace Lyo.ContentThreatScan.ClamAv;

/// <summary>How a clamav-rest failure becomes a reputation contribution.</summary>
public enum ClamAvScanFailureDisposition
{
    /// <summary>Leave the envelope empty.</summary>
    Ignore = 0,

    /// <summary>Add <see cref="ClamAvContentThreatScanOptions.SuspectBump" /> reputation points.</summary>
    TreatAsSuspect = 1,

    /// <summary>Add <see cref="ClamAvContentThreatScanOptions.ThreatBump" /> reputation points.</summary>
    ImmediateThreatBump = 2
}
