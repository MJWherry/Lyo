using Bogus;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Parameters;
using Lyo.Schedule.Models;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Job.Seed;

/// <summary>
/// EF seed of disabled job definitions, each with a parameter, a disabled schedule, a schedule parameter, a parallel restriction, and a finished run.
/// Also writes one blackout calendar, one stopped worker, one file upload, and one disabled workflow. Schema <c>job</c>.
/// </summary>
public sealed class JobEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Job";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresJobOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<JobContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new JobContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var ids = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var runIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var stepIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var calendarId = LyoGuid.CreateV7();
        var workflowId = LyoGuid.CreateV7();
        var workflowRunId = LyoGuid.CreateV7();
        var definitions = graph.Entity(options.Count, i => new JobDefinition {
            Id = ids[i],
            Name = Truncate(faker.Hacker.Phrase(), 200),
            Description = Truncate(faker.Lorem.Sentence(), 200),
            Type = "import",
            WorkerType = "importer",
            Enabled = false,
            RetryBackoffType = nameof(JobRetryBackoffType.Linear),
            CreatedTimestamp = now
        });
        definitions.ForEach(_ => 1, (definition, _) => new JobParameter {
            Id = LyoGuid.CreateV7(),
            JobDefinitionId = definition.Id,
            Key = "region",
            Type = "System.String",
            Value = faker.Address.City(),
            DefaultKind = nameof(LyoParameterDefaultKind.Literal),
            CreatedTimestamp = now
        });
        definitions.ForEach(_ => 1, (definition, _) => new JobSchedule {
            Id = LyoGuid.CreateV7(),
            JobDefinitionId = definition.Id,
            Type = nameof(ScheduleType.SetTimes),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            DayFlags = nameof(DayFlags.EveryDay),
            Times = ["09:00"],
            MisfirePolicy = nameof(JobMisfirePolicy.Skip),
            Enabled = false,
            CreatedTimestamp = now
        }).ForEach(_ => 1, (schedule, _) => new JobScheduleParameter {
            Id = LyoGuid.CreateV7(),
            JobScheduleId = schedule.Id,
            Key = "batch",
            Type = "System.String",
            Value = faker.Random.Int(1, 40).ToString(),
            Enabled = false,
            CreatedTimestamp = now
        });
        definitions.ForEach(_ => 1, (definition, _) => {
            var index = Array.IndexOf(ids, definition.Id);
            return new JobParallelRestriction {
                Id = LyoGuid.CreateV7(),
                BaseJobDefinitionId = definition.Id,
                OtherJobDefinitionId = ids[(index + 1) % ids.Length],
                Description = Truncate(faker.Hacker.Phrase(), 200),
                Enabled = false,
                CreatedTimestamp = now
            };
        });
        var runs = definitions.ForEach(_ => 1, (definition, _) => {
            var index = Array.IndexOf(ids, definition.Id);
            return new JobRun {
                Id = runIds[index],
                JobDefinitionId = definition.Id,
                CreatedBy = faker.Name.FirstName(),
                State = JobState.Finished,
                Result = Lyo.Job.Models.Enums.JobRunResult.Success,
                CreatedTimestamp = now,
                StartedTimestamp = now,
                FinishedTimestamp = now
            };
        });
        runs.ForEach(_ => 1, (run, _) => new JobRunLog {
            Id = LyoGuid.CreateV7(),
            JobRunId = run.Id,
            Level = "Information",
            Message = Truncate(faker.Hacker.Phrase(), 200),
            Timestamp = now
        });
        runs.ForEach(_ => 1, (run, _) => new JobRunParameter {
            Id = LyoGuid.CreateV7(),
            JobRunId = run.Id,
            Key = "region",
            Type = "System.String",
            Value = faker.Address.City()
        });
        runs.ForEach(_ => 1, (run, _) => new Lyo.Job.Postgres.Database.JobRunResult {
            Id = LyoGuid.CreateV7(),
            JobRunId = run.Id,
            Key = "summary",
            Type = "System.String",
            Value = "ok"
        });
        graph.Entity(1, _ => new JobBlackoutCalendar {
            Id = calendarId,
            Name = "Holiday freeze",
            Enabled = false,
            CreatedTimestamp = now
        }).ForEach(_ => 1, (_, _) => new JobBlackoutWindow {
            Id = LyoGuid.CreateV7(),
            JobBlackoutCalendarId = calendarId,
            Name = "Overnight hold",
            DayFlags = nameof(DayFlags.EveryDay),
            StartTime = "00:00",
            EndTime = "01:00",
            Policy = nameof(JobBlackoutPolicy.Skip),
            Enabled = false,
            CreatedTimestamp = now
        });
        graph.Entity(1, _ => new JobWorkerInstance {
            Id = LyoGuid.CreateV7(),
            WorkerType = "importer",
            MachineName = $"{faker.Internet.DomainWord()}-worker",
            ProcessId = 1,
            State = nameof(JobWorkerInstanceState.Stopped),
            StartedTimestamp = now,
            LastHeartbeatUtc = now,
            CreatedTimestamp = now
        });
        graph.Entity(1, _ => new JobFileUpload {
            Id = LyoGuid.CreateV7(),
            UploadTimestamp = now,
            CreatedTimestamp = now,
            OriginalFilename = "intake.csv",
            OriginalSize = 4,
            OriginalHash = [1, 2, 3, 4],
            SourceDirectory = "/imports",
            SourceFilename = "intake.csv",
            SourceSize = 4,
            SourceHash = [1, 2, 3, 4],
            EncryptedDataEncryptionKey = [1],
            DataEncryptionKeyVersion = 1
        });
        graph.Entity(1, _ => new JobWorkflow {
            Id = workflowId,
            Name = "Nightly import",
            Description = "Disabled workflow kept for history.",
            Enabled = false,
            CreatedTimestamp = now
        });
        graph.Entity(options.Count, i => new JobWorkflowStep {
            Id = stepIds[i],
            JobWorkflowId = workflowId,
            JobDefinitionId = ids[i],
            StepName = $"Step {i}",
            StepOrder = i,
            FailurePolicy = nameof(JobWorkflowFailurePolicy.Stop),
            Enabled = false,
            CreatedTimestamp = now
        });
        graph.Entity(1, _ => new JobWorkflowRun {
            Id = workflowRunId,
            JobWorkflowId = workflowId,
            State = JobWorkflowRunState.Finished,
            CreatedTimestamp = now,
            StartedTimestamp = now,
            FinishedTimestamp = now
        });
        graph.Entity(options.Count, i => new JobWorkflowRunStep {
            Id = LyoGuid.CreateV7(),
            JobWorkflowRunId = workflowRunId,
            JobWorkflowStepId = stepIds[i],
            JobRunId = runIds[i],
            State = JobWorkflowStepState.Finished,
            CreatedTimestamp = now
        });
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<JobContext> ef)
            throw new SeedException("Job OnClear requires EfSeedTransport<JobContext>.");

        var db = ef.Context;
        await db.JobWorkflowRunSteps.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobRunLogs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobRunParameters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobRunResults.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobRuns.ExecuteUpdateAsync(
                s => s.SetProperty(r => r.ParentJobRunId, (Guid?)null).SetProperty(r => r.ReRanFromJobRunId, (Guid?)null),
                ct)
            .ConfigureAwait(false);
        await db.JobRuns.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobWorkflowRuns.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobWorkflowSteps.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobScheduleParameters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobSchedules.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobWorkflows.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobParameters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobParallelRestrictions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.JobDefinitions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
