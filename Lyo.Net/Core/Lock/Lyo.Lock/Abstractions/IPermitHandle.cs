namespace Lyo.Lock.Abstractions;

/// <summary>One acquired permit from <see cref="IKeyedSemaphoreService.AcquireAsync" />; releasing frees a concurrency slot for that key.</summary>
/// <remarks>Call <see cref="ReleaseAsync" /> once when finished; later calls are ignored by implementations.</remarks>
public interface IPermitHandle : IAsyncDisposable, IDisposable
{
    /// <summary>Releases the permit. Safe to call again after the first successful release.</summary>
    ValueTask ReleaseAsync();
}