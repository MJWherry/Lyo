using System.Reflection;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Update;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

[Trait("Category", "Integration")]
[Collection(JobMaintenanceCollection.Name)]
public class JobWorkerInstanceMaintenanceTests
{
    private readonly JobPostgresFixture _fixture;

    public JobWorkerInstanceMaintenanceTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PatchWorkerInstance_HeartbeatUpdatesLastHeartbeatUtc()
    {
        var instanceId = await CreateWorkerInstanceAsync();
        using var scope = _fixture.ServiceProvider.CreateScope();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        var heartbeat = DateTime.UtcNow.AddSeconds(5);
        var patchRequest = PatchRequestBuilder.ForId(instanceId)
            .SetProperty("LastHeartbeatUtc", heartbeat)
            .SetProperty("InFlightCount", 0)
            .Build();

        var result = await patchService.PatchAsync<JobWorkerInstance, JobWorkerInstanceRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, result.Result);
        Assert.Equal(heartbeat, result.NewData!.LastHeartbeatUtc, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PatchWorkerInstance_SetsStoppedState()
    {
        var instanceId = await CreateWorkerInstanceAsync();
        using var scope = _fixture.ServiceProvider.CreateScope();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        var patchRequest = PatchRequestBuilder.ForId(instanceId)
            .SetProperty("State", JobWorkerInstanceState.Stopped)
            .SetProperty("InFlightCount", 0)
            .Build();

        var result = await patchService.PatchAsync<JobWorkerInstance, JobWorkerInstanceRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, result.Result);
        Assert.Equal(JobWorkerInstanceState.Stopped, result.NewData!.State);

        await using var db = await GetDbContextFactory().CreateDbContextAsync(TestContext.Current.CancellationToken);
        var stored = await db.JobWorkerInstances.AsNoTracking().SingleAsync(i => i.Id == instanceId, TestContext.Current.CancellationToken);
        Assert.Equal(nameof(JobWorkerInstanceState.Stopped), stored.State);
    }

    [Fact]
    public async Task Maintenance_PrunesStaleRunningWorkerInstance()
    {
        var instanceId = Guid.NewGuid();
        var staleHeartbeat = DateTime.UtcNow.AddMinutes(-10);

        await using (var db = await GetDbContextFactory().CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            db.JobWorkerInstances.Add(new JobWorkerInstance {
                Id = instanceId,
                WorkerType = "cs",
                MachineName = "test-host",
                ProcessId = 1,
                State = nameof(JobWorkerInstanceState.Running),
                InFlightCount = 0,
                StartedTimestamp = staleHeartbeat,
                LastHeartbeatUtc = staleHeartbeat
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await InvokeMaintenanceAsync(new JobMaintenanceOptions { WorkerInstanceStaleMinutes = 5 });

        await using var verify = await GetDbContextFactory().CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await verify.JobWorkerInstances.AnyAsync(i => i.Id == instanceId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Maintenance_PrunesStoppedWorkerInstance_EvenWhenHeartbeatIsRecent()
    {
        var instanceId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var db = await GetDbContextFactory().CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            db.JobWorkerInstances.Add(new JobWorkerInstance {
                Id = instanceId,
                WorkerType = "cs",
                MachineName = "test-host",
                ProcessId = 1,
                State = nameof(JobWorkerInstanceState.Stopped),
                InFlightCount = 0,
                StartedTimestamp = now,
                LastHeartbeatUtc = now
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await InvokeMaintenanceAsync(new JobMaintenanceOptions { WorkerInstanceStaleMinutes = 5 });

        await using var verify = await GetDbContextFactory().CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await verify.JobWorkerInstances.AnyAsync(i => i.Id == instanceId, TestContext.Current.CancellationToken));
    }

    private async Task<Guid> CreateWorkerInstanceAsync()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var now = DateTime.UtcNow;
        var req = new JobWorkerInstanceReq {
            WorkerType = "cs",
            MachineName = "test-host",
            ProcessId = Environment.ProcessId,
            State = JobWorkerInstanceState.Running,
            InFlightCount = 0,
            StartedTimestamp = now,
            LastHeartbeatUtc = now
        };

        var result = await createService.CreateAsync<JobWorkerInstanceReq, JobWorkerInstance, JobWorkerInstanceRes>(
            req, ctx => ctx.Entity.Id = Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return result.Data!.Id;
    }

    private async Task InvokeMaintenanceAsync(JobMaintenanceOptions options)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobMaintenanceService>>();
        var maintenance = new JobMaintenanceService(factory, logger, _fixture.FakePublisher, options);

        var method = typeof(JobMaintenanceService).GetMethod("RunMaintenanceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(maintenance, [TestContext.Current.CancellationToken])!;
    }

    private IDbContextFactory<JobContext> GetDbContextFactory()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
    }
}
