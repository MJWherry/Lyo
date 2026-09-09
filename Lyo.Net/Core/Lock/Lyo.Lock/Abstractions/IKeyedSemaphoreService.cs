namespace Lyo.Lock.Abstractions;

/// <summary>Caps how many operations may run at once per logical key (counting semaphore per key). Local implementations scope concurrency to one process.</summary>
/// <remarks>
/// For a given normalized key, the <c>maxConcurrency</c> argument must stay the same while any permit or waiter is active; otherwise implementations throw to avoid
/// undefined behavior.
/// </remarks>
public interface IKeyedSemaphoreService
{
    /// <summary>Tries to take one permit from the pool for <paramref name="key" />.</summary>
    /// <param name="key">Logical resource identity (for example <c>report:export</c>).</param>
    /// <param name="maxConcurrency">Maximum permits available at once for this key.</param>
    /// <param name="timeout">How long to wait for a permit; implementation default when null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A handle that releases the permit, or <see langword="null" /> on timeout.</returns>
    ValueTask<IPermitHandle?> AcquireAsync(string key, int maxConcurrency, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Takes a permit, runs <paramref name="action" />, then releases.</summary>
    /// <param name="key">Logical resource identity.</param>
    /// <param name="maxConcurrency">Maximum concurrent holders for this key.</param>
    /// <param name="action">Work to run while holding a permit.</param>
    /// <param name="timeout">How long to wait for a permit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="TimeoutException">A permit was not acquired within the effective timeout.</exception>
    Task ExecuteAsync(string key, int maxConcurrency, Func<CancellationToken, Task> action, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Takes a permit, runs <paramref name="action" />, returns its result, then releases.</summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="key">Logical resource identity.</param>
    /// <param name="maxConcurrency">Maximum concurrent holders for this key.</param>
    /// <param name="action">Work to run while holding a permit.</param>
    /// <param name="timeout">How long to wait for a permit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The value from <paramref name="action" />.</returns>
    /// <exception cref="TimeoutException">A permit was not acquired within the effective timeout.</exception>
    Task<T> ExecuteAsync<T>(string key, int maxConcurrency, Func<CancellationToken, Task<T>> action, TimeSpan? timeout = null, CancellationToken ct = default);
}