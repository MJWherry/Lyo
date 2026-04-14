namespace Lyo.Lock;

/// <summary>Handle for an acquired lock. Dispose or DisposeAsync to release.</summary>
public interface ILockHandle : IAsyncDisposable, IDisposable
{
    /// <summary>Releases the lock asynchronously.</summary>
    ValueTask ReleaseAsync();
}