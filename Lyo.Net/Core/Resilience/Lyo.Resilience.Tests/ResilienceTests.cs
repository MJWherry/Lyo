using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Resilience.Tests;

public sealed class ResilienceTests
{
    [Fact]
    public async Task Executor_can_execute_action()
    {
        var services = new ServiceCollection();
        services.AddResilientExecutor();
        using var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilientExecutor>();
        var executed = false;
        await executor.ExecuteAsync(
            _ => {
                executed = true;
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(executed);
    }
}