using System.Text;
using Lyo.Api;
using Lyo.Cache;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Regression tests for the encrypted-parameter round trip: values are stored as ciphertext, masked on public API responses, decrypted on the worker-trusted
/// <c>StartedJobRun</c> path, and survive rerun cloning (the old mapper-based clone persisted the <c>***</c> mask as the real value).
/// </summary>
[Trait("Category", "Integration")]
public class JobParameterEncryptionRoundTripTests
{
    private const string Plaintext = "s3cret-connection-string";
    private const string ParameterKey = "Secret";

    private readonly JobPostgresFixture _fixture;

    public JobParameterEncryptionRoundTripTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task EncryptedParameter_IsStoredAsCiphertext_AndMaskedOnCreateResponse()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var definitionId = await CreateEncryptedDefinitionAsync(sp);

        var created = await jobService.CreateJobRun(BuildRunRequest(definitionId), TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        var responseParam = Assert.Single(created.Data!.JobRunParameters!, p => p.Key == ParameterKey);
        Assert.Equal("***", responseParam.Value);

        await using var db = await CreateDbContextAsync(sp);
        var stored = await db.JobRunParameters.AsNoTracking()
            .SingleAsync(p => p.JobRunId == created.Data.Id && p.Key == ParameterKey, TestContext.Current.CancellationToken);
        Assert.Null(stored.Value);
        Assert.NotNull(stored.EncryptedValue);
        Assert.NotEqual(Plaintext, Encoding.UTF8.GetString(stored.EncryptedValue!));
    }

    [Fact]
    public async Task StartedJobRun_ReturnsDecryptedValueToWorker()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var definitionId = await CreateEncryptedDefinitionAsync(sp);
        var created = await jobService.CreateJobRun(BuildRunRequest(definitionId), TestContext.Current.CancellationToken);
        Assert.True(created.IsSuccess);

        var (started, error) = await jobService.StartedJobRun(created.Data!.Id);

        // The worker-trusted path must hand real plaintext to the worker — before the fix it received the literal "***".
        Assert.Null(error);
        var startedParam = Assert.Single(started!.JobRunParameters!, p => p.Key == ParameterKey);
        Assert.Equal(Plaintext, startedParam.Value);
    }

    [Fact]
    public async Task RerunJob_PreservesDecryptableEncryptedValue()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var encryption = scope.ServiceProvider.GetRequiredService<IJobParameterEncryptionService>();
        var definitionId = await CreateEncryptedDefinitionAsync(sp);
        var created = await jobService.CreateJobRun(BuildRunRequest(definitionId), TestContext.Current.CancellationToken);
        Assert.True(created.IsSuccess);
        await FinishRunAsync(sp, created.Data!.Id);

        var rerun = await jobService.RerunJob(created.Data.Id);

        Assert.NotNull(rerun);
        Assert.True(rerun!.IsSuccess, rerun.Error?.Detail ?? "rerun failed");

        await using var db = await CreateDbContextAsync(sp);
        var cloned = await db.JobRunParameters.AsNoTracking()
            .SingleAsync(p => p.JobRunId == rerun.Data!.Id && p.Key == ParameterKey, TestContext.Current.CancellationToken);

        // The clone must decrypt back to the original secret — the old response-based clone stored "***" (or double-encrypted bytes).
        Assert.Null(cloned.Value);
        Assert.Equal(Plaintext, encryption.DecryptValue(cloned.EncryptedValue));
    }

    private static JobRunReq BuildRunRequest(Guid definitionId)
        => new(definitionId, "test-user", false) {
            JobRunParameters = { new JobRunParameterReq { Key = ParameterKey, Type = JobParameterType.String, Value = Plaintext, Enabled = true } }
        };

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _fixture.ConnectionString });
        services.AddSingleton<Models.Events.IJobEventPublisher>(_ => new FakeJobEventPublisher());
        services.AddSingleton<IJobParameterEncryptionService, FakeParameterEncryptionService>();
        services.AddScoped<JobService>();
        return services.BuildServiceProvider();
    }

    private async Task<Guid> CreateEncryptedDefinitionAsync(ServiceProvider sp)
    {
        var definitionId = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync(sp);
        db.JobDefinitions.Add(new JobDefinition {
            Id = definitionId,
            Name = $"Encrypted-{definitionId:N}"[..32],
            Type = "Test",
            WorkerType = "cs",
            Enabled = true,
            CreatedTimestamp = DateTime.UtcNow
        });
        db.JobParameters.Add(new JobParameter {
            Id = LyoGuid.CreateCombPostgres(),
            JobDefinitionId = definitionId,
            Key = ParameterKey,
            Type = nameof(JobParameterType.String),
            Value = null,
            EncryptedValue = [], // non-null marker => parameter uses encrypted storage
            CreatedTimestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return definitionId;
    }

    private static async Task FinishRunAsync(ServiceProvider sp, Guid runId)
    {
        await using var db = await CreateDbContextAsync(sp);
        var run = await db.JobRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
        run.State = JobState.Finished;
        run.Result = Models.Enums.JobRunResult.Success;
        run.FinishedTimestamp = DateTime.UtcNow;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<JobContext> CreateDbContextAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Deterministic stand-in for the keyed encryption service: ciphertext is <c>enc:</c> + plaintext bytes.</summary>
    private sealed class FakeParameterEncryptionService : IJobParameterEncryptionService
    {
        private const string Prefix = "enc:";

        public bool IsEncryptionEnabled => true;

        public bool UsesEncryptedStorage(byte[]? encryptedValueMarker) => encryptedValueMarker is not null;

        public void EncryptParameterValue(ref string? value, ref byte[]? encryptedValue)
        {
            var plaintext = value;
            if (string.IsNullOrEmpty(plaintext) && encryptedValue is { Length: > 0 })
                plaintext = Encoding.UTF8.GetString(encryptedValue);

            if (string.IsNullOrEmpty(plaintext))
                return;

            encryptedValue = Encoding.UTF8.GetBytes(Prefix + plaintext);
            value = null;
        }

        public string? DecryptValue(byte[]? encryptedValue)
        {
            if (encryptedValue is null or { Length: 0 })
                return null;

            var text = Encoding.UTF8.GetString(encryptedValue);
            return text.StartsWith(Prefix, StringComparison.Ordinal) ? text[Prefix.Length..] : null;
        }

        public string? MaskValue(string? value, byte[]? encryptedValueMarker) => UsesEncryptedStorage(encryptedValueMarker) ? "***" : value;
    }
}
