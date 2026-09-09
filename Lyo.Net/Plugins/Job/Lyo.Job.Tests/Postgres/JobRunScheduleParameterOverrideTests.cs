using Lyo.Api;
using Lyo.Cache;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// A "run this schedule" request can send only <c>JobScheduleId</c>. CreateJobRun copies enabled schedule parameters onto omitted keys before definition back-fill, so schedule
/// values win the same way they would when the scheduler fires.
/// </summary>
[Trait("Category", "Integration")]
public class JobRunScheduleParameterOverrideTests
{
    private readonly JobPostgresFixture _fixture;

    public JobRunScheduleParameterOverrideTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ScheduleParameter_IsAppliedWhenTheCallerOmitsTheKey()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var (definitionId, scheduleId) = await CreateDefinitionWithScheduleAsync(sp, definitionValue: "200", scheduleValue: "50", scheduleEnabled: true);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false, scheduleId: scheduleId), TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        Assert.Equal(scheduleId, created.Data!.JobScheduleId);
        Assert.Equal("50", Assert.Single(created.Data.JobRunParameters!, p => p.Key == "PageSize").Value);
    }

    [Fact]
    public async Task SuppliedValue_IsNotOverwrittenByTheScheduleParameter()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var (definitionId, scheduleId) = await CreateDefinitionWithScheduleAsync(sp, definitionValue: "200", scheduleValue: "50", scheduleEnabled: true);
        var request = new JobRunReq(definitionId, "test-user", false, scheduleId: scheduleId) {
            JobRunParameters = {
                new() { Key = "PageSize", Type = LyoTypeInfo.Int.FullName, Value = "99", Enabled = true }
            }
        };

        var created = await scope.ServiceProvider.GetRequiredService<JobService>().CreateJobRun(request, TestContext.Current.CancellationToken);
        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        Assert.Equal("99", Assert.Single(created.Data!.JobRunParameters!, p => p.Key == "PageSize").Value);
    }

    [Fact]
    public async Task DisabledScheduleParameter_DoesNotOverrideTheDefinitionDefault()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var (definitionId, scheduleId) = await CreateDefinitionWithScheduleAsync(sp, definitionValue: "200", scheduleValue: "50", scheduleEnabled: false);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false, scheduleId: scheduleId), TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        Assert.Equal("200", Assert.Single(created.Data!.JobRunParameters!, p => p.Key == "PageSize").Value);
    }

    [Fact]
    public async Task UnknownScheduleId_IsRejected()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false, scheduleId: LyoGuid.CreateCombPostgres()), TestContext.Current.CancellationToken);

        Assert.False(created.IsSuccess);
        Assert.Contains("was not found", created.Error!.GetFullMessage());
    }

    [Fact]
    public async Task ScheduleFromAnotherDefinition_IsRejected()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp);
        var other = await CreateDefinitionWithScheduleAsync(sp, definitionValue: "200", scheduleValue: "50", scheduleEnabled: true);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false, scheduleId: other.ScheduleId), TestContext.Current.CancellationToken);

        Assert.False(created.IsSuccess);
        Assert.Contains("does not belong", created.Error!.GetFullMessage());
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _fixture.ConnectionString });
        services.AddSingleton<IJobEventPublisher>(_ => new FakeJobEventPublisher());
        services.AddScoped<JobService>();
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> CreateDefinitionAsync(ServiceProvider sp)
    {
        var definitionId = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync(sp);
        db.JobDefinitions.Add(
            new() {
                Id = definitionId,
                Name = $"SchedRun-{definitionId:N}"[..32],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });
        db.JobParameters.Add(
            new() {
                Id = LyoGuid.CreateCombPostgres(),
                JobDefinitionId = definitionId,
                Key = "PageSize",
                Type = LyoTypeInfo.Int.FullName,
                Value = "200",
                CreatedTimestamp = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return definitionId;
    }

    private static async Task<(Guid DefinitionId, Guid ScheduleId)> CreateDefinitionWithScheduleAsync(
        ServiceProvider sp, string definitionValue, string scheduleValue, bool scheduleEnabled)
    {
        var definitionId = LyoGuid.CreateCombPostgres();
        var scheduleId = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync(sp);
        db.JobDefinitions.Add(
            new() {
                Id = definitionId,
                Name = $"SchedRun-{definitionId:N}"[..32],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });
        db.JobParameters.Add(
            new() {
                Id = LyoGuid.CreateCombPostgres(),
                JobDefinitionId = definitionId,
                Key = "PageSize",
                Type = LyoTypeInfo.Int.FullName,
                Value = definitionValue,
                CreatedTimestamp = DateTime.UtcNow
            });
        db.JobSchedules.Add(
            new() {
                Id = scheduleId,
                JobDefinitionId = definitionId,
                Type = nameof(ScheduleType.SetTimes),
                DayFlags = nameof(DayFlags.EveryDay),
                MonthFlags = nameof(MonthFlags.EveryMonth),
                MisfirePolicy = nameof(JobMisfirePolicy.Skip),
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });
        db.JobScheduleParameters.Add(
            new() {
                Id = LyoGuid.CreateCombPostgres(),
                JobScheduleId = scheduleId,
                Key = "PageSize",
                Type = LyoTypeInfo.Int.FullName,
                Value = scheduleValue,
                Enabled = scheduleEnabled,
                CreatedTimestamp = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (definitionId, scheduleId);
    }

    private static async Task<JobContext> CreateDbContextAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }
}
