using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

namespace Lyo.Resilience.Tests;

public sealed class ResilienceTests
{
    [Fact]
    public async Task Executor_Can_Execute_Action()
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
            }, TestContext.Current.CancellationToken);

        Assert.True(executed);
    }

    [Fact]
    public async Task Pipeline_Retries_Transient_Http_Exception()
    {
        using var sp = BuildProviderWithFastRetryPipeline();
        var pipeline = sp.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline("test-pipeline");
        var attempts = 0;
        await pipeline.ExecuteAsync(
            _ => {
                attempts++;
                if (attempts < 3)
                    throw new ServiceUnavailableException();

                return ValueTask.CompletedTask;
            }, TestContext.Current.CancellationToken);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Pipeline_Does_Not_Retry_Non_Transient_Http_Exception()
    {
        using var sp = BuildProviderWithFastRetryPipeline();
        var pipeline = sp.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline("test-pipeline");
        var attempts = 0;
        await Assert.ThrowsAsync<NotFoundException>(async () => await pipeline.ExecuteAsync(
            ValueTask (_) => {
                attempts++;
                throw new NotFoundException("Widget");
            }, TestContext.Current.CancellationToken));

        Assert.Equal(1, attempts);
    }

    private static ServiceProvider BuildProviderWithFastRetryPipeline()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["Lyo:ResiliencePipelines:test-pipeline:Retry:MaxRetryAttempts"] = "3", ["Lyo:ResiliencePipelines:test-pipeline:Retry:Delay"] = "00:00:00.001"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddLyoResiliencePipelinesFromConfiguration(configuration);
        return services.BuildServiceProvider();
    }
}