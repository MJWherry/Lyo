using Lyo.IO.Temp.Models;
using Lyo.Schedule.Models;
using Lyo.Testing;
#if NET6_0_OR_GREATER
using Microsoft.Extensions.Logging;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Scheduler.Tests;

public class SchedulerServiceTests : IDisposable, IAsyncDisposable
{
    private readonly SchedulerService _service;
    private readonly IOTempSession _tempSession;

    public SchedulerServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var rootDir = Path.Combine(Path.GetTempPath(), "lyo-scheduler-tests");
        Directory.CreateDirectory(rootDir);
        _tempSession = new(new() { RootDirectory = rootDir }, loggerFactory.CreateLogger<IOTempSession>());
        _service = new(new() { CheckIntervalMs = 100 }, loggerFactory.CreateLogger<SchedulerService>());
    }

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _service.Dispose();

    [Fact]
    public void AddSchedule_ValidSchedule_AddsToSchedules()
    {
        var def = ScheduleDefinition.Create().EveryDay().SetTimes("09:00").Build();
        _service.AddSchedule("test-1", null, def, _ => Task.CompletedTask);
        var schedules = _service.GetSchedules();
        Assert.Single(schedules);
        Assert.Equal("test-1", schedules.First().Id);
    }

    [Fact]
    public void RemoveSchedule_ExistingSchedule_ReturnsTrue()
    {
        var def = ScheduleDefinition.Create().EveryDay().SetTimes("09:00").Build();
        _service.AddSchedule("test-remove", null, def, _ => Task.CompletedTask);
        var removed = _service.RemoveSchedule("test-remove");
        Assert.True(removed);
        Assert.Empty(_service.GetSchedules());
    }

    [Fact]
    public void RemoveSchedule_NonExistent_ReturnsFalse()
    {
        var removed = _service.RemoveSchedule("nonexistent");
        Assert.False(removed);
    }

    [Fact]
    public void GetSchedule_ExistingId_ReturnsSchedule()
    {
        var def = ScheduleDefinition.Create().EveryDay().SetTimes("09:00").Build();
        _service.AddSchedule("test-get", null, def, _ => Task.CompletedTask);
        var found = _service.GetSchedule("test-get");
        Assert.NotNull(found);
        Assert.Equal("test-get", found.Id);
    }

    [Fact]
    public void GetSchedulesOrderedByNextRun_ReturnsOrderedList()
    {
        var def = ScheduleDefinition.Create().EveryDay().SetTimes("12:00", "09:00").Build();
        _service.AddSchedule("test-order", null, def, _ => Task.CompletedTask);
        var ordered = _service.GetSchedulesOrderedByNextRun(DateTime.UtcNow.Date.AddHours(8));
        Assert.Single(ordered);
        Assert.True(ordered[0].NextRun.HasValue);
    }

    [Fact]
    public void GetUpcomingRuns_ReturnsRuns()
    {
        var def = ScheduleDefinition.Create().EveryDay().SetTimes("09:00", "10:00", "11:00").Build();
        _service.AddSchedule("test-upcoming", null, def, _ => Task.CompletedTask);
        var runs = _service.GetUpcomingRuns(DateTime.UtcNow.Date.AddHours(8), 5);
        Assert.True(runs.Count >= 1);
    }

    [Fact]
    public void IsRunning_BeforeStart_ReturnsFalse() => Assert.False(_service.IsRunning);

    [Fact]
    public async Task StartAsync_OneShotSchedule_WritesToIOTempWhenExecuted()
    {
        var outputPath = _tempSession.GetFilePath("schedule-ran.txt");
        var ran = false;
        var executeAt = DateTime.UtcNow.AddMilliseconds(200);
        var def = ScheduleDefinition.Create().SetExecuteAt(executeAt).Build();
        _service.AddSchedule(
            "one-shot-test", null, def, async ct => {
                ran = true;
                await File.WriteAllTextAsync(outputPath, "executed", ct).ConfigureAwait(false);
            });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _service.StartAsync(cts.Token).ConfigureAwait(false);
        await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
        await _service.StopAsync(cts.Token).ConfigureAwait(false);
        Assert.True(ran);
        Assert.True(File.Exists(outputPath));
        Assert.Equal("executed", await File.ReadAllTextAsync(outputPath, cts.Token).ConfigureAwait(false));
    }
}