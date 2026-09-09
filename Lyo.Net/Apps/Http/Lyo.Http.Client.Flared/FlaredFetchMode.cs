namespace Lyo.Http.Client.Flared;

/// <summary>How Flared talks to FlareSolverr.</summary>
public enum FlaredFetchMode
{
    /// <summary>Always POST /v1 and return a synthetic response from <c>solution.response</c> (default; avoids .NET TLS replay mismatches).</summary>
    ThroughSolver = 0,

    /// <summary>Use FlareSolverrSharp <c>ClearanceHandler</c> (direct request, solve on challenge, replay with cookies). Often fails TLS fingerprint checks.</summary>
    ReplayWithClearanceHandler = 1
}
