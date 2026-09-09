using Lyo.Http.Client.Session;

namespace Lyo.Http.Client.Flared;

/// <summary><see cref="ILyoHttpSession" /> plus the FlareSolverr session id.</summary>
public interface IFlareSession : ILyoHttpSession
{
    /// <summary>Id returned by <c>sessions.create</c>, when <see cref="FlaredHttpOptions.UseSolverSessions" /> is on.</summary>
    string? FlareSolverrSessionId { get; set; }
}
