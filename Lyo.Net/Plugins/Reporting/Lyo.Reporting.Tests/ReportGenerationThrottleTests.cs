using Lyo.Reporting.Models;
using Lyo.Reporting.Postgres;
namespace Lyo.Reporting.Tests;

public sealed class ReportGenerationThrottleTests
{
    private static ReportGenerationThrottle Create(int maxConcurrent)
        => new(new PostgresReportingOptions { ConnectionString = "unused", MaxConcurrentGenerations = maxConcurrent }) {
            AcquireTimeout = TimeSpan.FromMilliseconds(100)
        };

    [Fact]
    public async Task AcquireAsync_Unlimited_ReturnsNullReleaser() => Assert.Null(await Create(0).AcquireAsync(TestContext.Current.CancellationToken));

    [Fact]
    public async Task AcquireAsync_Saturated_FailsBusyThenRecovers()
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
    public async Task Dispose_Twice_ReleasesOnce()
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
    public void Validate_NegativeConcurrencyOrNonpositiveRetention_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", MaxConcurrentGenerations = -1 }.Validate());
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", GenerationRetention = TimeSpan.Zero }.Validate());
        new PostgresReportingOptions { ConnectionString = "x", MaxConcurrentGenerations = 4, GenerationRetention = TimeSpan.FromDays(30) }.Validate();
    }

    [Fact]
    public void Validate_NonpositiveTimeoutsOrInterval_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", GenerationTimeout = TimeSpan.Zero }.Validate());
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", StuckGenerationTimeout = TimeSpan.FromSeconds(-1) }.Validate());
        Assert.Throws<ArgumentException>(() => new PostgresReportingOptions { ConnectionString = "x", MaintenanceInterval = TimeSpan.Zero }.Validate());

        // Null timeouts disable the features; defaults are valid without extra setup.
        new PostgresReportingOptions { ConnectionString = "x", GenerationTimeout = null, StuckGenerationTimeout = null }.Validate();
        new PostgresReportingOptions { ConnectionString = "x" }.Validate();
    }
}