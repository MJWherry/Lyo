using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.Lock.Tests;

public class LocalLockServiceTests
{
    private readonly LocalLockService _service;

    public LocalLockServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _service = new(loggerFactory.CreateLogger<LocalLockService>());
    }

    [Fact]
    public async Task AcquireAsync_ReturnsHandle()
    {
        var handle = await _service.AcquireAsync("test-key-1", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_AfterRelease_CanAcquireAgain()
    {
        var handle1 = await _service.AcquireAsync("test-key-2", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        var handle2 = await _service.AcquireAsync("test-key-2", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldNotBeNull();
        await handle2.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_WhenHeldByOther_ReturnsNullAfterTimeout()
    {
        var handle1 = await _service.AcquireAsync("test-key-3", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        var handle2 = await _service.AcquireAsync("test-key-3", TimeSpan.FromMilliseconds(100), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_KeyNormalizedToLowercase()
    {
        var handle1 = await _service.AcquireAsync("Test-Key-MixedCase", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        var handle2 = await _service.AcquireAsync("test-key-mixedcase", TimeSpan.FromMilliseconds(100), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_RunsAction()
    {
        var executed = false;
        await _service.ExecuteWithLockAsync(
            "test-key-4", async ct => {
                await Task.Delay(10, ct).ConfigureAwait(false);
                executed = true;
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteWithLockAsync_ReturnsValue()
    {
        var result = await _service.ExecuteWithLockAsync("test-key-5", _ => Task.FromResult(42), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldBe(42);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_WhenLockHeld_ThrowsTimeoutException()
    {
        var handle = await _service.AcquireAsync("test-key-6", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await Assert.ThrowsAsync<TimeoutException>(async () => await _service.ExecuteWithLockAsync(
            "test-key-6", _ => Task.CompletedTask, TimeSpan.FromMilliseconds(50), ct: TestContext.Current.CancellationToken).ConfigureAwait(false));

        await handle.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_ThrowsOnNullOrEmptyKey()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AcquireAsync(null!, ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AcquireAsync("", ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AcquireAsync("   ", ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
    }

    [Fact]
    public async Task Release_IsIdempotent()
    {
        var handle = await _service.AcquireAsync("test-key-7", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
        await handle.ReleaseAsync().ConfigureAwait(false);
        handle.Dispose();
    }

    [Fact]
    public async Task DifferentKeys_CanBeLockedConcurrently()
    {
        var handle1 = await _service.AcquireAsync("key-a", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var handle2 = await _service.AcquireAsync("key-b", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        handle2.ShouldNotBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        await handle2.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task ExecuteWithLockAsync_ReleasesLockOnException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.ExecuteWithLockAsync(
            "test-key-8", _ => throw new InvalidOperationException("test"), ct: TestContext.Current.CancellationToken).ConfigureAwait(false));

        var handle = await _service.AcquireAsync("test-key-8", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_RespectsCancellation()
    {
        var handle = await _service.AcquireAsync("test-key-9", TimeSpan.FromSeconds(5), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _service.AcquireAsync("test-key-9", TimeSpan.FromSeconds(30), ct: cts.Token)).ConfigureAwait(false);
        await handle.ReleaseAsync().ConfigureAwait(false);
    }
}