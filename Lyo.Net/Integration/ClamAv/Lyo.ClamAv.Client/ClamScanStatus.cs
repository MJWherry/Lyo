namespace Lyo.ClamAv.Client;

/// <summary>Outcome of one clamav-rest scan.</summary>
public enum ClamScanStatus
{
    /// <summary>The engine reported the content clean.</summary>
    Clean = 0,

    /// <summary>The engine reported a signature.</summary>
    Infected = 1,

    /// <summary>The engine answered with a status other than clean or infected.</summary>
    Error = 2
}
