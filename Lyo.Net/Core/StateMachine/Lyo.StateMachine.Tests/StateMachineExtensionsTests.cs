using Microsoft.Extensions.DependencyInjection;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineExtensionsTests
{
    [Fact]
    public async Task AddStateMachine_RegistersDefinitionSingletonAndObservers()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddStateMachineObserver<JobState, JobTrigger, LoggingObserver>();
        services.AddStateMachine<JobState, JobTrigger, JobContext>("job", JobState.Queued, b => b.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running));
        await using var provider = services.BuildServiceProvider();
        var definition = provider.GetRequiredService<StateMachineDefinition<JobState, JobTrigger, JobContext>>();
        var result = await definition.FireAsync(new(), definition.CreateInitialSnapshot(), JobTrigger.Start, TestCt);
        Assert.Same(definition, provider.GetRequiredService<StateMachineDefinition<JobState, JobTrigger, JobContext>>());
        Assert.Equal("job", definition.Name);
        Assert.True(result.Succeeded);
        Assert.Equal(["Queued->Running"], log);
    }

    [Fact]
    public void AddStateMachine_NullConfigure_Throws()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddStateMachine<JobState, JobTrigger, JobContext>("job", JobState.Queued, null!));

    private sealed class LoggingObserver(List<string> log) : IStateMachineObserver<JobState, JobTrigger>
    {
        public void OnTransitioned(StateTransitioned<JobState, JobTrigger> transition) => log.Add($"{transition.Source}->{transition.Destination}");

        public void OnRejected(StateTransitionRejected<JobState, JobTrigger> rejection) { }
    }
}