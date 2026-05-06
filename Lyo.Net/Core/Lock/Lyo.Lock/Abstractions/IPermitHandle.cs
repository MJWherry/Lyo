namespace Lyo.Lock.Abstractions;

/// <summary>
/// Represents one acquired permit from <see cref="IKeyedSemaphoreService.AcquireAsync"/>; releasing frees a concurrency slot for that key.
/// </summary>
/// <remarks>
/// Call <see cref="ReleaseAsync"/> once when finished; further calls are ignored by implementations.
/// </remarks>
public interface IPermitHandle : IAsyncDisposable, IDisposable
{
    /// <summary>Releases the permit. Idempotent after the first successful release.</summary>
    ValueTask ReleaseAsync();
}