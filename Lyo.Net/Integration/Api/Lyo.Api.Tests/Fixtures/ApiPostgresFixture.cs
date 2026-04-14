using Lyo.Api.Services.Crud.Create;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Lyo.Api.Tests.Fixtures;

/// <summary>Shared fixture for PostgreSQL-backed API tests. Starts a Testcontainers Postgres instance and exposes the app host service provider.</summary>
public sealed class ApiPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString { get; private set; } = null!;

    public ApiWebApplicationFactory Factory { get; private set; } = null!;

    public IServiceProvider ServiceProvider => Factory.Services;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        Factory = new(ConnectionString);
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public HttpClient CreateClient() => Factory.CreateClient();

    public IServiceScope CreateScope() => Factory.Services.CreateScope();

    /// <summary>Creates a scope and seeds a JobDefinition for tests. Returns the created Id.</summary>
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

    /// <summary>Creates a scope and seeds a JobRun for tests. Returns the created Id.</summary>
    public async Task<Guid> SeedJobRunAsync(Guid jobDefinitionId, string createdBy = "test-user")
    {
        using var scope = CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobRunReq(jobDefinitionId, createdBy, false);
        var result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = nameof(JobState.Queued);
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            }, ctx => ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load(), TestContext.Current.CancellationToken);

        OperationHelpers.ThrowIf(!result.IsSuccess, $"Failed to seed JobRun: {result.Error?.Detail}");
        return result.Data!.Id;
    }

    /// <summary>Creates a scope and seeds a JobRunLog for tests. Returns the created Id.</summary>
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
}