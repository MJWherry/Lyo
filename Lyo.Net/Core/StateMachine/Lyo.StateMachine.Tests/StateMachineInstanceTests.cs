namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineInstanceTests
{
    [Fact]
    public async Task FireAsync_SavesSnapshotWithExpectedVersion()
    {
        var definition = TestMachines.Job().Build();
        var store = new InMemoryStateStore<JobState>();
        var instance = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, "job-1");
        Assert.Equal(JobState.Queued, (await instance.GetSnapshotAsync(TestCt)).State);
        var started = await instance.FireAsync(new(), JobTrigger.Start, TestCt);
        var done = await instance.FireAsync(new(), JobTrigger.Complete, TestCt);
        Assert.True(started.Succeeded);
        Assert.True(done.Succeeded);
        var saved = await store.LoadAsync("job", "job-1", TestCt);
        Assert.NotNull(saved);
        Assert.Equal(JobState.Done, saved.State);
        Assert.Equal(2, saved.Version);
        var history = store.GetHistory("job", "job-1");
        Assert.Equal(
            [(JobState.Queued, JobState.Running, "Start", 1L), (JobState.Running, JobState.Done, "Complete", 2L)], history.Select(h => (h.From, h.To, h.Trigger, h.Version)));
    }

    [Fact]
    public async Task FireAsync_Rejected_DoesNotSave()
    {
        var definition = TestMachines.Job().Build();
        var store = new InMemoryStateStore<JobState>();
        var instance = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, "job-1");
        var result = await instance.FireAsync(new(), JobTrigger.Complete, TestCt);
        Assert.Equal(TransitionRejection.NoTransition, result.Rejection);
        Assert.Null(await store.LoadAsync("job", "job-1", TestCt));
    }

    [Fact]
    public async Task FireAsync_StaleVersion_ReturnsConcurrencyConflict()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
        builder.Configure(JobState.Running).OnEntryAsync((t, _) => t.Context.DuringTransition?.Invoke() ?? Task.CompletedTask);
        var definition = builder.Build();
        var store = new InMemoryStateStore<JobState>();
        var first = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, "job-1");
        var second = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, "job-1");
        TransitionResult<JobState>? secondResult = null;
        var firstResult = await first.FireAsync(
            new() { DuringTransition = async () => secondResult = await second.FireAsync(new(), JobTrigger.Start, TestCt) }, JobTrigger.Start, TestCt);

        Assert.True(secondResult!.Succeeded);
        Assert.Equal(TransitionRejection.ConcurrencyConflict, firstResult.Rejection);
        var saved = await store.LoadAsync("job", "job-1", TestCt);
        Assert.Equal(1, saved!.Version);
        Assert.Single(store.GetHistory("job", "job-1"));
    }

    [Fact]
    public async Task FireAsync_SameInstanceFromHook_ThrowsReentrancy()
    {
        StateMachineInstance<JobState, JobTrigger, JobContext>? instance = null;
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
        builder.Configure(JobState.Running).Permit(JobTrigger.Complete, JobState.Done).OnEntryAsync((t, _) => t.Context.DuringTransition!());
        instance = new(builder.Build(), new InMemoryStateStore<JobState>(), "job-1");
        await Assert.ThrowsAsync<StateMachineReentrancyException>(() => instance.FireAsync(
            new() { DuringTransition = () => instance.FireAsync(new(), JobTrigger.Complete, TestCt) }, JobTrigger.Start, TestCt));
    }

    [Fact]
    public async Task FireAsync_ConflictWithRetries_ReloadsAndSucceeds()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
        builder.Configure(JobState.Running).PermitReentry(JobTrigger.Start).OnEntryAsync((t, _) => t.Context.DuringTransition?.Invoke() ?? Task.CompletedTask);
        var definition = builder.Build();
        var store = new InMemoryStateStore<JobState>();
        var first = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, "job-1", new() { MaxConflictRetries = 1 });
        var second = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, "job-1");
        var raced = false;
        var result = await first.FireAsync(
            new() {
                DuringTransition = async () => {
                    if (raced)
                        return;

                    raced = true;
                    await second.FireAsync(new(), JobTrigger.Start, TestCt);
                }
            }, JobTrigger.Start, TestCt);

        Assert.True(result.Succeeded);
        Assert.Equal(TransitionKind.Reentry, result.Kind);
        Assert.Equal(2, (await store.LoadAsync("job", "job-1", TestCt))!.Version);
    }

    [Fact]
    public async Task UpdateDataAsync_BumpsVersionAndPersistsJson()
    {
        var store = new InMemoryStateStore<JobState>();
        var instance = new StateMachineInstance<JobState, JobTrigger, JobContext>(TestMachines.Job().Build(), store, "job-1");
        Assert.True(await instance.UpdateDataAsync<Progress>(p => new((p?.Percent ?? 0) + 10), TestCt));
        Assert.True(await instance.UpdateDataAsync<Progress>(p => new((p?.Percent ?? 0) + 10), TestCt));
        Assert.Equal(new(20), await instance.GetDataAsync<Progress>(TestCt));
        var saved = await store.LoadAsync("job", "job-1", TestCt);
        Assert.Equal(2, saved!.Version);
        Assert.Equal(JobState.Queued, saved.State);
        Assert.Empty(store.GetHistory("job", "job-1"));
    }

    [Fact]
    public async Task FireAsync_CarriesDataForward()
    {
        var store = new InMemoryStateStore<JobState>();
        var instance = new StateMachineInstance<JobState, JobTrigger, JobContext>(TestMachines.Job().Build(), store, "job-1");
        await instance.UpdateDataAsync<Progress>(_ => new(50), TestCt);
        await instance.FireAsync(new(), JobTrigger.Start, TestCt);
        var saved = await store.LoadAsync("job", "job-1", TestCt);
        Assert.Equal((JobState.Running, 2L), (saved!.State, saved.Version));
        Assert.Equal(new(50), saved.GetData<JobState, Progress>());
    }

    [Fact]
    public async Task FireDueAsync_Due_FiresTimerTrigger()
    {
        var t0 = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued).UseClock(() => t0);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
        builder.Configure(JobState.Running).Permit(JobTrigger.Complete, JobState.Done).After(TimeSpan.FromSeconds(10), JobTrigger.Complete);
        var instance = new StateMachineInstance<JobState, JobTrigger, JobContext>(builder.Build(), new InMemoryStateStore<JobState>(), "job-1");
        await instance.FireAsync(new(), JobTrigger.Start, TestCt);
        var early = await instance.FireDueAsync(new(), t0.AddSeconds(5), TestCt);
        var due = await instance.FireDueAsync(new(), t0.AddSeconds(10), TestCt);
        Assert.Null(early);
        Assert.Equal(JobState.Done, due!.To);
    }

    public sealed record Progress(int Percent);
}