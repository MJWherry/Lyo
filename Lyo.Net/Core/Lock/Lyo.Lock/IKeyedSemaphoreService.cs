namespace Lyo.Lock;

/// <summary>Coordinates bounded concurrency by key using semaphore semantics.</summary>
public interface IKeyedSemaphoreService
{
    /// <summary>Acquires a permit for the given key. Returns a handle that releases the permit when disposed, or null if acquisition failed within the timeout.</summary>
    /// <param name="key">The semaphore key (e.g. "order:123", "user:456:profile")</param>
    /// <param name="maxConcurrency">Maximum number of concurrent permit holders for the key</param>
    /// <param name="timeout">Maximum time to wait for a permit. Default is 30 seconds.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Permit handle, or null if a permit could not be acquired within the timeout</returns>
    ValueTask<IPermitHandle?> AcquireAsync(string key, int maxConcurrency, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Executes an action while holding a permit. Throws if a permit cannot be acquired.</summary>
    Task ExecuteAsync(string key, int maxConcurrency, Func<CancellationToken, Task> action, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Executes a function while holding a permit. Throws if a permit cannot be acquired.</summary>
    Task<T> ExecuteAsync<T>(string key, int maxConcurrency, Func<CancellationToken, Task<T>> action, TimeSpan? timeout = null, CancellationToken ct = default);
}