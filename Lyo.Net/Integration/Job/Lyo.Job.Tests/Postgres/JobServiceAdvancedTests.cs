using Lyo.Api.Services.Crud.Create;
using Lyo.Common.Records;
using Lyo.Job.Models.Builders;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Tests.Postgres;

[Trait("Category", "Integration")]
public class JobServiceAdvancedTests
{
    private readonly JobPostgresFixture _fixture;

    public JobServiceAdvancedTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateJobRun_WithIdempotencyKey_ReturnsExistingRun()
    {
        var jobService = _fixture.JobService;
        const string idempotencyKey = "idem-advanced-1";
        var firstReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = idempotencyKey };
        var first = await jobService.CreateJobRun(firstReq, TestContext.Current.CancellationToken);
        Assert.True(first.IsSuccess);

        var secondReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = idempotencyKey };
        var second = await jobService.CreateJobRun(secondReq, TestContext.Current.CancellationToken);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.Id, second.Data!.Id);
    }

    [Fact]
    public async Task CreateJobRun_WhenRateLimitExceeded_ReturnsFailure()
    {
        var definitionId = await CreateDefinitionWithMaxRunsPerHourAsync(1);
        var jobService = _fixture.JobService;

        var first = await jobService.CreateJobRun(new JobRunReq(definitionId, "test-user", false), TestContext.Current.CancellationToken);
        Assert.True(first.IsSuccess);

        var second = await jobService.CreateJobRun(new JobRunReq(definitionId, "test-user", false), TestContext.Current.CancellationToken);

        Assert.False(second.IsSuccess);
        Assert.NotNull(second.Error);
        Assert.Contains("hourly run limit", second.Error!.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateJobRun_WhenDryRun_DoesNotPersistRun()
    {
        var jobService = _fixture.JobService;
        var factory = GetDbContextFactory();
        var countBefore = await CountRunsAsync(factory);

        var result = await jobService.CreateJobRun(
            new JobRunReq(_fixture.JobDefinitionId, "test-user", false) { DryRun = true },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.DryRun);
        Assert.Equal(Guid.Empty, result.Data.Id);

        var countAfter = await CountRunsAsync(factory);
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task GetNextRuns_WhenScheduleExists_ReturnsFutureDates()
    {
        var definitionId = await CreateDefinitionWithScheduleAsync();
        var jobService = _fixture.JobService;

        var nextRuns = await jobService.GetNextRuns(definitionId, count: 5, TestContext.Current.CancellationToken);

        Assert.NotEmpty(nextRuns);
        Assert.True(nextRuns.All(d => d > DateTime.UtcNow));
        Assert.Equal(nextRuns.OrderBy(d => d).ToList(), nextRuns.ToList());
    }

    private async Task<Guid> CreateDefinitionWithMaxRunsPerHourAsync(int maxRunsPerHour)
    {
        var createService = _fixture.CreateService;
        var req = new JobDefinitionReq {
            Name = $"RateLimit-{Guid.NewGuid():N}",
            Description = "Rate limit test",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true,
            MaxRunsPerHour = maxRunsPerHour
        };

        var result = await createService.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
                ctx.Entity.MaxRunsPerHour = maxRunsPerHour;
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return result.Data!.Id;
    }

    private async Task<Guid> CreateDefinitionWithScheduleAsync()
    {
        var createService = _fixture.CreateService;
        var definitionId = Guid.NewGuid();
        var req = new JobDefinitionReq {
            Name = $"Scheduled-{Guid.NewGuid():N}",
            Description = "Schedule test",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true,
            CreateSchedules = [
                new JobScheduleBuilder()
                    .EveryDay()
                    .SetTimes("00:00", "06:00", "12:00", "18:00")
                    .WithDescription("Quarter-day schedule")
                    .Build()
            ]
        };

        var result = await createService.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            req, ctx => {
                ctx.Entity.Id = definitionId;
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
                foreach (var schedule in ctx.Entity.JobSchedules)
                    schedule.JobDefinitionId = definitionId;
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return definitionId;
    }

    private IDbContextFactory<JobContext> GetDbContextFactory()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
    }

    private static async Task<int> CountRunsAsync(IDbContextFactory<JobContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.JobRuns.CountAsync();
    }
}
