using Lyo.Lock.Abstractions;
using Lyo.Lock.Redis;
using Lyo.Testing;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Lyo.Lock.Tests;

/// <summary>Runs core lock behavior tests against both LocalLockService and RedisLockService.</summary>
public class LockServiceBothImplementationsTests : IAsyncLifetime
{
    private readonly ILogger<LocalLockService> _localLogger;
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();
    private readonly ILogger<RedisLockService> _redisLogger;
    private IConnectionMultiplexer? _redis;

    public static IEnumerable<object[]> LockImplementations => [["Local"], ["Redis"]];

    public LockServiceBothImplementationsTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _localLogger = loggerFactory.CreateLogger<LocalLockService>();
        _redisLogger = loggerFactory.CreateLogger<RedisLockService>();
    }

    public async ValueTask InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        _redis?.Dispose();
        await _redisContainer.DisposeAsync();
    }

    private ILockService CreateLockService(string implementation)
        => implementation switch {
            "Local" => new LocalLockService(_localLogger, new()),
            "Redis" => new RedisLockService(_redis!, _redisLogger, new()),
            var _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task AcquireAsync_ReturnsHandle(string implementation)
    {
        var service = CreateLockService(implementation);
        var key = $"both-acquire-{implementation}-{Guid.NewGuid():N}";
        var handle = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync();
    }

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task AcquireAsync_AfterRelease_CanAcquireAgain(string implementation)
    {
        var service = CreateLockService(implementation);
        var key = $"both-reacquire-{implementation}-{Guid.NewGuid():N}";
        var handle1 = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle1.ShouldNotBeNull();
        await handle1.ReleaseAsync();
        var handle2 = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle2.ShouldNotBeNull();
        await handle2.ReleaseAsync();
    }

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task AcquireAsync_WhenHeldByOther_ReturnsNullAfterTimeout(string implementation)
    {
        var service = CreateLockService(implementation);
        var key = $"both-blocked-{implementation}-{Guid.NewGuid():N}";
        var handle1 = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle1.ShouldNotBeNull();
        var handle2 = await service.AcquireAsync(key, TimeSpan.FromMilliseconds(200), ct: TestContext.Current.CancellationToken);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync();
    }

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task ExecuteWithLockAsync_RunsAction(string implementation)
    {
        var service = CreateLockService(implementation);
        var key = $"both-execute-{implementation}-{Guid.NewGuid():N}";
        var executed = false;
        await service.ExecuteWithLockAsync(
            key, async ct => {
                await Task.Delay(10, ct);
                executed = true;
            }, ct: TestContext.Current.CancellationToken);

        executed.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task ExecuteWithLockAsync_ReturnsValue(string implementation)
    {
        var service = CreateLockService(implementation);
        var key = $"both-return-{implementation}-{Guid.NewGuid():N}";
        var result = await service.ExecuteWithLockAsync(key, _ => Task.FromResult("result"), ct: TestContext.Current.CancellationToken);
        result.ShouldBe("result");
    }

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task AcquireAsync_KeyNormalizedToLowercase(string implementation)
    {
        var service = CreateLockService(implementation);
        var key = $"Both-MixedCase-{implementation}-{Guid.NewGuid():N}";
        var handle1 = await service.AcquireAsync(key, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle1.ShouldNotBeNull();
        var handle2 = await service.AcquireAsync(key.ToLowerInvariant(), TimeSpan.FromMilliseconds(200), ct: TestContext.Current.CancellationToken);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync();
    }

    [Theory]
    [MemberData(nameof(LockImplementations))]
    public async Task DifferentKeys_CanBeLockedConcurrently(string implementation)
    {
        var service = CreateLockService(implementation);
        var keyA = $"both-key-a-{implementation}-{Guid.NewGuid():N}";
        var keyB = $"both-key-b-{implementation}-{Guid.NewGuid():N}";
        var handle1 = await service.AcquireAsync(keyA, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        var handle2 = await service.AcquireAsync(keyB, TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken);
        handle1.ShouldNotBeNull();
        handle2.ShouldNotBeNull();
        await handle1.ReleaseAsync();
        await handle2.ReleaseAsync();
    }
}