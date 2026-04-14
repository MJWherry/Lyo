using Lyo.Lock.Redis;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.Testing;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Lyo.Lock.Tests;

public class LockServiceMetricsTests
{
    private static LockOptions OptionsWithMetrics => new() { EnableMetrics = true };

    private static KeyedSemaphoreOptions SemaphoreOptionsWithMetrics => new() { EnableMetrics = true };

    private static RedisLockOptions RedisOptionsWithMetrics => new() { EnableMetrics = true };

    [Fact]
    public void LocalLockService_WithNullMetrics_DoesNotThrow()
    {
        var options = new LockOptions { EnableMetrics = false };
        var service = new LocalLockService(null, options);
        Assert.NotNull(service);
    }

    [Fact]
    public void LocalKeyedSemaphoreService_WithNullMetrics_DoesNotThrow()
    {
        var options = new KeyedSemaphoreOptions { EnableMetrics = false };
        var service = new LocalKeyedSemaphoreService(null, options);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task LocalLockService_AcquireAsync_RecordsAcquireSuccessAndDuration()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalLockService(null, OptionsWithMetrics, metrics);
        var key = "metrics-acquire-" + Guid.NewGuid().ToString("N");
        var handle = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
        Assert.True(metrics.AcquireSuccessCounters.Count > 0);
        var success = metrics.AcquireSuccessCounters.FirstOrDefault(c => HasKey(c.Tags, key));
        Assert.NotNull(success);
        Assert.True(metrics.AcquireTimings.Count > 0);
        Assert.Contains(metrics.AcquireTimings, t => HasKey(t.Tags, key));
    }

    [Fact]
    public async Task LocalLockService_AcquireAsync_WhenBlocked_RecordsAcquireFailure()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalLockService(null, OptionsWithMetrics, metrics);
        var key = "metrics-failure-" + Guid.NewGuid().ToString("N");
        var handle1 = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        var handle2 = await service.AcquireAsync(key, TimeSpan.FromMilliseconds(100), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        Assert.True(metrics.AcquireFailureCounters.Count > 0);
        var failure = metrics.AcquireFailureCounters.FirstOrDefault(c => HasKey(c.Tags, key));
        Assert.NotNull(failure);
    }

