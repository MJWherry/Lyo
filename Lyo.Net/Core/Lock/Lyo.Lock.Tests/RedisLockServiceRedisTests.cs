using Lyo.Lock.Abstractions;
using Lyo.Lock.Redis;
using Lyo.Testing;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Lock.Tests;

public class RedisLockServiceRedisTests : RedisContainerFixtureBase
{
    private ILockService? _lockService;
    private IServiceProvider? _serviceProvider;

    protected override ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRedisLock(connectionString, options => options.KeyPrefix = "lyo:lock:test:");
        _serviceProvider = services.BuildServiceProvider();
        _lockService = _serviceProvider.GetRequiredService<ILockService>();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AcquireAsync_WithRedis_ReturnsHandle()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-acquire-1";
        var handle = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenHeldByOther_ReturnsNullAfterTimeout()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-blocked-2";
        var handle1 = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle1.ShouldNotBeNull();
        var handle2 = await _lockService.AcquireAsync(key, TimeSpan.FromMilliseconds(200), ct: TestContext.Current.CancellationToken);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync();
    }

    [Fact]
    public async Task ExecuteWithLockAsync_WithRedis_RunsAction()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-execute-3";
        var executed = false;
        await _lockService.ExecuteWithLockAsync(
            key, async ct => {
                await Task.Delay(10, ct);
                executed = true;
            }, ct: TestContext.Current.CancellationToken);

        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteWithLockAsync_WithRedis_ReturnsValue()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-return-4";
        var result = await _lockService.ExecuteWithLockAsync(key, _ => Task.FromResult(123), ct: TestContext.Current.CancellationToken);
        result.ShouldBe(123);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_WhenLockHeld_ThrowsTimeoutException()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-timeout-5";
        var handle = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle.ShouldNotBeNull();
        await Assert.ThrowsAsync<TimeoutException>(async () => await _lockService.ExecuteWithLockAsync(
            key, _ => Task.CompletedTask, TimeSpan.FromMilliseconds(100), ct: TestContext.Current.CancellationToken));

        await handle.ReleaseAsync();
    }

    [Fact]
    public async Task Release_IsIdempotent()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-idempotent-6";
        var handle = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync();
        await handle.ReleaseAsync();
        handle.Dispose();
    }

    [Fact]
    public async Task DifferentKeys_CanBeLockedConcurrently()
    {
        Assert.NotNull(_lockService);
        var keyA = "redis-test-key-a-7";
        var keyB = "redis-test-key-b-7";
        var handle1 = await _lockService.AcquireAsync(keyA, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        var handle2 = await _lockService.AcquireAsync(keyB, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle1.ShouldNotBeNull();
        handle2.ShouldNotBeNull();
        await handle1.ReleaseAsync();
        await handle2.ReleaseAsync();
    }

    [Fact]
    public async Task ExecuteWithLockAsync_ReleasesLockOnException()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-exception-8";
        await Assert.ThrowsAsync<InvalidOperationException>(async ()
            => await _lockService.ExecuteWithLockAsync(key, _ => throw new InvalidOperationException("test"), ct: TestContext.Current.CancellationToken));

        var handle = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync();
    }

    [Fact]
    public async Task Lock_ExpiresAfterDuration()
    {
        Assert.NotNull(_lockService);
        var key = "redis-test-expiry-9";
        var handle = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync();
        await Task.Delay(150, TestContext.Current.CancellationToken);
        var handle2 = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle2.ShouldNotBeNull();
        await handle2.ReleaseAsync();
    }
}