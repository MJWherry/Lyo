namespace Lyo.Lock.Abstractions;

/// <summary>
/// Represents one successful acquisition from <see cref="ILockService.AcquireAsync"/>; releasing returns the lock (or permit slot) to waiters.
/// </summary>
/// <remarks>
/// Call <see cref="ReleaseAsync"/> once when finished; further calls are ignored by implementations.
/// <see cref="IDisposable.Dispose"/> / <see cref="IAsyncDisposable.DisposeAsync"/> delegate to release (dispose may block briefly on synchronous paths).
/// </remarks>
public interface ILockHandle : IAsyncDisposable, IDisposable
{
    /// <summary>Releases the lock. Idempotent after the first successful release.</summary>
    ValueTask ReleaseAsync();
}