    [Fact]
    public async Task LocalLockService_Release_RecordsReleaseDuration()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalLockService(null, OptionsWithMetrics, metrics);
        var key = "metrics-release-" + Guid.NewGuid().ToString("N");
        var handle = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
        Assert.True(metrics.ReleaseTimings.Count > 0);
        Assert.Contains(metrics.ReleaseTimings, t => HasKey(t.Tags, key));
    }

    [Fact]
    public async Task LocalLockService_ExecuteWithLockAsync_RecordsExecuteDuration()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalLockService(null, OptionsWithMetrics, metrics);
        var key = "metrics-execute-" + Guid.NewGuid().ToString("N");
        await service.ExecuteWithLockAsync(key, _ => Task.CompletedTask, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(metrics.ExecuteTimings.Count > 0);
        Assert.Contains(metrics.ExecuteTimings, t => HasKey(t.Tags, key));
    }

    [Fact]
    public async Task LocalLockService_WithEnableMetricsFalse_DoesNotRecordMetrics()
    {
        var metrics = new LockTestMetrics();
        var options = new LockOptions { EnableMetrics = false };
        var service = new LocalLockService(null, options, metrics);
        var key = "metrics-disabled-" + Guid.NewGuid().ToString("N");
        await service.ExecuteWithLockAsync(key, _ => Task.CompletedTask, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(metrics.AcquireSuccessCounters);
        Assert.Empty(metrics.AcquireTimings);
        Assert.Empty(metrics.ExecuteTimings);
    }

    [Fact]
    public async Task LocalKeyedSemaphoreService_AcquireAsync_RecordsAcquireSuccessAndDuration()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalKeyedSemaphoreService(null, SemaphoreOptionsWithMetrics, metrics);
        var key = "semaphore-metrics-acquire-" + Guid.NewGuid().ToString("N");
        var handle = await service.AcquireAsync(key, 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
        Assert.True(metrics.SemaphoreAcquireSuccessCounters.Count > 0);
        Assert.Contains(metrics.SemaphoreAcquireSuccessCounters, c => HasSemaphoreKey(c.Tags, key));
        Assert.True(metrics.SemaphoreAcquireTimings.Count > 0);
        Assert.Contains(metrics.SemaphoreAcquireTimings, t => HasSemaphoreKey(t.Tags, key));
    }

    [Fact]
    public async Task LocalKeyedSemaphoreService_AcquireAsync_WhenBlocked_RecordsAcquireFailure()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalKeyedSemaphoreService(null, SemaphoreOptionsWithMetrics, metrics);
        var key = "semaphore-metrics-failure-" + Guid.NewGuid().ToString("N");
        var handle1 = await service.AcquireAsync(key, 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        var handle2 = await service.AcquireAsync(key, 1, TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        Assert.True(metrics.SemaphoreAcquireFailureCounters.Count > 0);
        Assert.Contains(metrics.SemaphoreAcquireFailureCounters, c => HasSemaphoreKey(c.Tags, key));
    }

    [Fact]
    public async Task LocalKeyedSemaphoreService_Release_RecordsReleaseDuration()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalKeyedSemaphoreService(null, SemaphoreOptionsWithMetrics, metrics);
        var key = "semaphore-metrics-release-" + Guid.NewGuid().ToString("N");
        var handle = await service.AcquireAsync(key, 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
        Assert.True(metrics.SemaphoreReleaseTimings.Count > 0);
        Assert.Contains(metrics.SemaphoreReleaseTimings, t => HasSemaphoreKey(t.Tags, key));
    }

    [Fact]
    public async Task LocalKeyedSemaphoreService_ExecuteAsync_RecordsExecuteDuration()
    {
        var metrics = new LockTestMetrics();
        var service = new LocalKeyedSemaphoreService(null, SemaphoreOptionsWithMetrics, metrics);
        var key = "semaphore-metrics-execute-" + Guid.NewGuid().ToString("N");
        await service.ExecuteAsync(key, 2, _ => Task.CompletedTask, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(metrics.SemaphoreExecuteTimings.Count > 0);
        Assert.Contains(metrics.SemaphoreExecuteTimings, t => HasSemaphoreKey(t.Tags, key));
    }

    [Fact]
    public async Task LocalKeyedSemaphoreService_WithEnableMetricsFalse_DoesNotRecordMetrics()
    {
        var metrics = new LockTestMetrics();
        var options = new KeyedSemaphoreOptions { EnableMetrics = false };
        var service = new LocalKeyedSemaphoreService(null, options, metrics);
        var key = "semaphore-metrics-disabled-" + Guid.NewGuid().ToString("N");
        await service.ExecuteAsync(key, 2, _ => Task.CompletedTask, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(metrics.SemaphoreAcquireSuccessCounters);
        Assert.Empty(metrics.SemaphoreAcquireTimings);
        Assert.Empty(metrics.SemaphoreExecuteTimings);
    }

    [Fact]
    public async Task RedisLockService_AcquireAsync_RecordsAcquireSuccessAndDuration()
    {
        var metrics = new LockTestMetrics();
        var redis = await CreateRedisConnectionAsync().ConfigureAwait(false);
        try {
            var service = new RedisLockService(redis, null, RedisOptionsWithMetrics, metrics);
            var key = "redis-metrics-acquire-" + Guid.NewGuid().ToString("N");
            var handle = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            handle.ShouldNotBeNull();
            await handle.ReleaseAsync().ConfigureAwait(false);
            Assert.True(metrics.AcquireSuccessCounters.Count > 0);
            Assert.Contains(metrics.AcquireSuccessCounters, c => HasKey(c.Tags, key));
            Assert.True(metrics.AcquireTimings.Count > 0);
            Assert.Contains(metrics.AcquireTimings, t => HasKey(t.Tags, key));
        }
        finally {
            redis.Dispose();
        }
    }

    [Fact]
    public async Task RedisLockService_ExecuteWithLockAsync_RecordsExecuteDuration()
    {
        var metrics = new LockTestMetrics();
        var redis = await CreateRedisConnectionAsync().ConfigureAwait(false);
        try {
            var service = new RedisLockService(redis, null, RedisOptionsWithMetrics, metrics);
            var key = "redis-metrics-execute-" + Guid.NewGuid().ToString("N");
            await service.ExecuteWithLockAsync(key, _ => Task.CompletedTask, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(metrics.ExecuteTimings.Count > 0);
            Assert.Contains(metrics.ExecuteTimings, t => HasKey(t.Tags, key));
        }
        finally {
            redis.Dispose();
        }
    }

    private static bool HasKey(Dictionary<string, string>? tags, string key)
        => tags != null && tags.TryGetValue(Constants.Metrics.Tags.Key, out var v) && string.Equals(v, key, StringComparison.OrdinalIgnoreCase);

    private static bool HasSemaphoreKey(Dictionary<string, string>? tags, string key)
        => tags != null && tags.TryGetValue(Constants.SemaphoreMetrics.Tags.Key, out var v) && string.Equals(v, key, StringComparison.OrdinalIgnoreCase);

    private static async Task<IConnectionMultiplexer> CreateRedisConnectionAsync()
    {
        var container = new RedisBuilder("redis:7-alpine").Build();
        await container.StartAsync().ConfigureAwait(false);
        try {
            return await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString()).ConfigureAwait(false);
        }
        catch {
            await container.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private class LockTestMetrics : IMetrics
    {
        public List<(string Name, long Value, Dictionary<string, string>? Tags)> AcquireSuccessCounters { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> AcquireFailureCounters { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> SemaphoreAcquireSuccessCounters { get; } = new();

        public List<(string Name, long Value, Dictionary<string, string>? Tags)> SemaphoreAcquireFailureCounters { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> AcquireTimings { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> ReleaseTimings { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> ExecuteTimings { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> SemaphoreAcquireTimings { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> SemaphoreReleaseTimings { get; } = new();

        public List<(string Name, TimeSpan Duration, Dictionary<string, string>? Tags)> SemaphoreExecuteTimings { get; } = new();

        public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
        {
            var counterValue = value != null ? Convert.ToInt64(value) : 1L;
            var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
            if (name == Constants.Metrics.AcquireSuccess)
                AcquireSuccessCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.Metrics.AcquireFailure)
                AcquireFailureCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.SemaphoreMetrics.AcquireSuccess)
                SemaphoreAcquireSuccessCounters.Add((name, counterValue, dictTags));
            else if (name == Constants.SemaphoreMetrics.AcquireFailure)
                SemaphoreAcquireFailureCounters.Add((name, counterValue, dictTags));
        }

        public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }

        public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

        public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

        public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null)
        {
            var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
            if (name == Constants.Metrics.AcquireDuration)
                AcquireTimings.Add((name, duration, dictTags));
            else if (name == Constants.Metrics.ReleaseDuration)
                ReleaseTimings.Add((name, duration, dictTags));
            else if (name == Constants.Metrics.ExecuteDuration)
                ExecuteTimings.Add((name, duration, dictTags));
            else if (name == Constants.SemaphoreMetrics.AcquireDuration)
                SemaphoreAcquireTimings.Add((name, duration, dictTags));
            else if (name == Constants.SemaphoreMetrics.ReleaseDuration)
                SemaphoreReleaseTimings.Add((name, duration, dictTags));
            else if (name == Constants.SemaphoreMetrics.ExecuteDuration)
                SemaphoreExecuteTimings.Add((name, duration, dictTags));
        }

        public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => new(new(this, name, tags));

        public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null) { }

        public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }
    }
}