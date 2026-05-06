using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Api.Services.Crud.Create;
using Lyo.Cache;
using Lyo.Common.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Models.Constants;
using JobRunResultEnum = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Postgres.Tests;

public class JobServiceIntegrationTests
{
    private readonly JobPostgresFixture _fixture;

    public JobServiceIntegrationTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Log_WithValidJobRun_ReturnsSuccess()
    {
        var createService = _fixture.CreateService;
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var runResult = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Running;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var logReq = new JobRunLogReq(JobLogLevel.Information, "Test log message", "TestContext");
        var result = await jobService.Log(jobRunId, logReq);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test log message", result.Data!.Message);
    }

    [Fact]
    public async Task CreateJobRun_WhenConnected_ReturnsQueuedRunAndPublishesEvent()
    {
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        var result = await jobService.CreateJobRun(runReq, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(JobState.Queued, result.Data!.State);
        Assert.Contains(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == result.Data.Id);
    }

    [Fact]
    public async Task CreateJobRun_WhenMqDisconnected_ReturnsFailure()
    {
        var disconnectedPublisher = new FakeJobEventPublisher();
        disconnectedPublisher.SetConnected(false);
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _fixture.ConnectionString });
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        services.AddSingleton<IJobEventPublisher>(_ => disconnectedPublisher);
        services.AddScoped<JobService>();
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var result = await jobService.CreateJobRun(runReq, TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task StartedJobRun_WhenJobExists_UpdatesStateAndReturnsSuccess()
    {
        var createService = _fixture.CreateService;
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var runResult = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Queued;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var (result, error) = await jobService.StartedJobRun(jobRunId);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Running, result.State);
    }

    [Fact]
    public async Task StartedJobRun_WhenJobNotFound_ReturnsError()
    {
        var jobService = _fixture.JobService;
        var nonExistentId = Guid.NewGuid();
        var (result, error) = await jobService.StartedJobRun(nonExistentId);
        Assert.NotNull(error);
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelJobRun_WhenJobRunning_SetsStateToCancellingAndPublishesEvent()
    {
        var createService = _fixture.CreateService;
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var runResult = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Running;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.StartedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        var (result, error) = await jobService.CancelJobRun(jobRunId);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Cancelling, result.State);
        Assert.Contains(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCancelled" && e.RunId == jobRunId);
    }

    [Fact]
    public async Task CancelJobRun_WhenJobNotRunning_ReturnsError()
    {
        var createService = _fixture.CreateService;
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var runResult = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Finished; // Finished is not a cancellable state
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.FinishedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var (result, error) = await jobService.CancelJobRun(jobRunId);
        Assert.NotNull(error);
        Assert.Null(result);
    }

    [Fact]
    public async Task FinishedJobRun_WhenJobRunning_PersistsResultsAndReturnsSuccess()
    {
        var createService = _fixture.CreateService;
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var runResult = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Running;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.StartedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var results = new List<JobRunResultReq> { new(Constants.Data.JobRunResultKey.Result, JobParameterType.String, nameof(JobRunResultEnum.Success)) };
        var (result, error) = await jobService.FinishedJobRun(jobRunId, results);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Finished, result.State);
        Assert.Equal(JobRunResultEnum.Success, result.Result);
    }

    [Fact]
    public async Task RerunJob_WhenJobExists_CreatesNewRun()
    {
        var createService = _fixture.CreateService;
        var jobService = _fixture.JobService;
        var runReq = new JobRunReq(_fixture.JobDefinitionId, "test-user", false);
        var runResult = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Finished;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.StartedTimestamp = DateTime.UtcNow;
                ctx.Entity.FinishedTimestamp = DateTime.UtcNow;
                ctx.Entity.Result = JobRunResultEnum.Success;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var result = await jobService.RerunJob(jobRunId);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEqual(jobRunId, result.Data!.Id);
        Assert.Equal(JobState.Queued, result.Data.State);
    }

    [Fact]
    public async Task RerunJob_WhenJobNotFound_ReturnsFailure()
    {
        var jobService = _fixture.JobService;
        var nonExistentId = Guid.NewGuid();
        var result = await jobService.RerunJob(nonExistentId);
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
    }
}
