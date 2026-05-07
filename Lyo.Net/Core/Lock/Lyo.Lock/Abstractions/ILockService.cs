namespace Lyo.Lock.Abstractions;

/// <summary>Acquires exclusive locks by string key. Implementations may be in-process only or distributed (for example Redis-backed).</summary>
/// <remarks>
/// For manual acquire/release, prefer <c>await using</c> on the returned handle (when non-null) so the lock is released on all paths. The lock-duration argument on
/// <see cref="AcquireAsync" /> is honored by distributed implementations to bound how long a crashed holder can block the key; local locks ignore it.
/// </remarks>
public interface ILockService
{
    /// <summary>Attempts to acquire an exclusive lock for <paramref name="key" />.</summary>
    /// <param name="key">Logical lock identity (e.g. <c>order:123</c>). May be normalized per implementation/options.</param>
    /// <param name="timeout">Maximum time to wait for the lock; implementation default if null.</param>
    /// <param name="lockDuration">Distributed locks: Redis key TTL / safety expiry; ignored for purely local locks.</param>
    /// <param name="ct">Cancellation token; waiting operations respect cancellation.</param>
    /// <returns>A handle that releases the lock, or <see langword="null" /> if the lock was not acquired before <paramref name="timeout" />.</returns>
    ValueTask<ILockHandle?> AcquireAsync(string key, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default);

    /// <summary>Acquires the lock, runs <paramref name="action" />, then releases. Fails fast with <see cref="TimeoutException" /> if not acquired.</summary>
    /// <param name="key">Logical lock identity.</param>
    /// <param name="action">Work to run while holding the lock; receives <paramref name="ct" />.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="lockDuration">Distributed locks: TTL while held.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="TimeoutException">The lock could not be acquired within <paramref name="timeout" /> (or the configured default).</exception>
    Task ExecuteWithLockAsync(string key, Func<CancellationToken, Task> action, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default);

    /// <summary>Acquires the lock, runs <paramref name="action" />, returns its result, then releases.</summary>
    /// <typeparam name="T">Result type of <paramref name="action" />.</typeparam>
    /// <param name="key">Logical lock identity.</param>
    /// <param name="action">Work to run while holding the lock.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="lockDuration">Distributed locks: TTL while held.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The value returned by <paramref name="action" />.</returns>
    /// <exception cref="TimeoutException">The lock could not be acquired within the effective timeout.</exception>
    Task<T> ExecuteWithLockAsync<T>(string key, Func<CancellationToken, Task<T>> action, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default);
}