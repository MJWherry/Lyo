namespace Lyo.Lock;

/// <summary>Handle for an acquired semaphore permit. Dispose or DisposeAsync to release.</summary>
public interface IPermitHandle : IAsyncDisposable, IDisposable
{
    /// <summary>Releases the permit asynchronously.</summary>
    ValueTask ReleaseAsync();
}