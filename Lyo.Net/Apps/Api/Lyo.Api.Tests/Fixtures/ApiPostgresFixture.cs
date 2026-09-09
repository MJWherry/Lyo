using Lyo.Api.Services.Crud.Create;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Tests.Fixtures;

/// <summary>Shared fixture for PostgreSQL-backed API checks. Starts a Testcontainers Postgres instance and exposes the app host service provider.</summary>
public sealed class ApiPostgresFixture : PostgresContainerFixtureBase
{
    public ApiWebApplicationFactory Factory { get; private set; } = null!;

    public IServiceProvider ServiceProvider => Factory.Services;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        Factory = new(connectionString);
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    protected override async ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken) => await Factory.DisposeAsync();

    public HttpClient CreateClient() => Factory.CreateClient();

    public IServiceScope CreateScope() => Factory.Services.CreateScope();

    /// <summary>Creates a scope and seeds a JobDefinition for checks. Returns the created Id.</summary>
    public async Task<Guid> SeedJobDefinitionAsync(string name = "TestJob", string? description = null, DateTime? createdTimestamp = null)
    {
        using var scope = CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobDefinitionReq(name, description ?? "Integration test job") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var result = await createService.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
                if (createdTimestamp.HasValue)
                    ctx.Entity.CreatedTimestamp = createdTimestamp.Value;
            }, ct: TestContext.Current.CancellationToken);

        OperationHelpers.ThrowIf(!result.IsSuccess, $"Failed to seed JobDefinition: {result.Error?.Detail}");
        return result.Data!.Id;
    }

    /// <summary>Creates a scope and seeds a JobRun for checks. Returns the created Id.</summary>
    public async Task<Guid> SeedJobRunAsync(Guid jobDefinitionId, string createdBy = "test-user")
    {
        using var scope = CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobRunReq(jobDefinitionId, createdBy, false);
        var result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = JobState.Queued;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), null, TestContext.Current.CancellationToken);

        OperationHelpers.ThrowIf(!result.IsSuccess, $"Failed to seed JobRun: {result.Error?.Detail}");
        return result.Data!.Id;
    }

    /// <summary>Creates a scope and seeds a JobRunLog for checks. Returns the created Id.</summary>
    public async Task<Guid> SeedJobRunLogAsync(Guid jobRunId, string message = "Test log message", JobLogLevel level = JobLogLevel.Information)
    {
        using var scope = CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobRunLogReq(level, message, "TestContext");
        var result = await createService.CreateAsync<JobRunLogReq, JobRunLog, JobRunLogRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.JobRunId = jobRunId;
            }, ct: TestContext.Current.CancellationToken);

        OperationHelpers.ThrowIf(!result.IsSuccess, $"Failed to seed JobRunLog: {result.Error?.Detail}");
        return result.Data!.Id;
    }

    /// <summary>Creates a scope and seeds a JobParameter for checks. Returns the created Id.</summary>
    public async Task<Guid> SeedJobParameterAsync(Guid jobDefinitionId, string key = "Tag", string? options = null)
    {
        using var scope = CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobParameterReq {
            JobDefinitionId = jobDefinitionId,
            Key = key,
            Type = LyoTypeInfo.String.FullName,
            Options = options
        };
        var result = await createService.CreateAsync<JobParameterReq, JobParameter, JobParameterRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.JobDefinitionId = jobDefinitionId;
            }, ct: TestContext.Current.CancellationToken);

        OperationHelpers.ThrowIf(!result.IsSuccess, $"Failed to seed JobParameter: {result.Error?.Detail}");
        return result.Data!.Id;
    }

    /// <summary>Creates a scope and seeds a JobSchedule for checks. Returns the created Id.</summary>
    public async Task<Guid> SeedJobScheduleAsync(Guid jobDefinitionId, string? description = null)
    {
        using var scope = CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobScheduleReq {
            JobDefinitionId = jobDefinitionId,
            Type = ScheduleType.SetTimes,
            DayFlags = DayFlags.EveryDay,
            Description = description,
            Times = [new TimeOnly(9, 0)],
            Enabled = true
        };
        var result = await createService.CreateAsync<JobScheduleReq, JobSchedule, JobScheduleRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.JobDefinitionId = jobDefinitionId;
            }, ct: TestContext.Current.CancellationToken);

        OperationHelpers.ThrowIf(!result.IsSuccess, $"Failed to seed JobSchedule: {result.Error?.Detail}");
        return result.Data!.Id;
    }
}