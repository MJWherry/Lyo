using Lyo.Reporting.Models;
using Lyo.Reporting.Postgres;
using Microsoft.Extensions.Options;

namespace Lyo.Reporting.Tests;

public sealed class ReportGenerationThrottleTests
{
    private static ReportGenerationThrottle Create(int maxConcurrent)
        => new(Options.Create(new PostgresReportingOptions { ConnectionString = "unused", MaxConcurrentGenerations = maxConcurrent })) {
            AcquireTimeout = TimeSpan.FromMilliseconds(100)
        };

    [Fact]
    public async Task Unlimited_returns_null_releaser() => Assert.Null(await Create(0).AcquireAsync(TestContext.Current.CancellationToken));

    [Fact]
    public async Task Saturated_throttle_fails_with_busy_and_recovers_after_release()
    {
        var throttle = Create(1);
        var slot = await throttle.AcquireAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(slot);
        var ex = await Assert.ThrowsAsync<ReportBusyException>(() => throttle.AcquireAsync(TestContext.Current.CancellationToken));
        Assert.Contains("busy", ex.Message, StringComparison.OrdinalIgnoreCase);
        slot!.Dispose();
        var next = await throttle.AcquireAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(next);
        next!.Dispose();
    }

    [Fact]
    public async Task Double_dispose_releases_only_once()
    {
        var throttle = Create(1);
        var slot = await throttle.AcquireAsync(TestContext.Current.CancellationToken);
        slot!.Dispose();
        slot.Dispose();
        var a = await throttle.AcquireAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ReportBusyException>(() => throttle.AcquireAsync(TestContext.Current.CancellationToken));
        a!.Dispose();
    }

    [Fact]
    public void Options_validate_rejects_negative_concurrency_and_nonpositive_retention()
    {
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", MaxConcurrentGenerations = -1 }.Validate());
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", GenerationRetention = TimeSpan.Zero }.Validate());
        new PostgresReportingOptions { ConnectionString = "x", MaxConcurrentGenerations = 4, GenerationRetention = TimeSpan.FromDays(30) }.Validate();
    }

    [Fact]
    public void Options_validate_rejects_nonpositive_timeouts_and_interval()
    {
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", GenerationTimeout = TimeSpan.Zero }.Validate());
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", StuckGenerationTimeout = TimeSpan.FromSeconds(-1) }.Validate());
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", MaintenanceInterval = TimeSpan.Zero }.Validate());

        // Null timeouts disable the features; defaults are valid out of the box.
        new PostgresReportingOptions { ConnectionString = "x", GenerationTimeout = null, StuckGenerationTimeout = null }.Validate();
        new PostgresReportingOptions { ConnectionString = "x" }.Validate();
    }
}