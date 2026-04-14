using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Lyo.Lock.Redis;

internal sealed class RedisLockHandle : ILockHandle
{
    private const string ReleaseScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    private readonly IDatabaseAsync _db;
    private readonly string _key;
    private readonly string _keyForMetrics;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly RedisChannel _notifyChannel;
    private readonly ISubscriber _subscriber;
    private readonly string _token;
    private int _released;

    public RedisLockHandle(IDatabaseAsync db, ISubscriber subscriber, string key, RedisChannel notifyChannel, string token, ILogger logger, IMetrics metrics, string keyForMetrics)
    {
        _db = db;
        _subscriber = subscriber;
        _notifyChannel = notifyChannel;
        _key = key;
        _token = token;
        _logger = logger;
        _metrics = metrics;
        _keyForMetrics = keyForMetrics;
    }

    public async ValueTask ReleaseAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return;

        using (_metrics.StartTimer(Constants.Metrics.ReleaseDuration, [(Constants.Metrics.Tags.Key, _keyForMetrics)])) {
            try {
                var result = await _db.ScriptEvaluateAsync(ReleaseScript, [_key], [_token]).ConfigureAwait(false);
                if (result.IsNull)
                    _logger.LogWarning("Lock for key {LockKey} may have already expired or been released", _key);
                else if (!result.IsNull && (int)result == 1)
                    await _subscriber.PublishAsync(_notifyChannel, RedisValue.EmptyString).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error releasing distributed lock for key {LockKey}", _key);
                throw;
            }
        }
    }

    public void Dispose() => ReleaseAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
}