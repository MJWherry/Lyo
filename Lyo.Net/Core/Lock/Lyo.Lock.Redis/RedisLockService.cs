using Lyo.Exceptions;
using Lyo.Lock;
using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace Lyo.Lock.Redis;

/// <summary>
/// Distributed exclusive lock via Redis: acquire uses <c>SET key token NX PX ttl</c>; release uses a Lua script so only the token owner deletes the key.
/// Optionally waits using pub/sub on a companion notify channel to reduce polling latency.
/// </summary>
/// <remarks>
/// Register through <see cref="RedisLockServiceExtensions"/> with an existing <see cref="IConnectionMultiplexer"/> or a connection string.
/// Choose <see cref="RedisLockOptions.DefaultLockDuration"/> large enough for typical critical sections; if work runs longer than TTL, another instance may acquire—design accordingly.
/// </remarks>
public sealed class RedisLockService : ILockService
{
    private const string NotifyChannelPrefix = "lock:notify:";
    private readonly IDatabaseAsync _db;

    private readonly ILogger<RedisLockService> _logger;
    private readonly IMetrics _metrics;
    private readonly RedisLockOptions _options;
    private readonly ISubscriber _subscriber;

    /// <param name="redis">Shared multiplexer (same as caching stack when applicable).</param>
    /// <param name="logger">Optional diagnostics.</param>
    /// <param name="options">Timeouts, key prefix, pub/sub vs poll, TTL defaults.</param>
    /// <param name="metrics">Emitted when <see cref="LockOptions.EnableMetrics"/> is true.</param>
    public RedisLockService(IConnectionMultiplexer redis, ILogger<RedisLockService>? logger = null, RedisLockOptions? options = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(redis);
        _logger = logger ?? NullLogger<RedisLockService>.Instance;
        _options = options ?? new RedisLockOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _db = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
    }

    /// <inheritdoc />
    public async ValueTask<ILockHandle?> AcquireAsync(string key, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var normalizedKey = _options.SkipKeyNormalization ? key : key.ToLowerInvariant();
        var redisKey = _options.KeyPrefix + normalizedKey;
        var token = Guid.NewGuid().ToString("N");
        var effectiveDuration = (lockDuration ?? _options.DefaultLockDuration).TotalMilliseconds;
        var effectiveTimeout = timeout ?? _options.DefaultAcquireTimeout;
        var deadline = DateTime.UtcNow + effectiveTimeout;
        var tags = new[] { (Constants.Metrics.Tags.Key, key) };
        using (_metrics.StartTimer(Constants.Metrics.AcquireDuration, tags)) {
            while (DateTime.UtcNow < deadline) {
                ct.ThrowIfCancellationRequested();
                var acquired = await _db.StringSetAsync(redisKey, token, TimeSpan.FromMilliseconds(effectiveDuration), When.NotExists).ConfigureAwait(false);
                if (acquired) {
                    _metrics.IncrementCounter(Constants.Metrics.AcquireSuccess, 1, tags);
                    return new RedisLockHandle(_db, _subscriber, redisKey, GetNotifyChannel(normalizedKey), token, _logger, _metrics, key);
                }

                if (_options.UsePubSubForAcquireWait) {
                    var notifyChannel = GetNotifyChannel(normalizedKey);
                    var tcsRef = new object[] { new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously) };
                    Action<RedisChannel, RedisValue> handler = (_, _) => ((TaskCompletionSource<bool>)tcsRef[0]).TrySetResult(true);
                    await _subscriber.SubscribeAsync(notifyChannel, handler).ConfigureAwait(false);
                    try {
                        while (DateTime.UtcNow < deadline) {
                            ct.ThrowIfCancellationRequested();
                            var remaining = deadline - DateTime.UtcNow;
                            if (remaining <= TimeSpan.Zero)
                                break;

                            var waitTask = ((TaskCompletionSource<bool>)tcsRef[0]).Task;
                            var delayTask = Task.Delay(remaining, ct);
                            var completed = await Task.WhenAny(waitTask, delayTask).ConfigureAwait(false);
                            if (completed == delayTask)
                                break;

                            tcsRef[0] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                            acquired = await _db.StringSetAsync(redisKey, token, TimeSpan.FromMilliseconds(effectiveDuration), When.NotExists).ConfigureAwait(false);
                            if (acquired) {
                                _metrics.IncrementCounter(Constants.Metrics.AcquireSuccess, 1, tags);
                                return new RedisLockHandle(_db, _subscriber, redisKey, notifyChannel, token, _logger, _metrics, key);
                            }
                        }
                    }
                    finally {
                        await _subscriber.UnsubscribeAsync(notifyChannel, handler).ConfigureAwait(false);
                    }
                }
                else
                    await Task.Delay(_options.AcquirePollInterval, ct).ConfigureAwait(false);
            }
        }

        _metrics.IncrementCounter(Constants.Metrics.AcquireFailure, 1, tags);
        _logger.LogDebug("Failed to acquire distributed lock for key {LockKey} within {Timeout}", key, effectiveTimeout);
        return null;
    }

    /// <inheritdoc />
    public async Task ExecuteWithLockAsync(
        string key,
        Func<CancellationToken, Task> action,
        TimeSpan? timeout = null,
        TimeSpan? lockDuration = null,
        CancellationToken ct = default)
    {
        using (_metrics.StartTimer(Constants.Metrics.ExecuteDuration, [(Constants.Metrics.Tags.Key, key)])) {
            var handle = await AcquireAsync(key, timeout, lockDuration, ct).ConfigureAwait(false);
            if (handle == null)
                throw new TimeoutException($"Could not acquire distributed lock for key '{key}' within {timeout ?? _options.DefaultAcquireTimeout}.");

            try {
                await action(ct).ConfigureAwait(false);
            }
            finally {
                await handle.ReleaseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithLockAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? timeout = null,
        TimeSpan? lockDuration = null,
        CancellationToken ct = default)
    {
        using (_metrics.StartTimer(Constants.Metrics.ExecuteDuration, [(Constants.Metrics.Tags.Key, key)])) {
            var handle = await AcquireAsync(key, timeout, lockDuration, ct).ConfigureAwait(false);
            if (handle == null)
                throw new TimeoutException($"Could not acquire distributed lock for key '{key}' within {timeout ?? _options.DefaultAcquireTimeout}.");

            try {
                return await action(ct).ConfigureAwait(false);
            }
            finally {
                await handle.ReleaseAsync().ConfigureAwait(false);
            }
        }
    }

    private RedisChannel GetNotifyChannel(string normalizedKey) => RedisChannel.Literal(_options.KeyPrefix + NotifyChannelPrefix + normalizedKey);
}