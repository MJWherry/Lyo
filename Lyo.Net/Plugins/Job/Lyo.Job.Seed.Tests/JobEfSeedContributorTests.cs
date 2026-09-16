using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Seed;
using Lyo.Seed;

namespace Lyo.Job.Seed.Tests;

public sealed class JobEfSeedContributorTests
{
    [Fact]
    public async Task JobEfSeedContributor_Default_WritesDisabledDefinitions()
    {
        var transport = await SeedAsync(new JobEfSeedContributor(), 4);
        var definitions = transport.Items.OfType<JobDefinition>().ToList();
        var parameters = transport.Items.OfType<JobParameter>().ToList();
        var schedules = transport.Items.OfType<JobSchedule>().ToList();
        var runs = transport.Items.OfType<JobRun>().ToList();
        var workflow = Assert.Single(transport.Items.OfType<JobWorkflow>());
        var steps = transport.Items.OfType<JobWorkflowStep>().ToList();
        var workflowRun = Assert.Single(transport.Items.OfType<JobWorkflowRun>());
        var runSteps = transport.Items.OfType<JobWorkflowRunStep>().ToList();
        Assert.Equal(4, definitions.Count);
        Assert.Equal(4, parameters.Count);
        Assert.Equal(4, schedules.Count);
        Assert.Equal(4, transport.Items.OfType<JobScheduleParameter>().Count());
        Assert.Equal(4, transport.Items.OfType<JobParallelRestriction>().Count());
        Assert.Equal(4, runs.Count);
        Assert.Equal(4, transport.Items.OfType<JobRunLog>().Count());
        Assert.Equal(4, transport.Items.OfType<JobRunParameter>().Count());
        Assert.Equal(4, transport.Items.OfType<Lyo.Job.Postgres.Database.JobRunResult>().Count());
        Assert.Single(transport.Items.OfType<JobBlackoutCalendar>());
        Assert.Single(transport.Items.OfType<JobBlackoutWindow>());
        Assert.Single(transport.Items.OfType<JobWorkerInstance>());
        Assert.Single(transport.Items.OfType<JobFileUpload>());
        Assert.False(workflow.Enabled);
        Assert.Equal(4, steps.Count);
        Assert.Equal(4, runSteps.Count);
        Assert.Equal(workflow.Id, workflowRun.JobWorkflowId);
        Assert.All(definitions, definition => {
            Assert.False(definition.Enabled);
            Assert.Contains(runs, run => run.JobDefinitionId == definition.Id && run.State == JobState.Finished && run.ParentJobRunId == null && run.ReRanFromJobRunId == null);
            Assert.Contains(steps, step => step.JobDefinitionId == definition.Id && step.JobWorkflowId == workflow.Id && !step.Enabled);
        });
        Assert.All(schedules, schedule => Assert.False(schedule.Enabled));
        Assert.All(runSteps, step => Assert.Equal(workflowRun.Id, step.JobWorkflowRunId));
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
