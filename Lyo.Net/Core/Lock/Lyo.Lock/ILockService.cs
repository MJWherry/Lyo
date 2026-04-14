namespace Lyo.Lock;

/// <summary>Centralized locking service for acquiring and releasing locks by key.</summary>
public interface ILockService
{
    /// <summary>Acquires a lock for the given key. Returns a handle that releases the lock when disposed, or null if acquisition failed within the timeout.</summary>
    /// <param name="key">The lock key (e.g. "order:123", "user:456:profile")</param>
    /// <param name="timeout">Maximum time to wait for the lock. Default is 30 seconds.</param>
    /// <param name="lockDuration">For distributed locks: auto-release duration if the process crashes. Default is 60 seconds.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Lock handle, or null if the lock could not be acquired within the timeout</returns>
    ValueTask<ILockHandle?> AcquireAsync(string key, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default);

    /// <summary>Executes an action while holding the lock. Throws if the lock cannot be acquired.</summary>
    /// <param name="key">The lock key</param>
    /// <param name="action">The action to execute while holding the lock</param>
    /// <param name="timeout">Maximum time to wait for the lock</param>
    /// <param name="lockDuration">Auto-release duration for distributed locks</param>
    /// <param name="ct">Cancellation token</param>
    Task ExecuteWithLockAsync(string key, Func<CancellationToken, Task> action, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default);

    /// <summary>Executes a function while holding the lock. Throws if the lock cannot be acquired.</summary>
    Task<T> ExecuteWithLockAsync<T>(string key, Func<CancellationToken, Task<T>> action, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default);
}