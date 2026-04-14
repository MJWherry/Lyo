using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.Lock.Tests;

public class LocalKeyedSemaphoreServiceTests
{
    private readonly LocalKeyedSemaphoreService _service;

    public LocalKeyedSemaphoreServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _service = new(loggerFactory.CreateLogger<LocalKeyedSemaphoreService>());
    }

    [Fact]
    public async Task AcquireAsync_ReturnsPermitHandle()
    {
        var handle = await _service.AcquireAsync("semaphore-key-1", 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_AllowsConcurrencyUpToMax()
    {
        var handle1 = await _service.AcquireAsync("semaphore-key-2", 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var handle2 = await _service.AcquireAsync("semaphore-key-2", 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var handle3 = await _service.AcquireAsync("semaphore-key-2", 2, TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        handle2.ShouldNotBeNull();
        handle3.ShouldBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        await handle2.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_AfterRelease_AllowsDifferentFutureConcurrency()
    {
        var handle1 = await _service.AcquireAsync("semaphore-key-3", 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        var handle2 = await _service.AcquireAsync("semaphore-key-3", 3, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldNotBeNull();
        await handle2.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_WhenMaxChangesWhileActive_Throws()
    {
        var handle = await _service.AcquireAsync("semaphore-key-4", 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.AcquireAsync(
            "semaphore-key-4", 3, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false));

        Assert.Contains("max concurrency 2", ex.Message, StringComparison.Ordinal);
        await handle.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_KeyNormalizedToLowercase()
    {
        var handle1 = await _service.AcquireAsync("Semaphore-Key-MixedCase", 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        var handle2 = await _service.AcquireAsync("semaphore-key-mixedcase", 1, TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle2.ShouldBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task ExecuteAsync_RunsAction()
    {
        var executed = false;
        await _service.ExecuteAsync(
            "semaphore-key-5", 2, async ct => {
                await Task.Delay(10, ct).ConfigureAwait(false);
                executed = true;
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsValue()
    {
        var result = await _service.ExecuteAsync("semaphore-key-6", 2, _ => Task.FromResult(42), ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldBe(42);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxReached_ThrowsTimeoutException()
    {
        var handle = await _service.AcquireAsync("semaphore-key-7", 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await Assert.ThrowsAsync<TimeoutException>(async () => await _service.ExecuteAsync(
            "semaphore-key-7", 1, _ => Task.CompletedTask, TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken).ConfigureAwait(false));

        await handle.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_ThrowsOnInvalidArguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AcquireAsync(null!, 1, ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AcquireAsync("", 1, ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AcquireAsync("   ", 1, ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.AcquireAsync("semaphore-key-8", 0, ct: TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(false);
    }

    [Fact]
    public async Task Release_IsIdempotent()
    {
        var handle = await _service.AcquireAsync("semaphore-key-9", 2, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        await handle.ReleaseAsync().ConfigureAwait(false);
        await handle.ReleaseAsync().ConfigureAwait(false);
        handle.Dispose();
    }

    [Fact]
    public async Task DifferentKeys_CanBeAcquiredConcurrently()
    {
        var handle1 = await _service.AcquireAsync("semaphore-key-a", 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var handle2 = await _service.AcquireAsync("semaphore-key-b", 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle1.ShouldNotBeNull();
        handle2.ShouldNotBeNull();
        await handle1.ReleaseAsync().ConfigureAwait(false);
        await handle2.ReleaseAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task AcquireAsync_RespectsCancellation()
    {
        var handle = await _service.AcquireAsync("semaphore-key-10", 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(false);
        handle.ShouldNotBeNull();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _service.AcquireAsync("semaphore-key-10", 1, TimeSpan.FromSeconds(30), cts.Token)).ConfigureAwait(false);
        await handle.ReleaseAsync().ConfigureAwait(false);
    }
}