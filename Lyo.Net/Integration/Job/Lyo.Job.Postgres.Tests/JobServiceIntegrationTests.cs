using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Api.Services.Crud.Create;
using Lyo.Cache;
using Lyo.Common.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.MessageQueue;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Testing;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Constants = Lyo.Job.Models.Constants;
using JobRunResultEnum = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Postgres.Tests;

public class JobServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private ICreateService<JobContext> _createService;
    private Guid _jobDefinitionId;
    private JobService? _jobService;
    private IServiceProvider? _serviceProvider;

    public JobServiceIntegrationTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var fakeMq = new FakeMqService();
        await fakeMq.ConnectAsync().ConfigureAwait(false);
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        services.AddSingleton<IRabbitMqService>(_ => fakeMq);
        services.AddSingleton<IMqService>(_ => fakeMq);
        services.AddScoped<JobService>();
        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
            await using var context = await factory.CreateDbContextAsync();
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        using (var scope = _serviceProvider.CreateScope()) {
            _createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
            await CreateJobDefinitionAsync().ConfigureAwait(false);
        }

        using (var scope = _serviceProvider.CreateScope())
            _jobService = scope.ServiceProvider.GetRequiredService<JobService>();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    private async Task CreateJobDefinitionAsync()
    {
        var req = new JobDefinitionReq {
            Name = "TestJob",
            Description = "Integration test job",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true
        };

        var result = await _createService.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
            });

        Assert.True(result.IsSuccess);
        _jobDefinitionId = result.Data!.Id;
    }

    [Fact]
    public async Task Log_WithValidJobRun_ReturnsSuccess()
    {
        Assert.NotNull(_jobService);
        Assert.NotNull(_createService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var runResult = await _createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Running);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var logReq = new JobRunLogReq(JobLogLevel.Information, "Test log message", "TestContext");
        var result = await _jobService.Log(jobRunId, logReq).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test log message", result.Data!.Message);
    }

    [Fact]
    public async Task CreateJobRun_WhenMqConnected_ReturnsSuccess()
    {
        Assert.NotNull(_jobService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var result = await _jobService.CreateJobRun(runReq).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(JobState.Queued, result.Data!.State);
    }

    [Fact]
    public async Task CreateJobRun_WhenMqDisconnected_ReturnsFailure()
    {
        Assert.NotNull(_serviceProvider);
        var fakeMq = new FakeMqService();
        fakeMq.SetConnected(false);
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _container.GetConnectionString() });
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        services.AddSingleton<IRabbitMqService>(_ => fakeMq);
        services.AddSingleton<IMqService>(_ => fakeMq);
        services.AddScoped<JobService>();
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var result = await jobService.CreateJobRun(runReq).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task StartedJobRun_WhenJobExists_UpdatesStateAndReturnsSuccess()
    {
        Assert.NotNull(_jobService);
        Assert.NotNull(_createService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var runResult = await _createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Queued);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var (result, error) = await _jobService.StartedJobRun(jobRunId).ConfigureAwait(false);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Running, result.State);
    }

    [Fact]
    public async Task StartedJobRun_WhenJobNotFound_ReturnsError()
    {
        Assert.NotNull(_jobService);
        var nonExistentId = Guid.NewGuid();
        var (result, error) = await _jobService.StartedJobRun(nonExistentId).ConfigureAwait(false);
        Assert.NotNull(error);
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelJobRun_WhenJobRunning_ReturnsSuccess()
    {
        Assert.NotNull(_jobService);
        Assert.NotNull(_createService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var runResult = await _createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Running);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.StartedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var (result, error) = await _jobService.CancelJobRun(jobRunId).ConfigureAwait(false);
        Assert.Null(error);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CancelJobRun_WhenJobNotRunning_ReturnsError()
    {
        Assert.NotNull(_jobService);
        Assert.NotNull(_createService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var runResult = await _createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Queued);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var (result, error) = await _jobService.CancelJobRun(jobRunId).ConfigureAwait(false);
        Assert.NotNull(error);
        Assert.Null(result);
    }

    [Fact]
    public async Task FinishedJobRun_WhenJobRunning_PersistsResultsAndReturnsSuccess()
    {
        Assert.NotNull(_jobService);
        Assert.NotNull(_createService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var runResult = await _createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Running);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.StartedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var results = new List<JobRunResultReq> { new(Constants.Data.JobRunResultKey.Result, JobParameterType.String, nameof(JobRunResultEnum.Success)) };
        var (result, error) = await _jobService.FinishedJobRun(jobRunId, results).ConfigureAwait(false);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Finished, result.State);
        Assert.Equal(JobRunResultEnum.Success, result.Result);
    }

    [Fact]
    public async Task RerunJob_WhenJobExists_CreatesNewRun()
    {
        Assert.NotNull(_jobService);
        Assert.NotNull(_createService);
        var runReq = new JobRunReq(_jobDefinitionId, "test-user", false);
        var runResult = await _createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            runReq, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Finished);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                ctx.Entity.StartedTimestamp = DateTime.UtcNow;
                ctx.Entity.FinishedTimestamp = DateTime.UtcNow;
                ctx.Entity.Result = nameof(JobRunResultEnum.Success);
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(runResult.IsSuccess);
        var jobRunId = runResult.Data!.Id;
        var result = await _jobService.RerunJob(jobRunId).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEqual(jobRunId, result.Data!.Id);
        Assert.Equal(JobState.Queued, result.Data.State);
    }

    [Fact]
    public async Task RerunJob_WhenJobNotFound_ReturnsFailure()
    {
        Assert.NotNull(_jobService);
        var nonExistentId = Guid.NewGuid();
        var result = await _jobService.RerunJob(nonExistentId).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
    }
}