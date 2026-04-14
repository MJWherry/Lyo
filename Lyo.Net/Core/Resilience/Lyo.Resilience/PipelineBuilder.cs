using System.Net.Sockets;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Lyo.Resilience;

internal static class PipelineBuilder
{
    public static void Configure(ResiliencePipelineBuilder builder, IConfigurationSection pipelineSection, IServiceProvider serviceProvider, ILogger? logger, string pipelineName)
    {
        var retrySection = pipelineSection.GetSection("Retry");
        if (retrySection.Exists()) {
            var retryOptions = new RetryStrategyOptions();
            retrySection.Bind(retryOptions);
            retryOptions.ShouldHandle = new PredicateBuilder().Handle<SocketException>()
                .Handle<TimeoutException>()
                .Handle<HttpRequestException>()
                .Handle<IOException>()
                .Handle<RetryableResultException>();

            ConfigureRetry(retryOptions, logger, GetMetrics(serviceProvider), pipelineName);
            builder.AddRetry(retryOptions);
        }

        var timeoutSection = pipelineSection.GetSection("Timeout");
        if (timeoutSection.Exists()) {
            var timeoutOptions = new TimeoutStrategyOptions();
            timeoutSection.Bind(timeoutOptions);
            if (timeoutOptions.Timeout == TimeSpan.Zero)
                timeoutOptions.Timeout = TimeSpan.FromSeconds(30);

            ConfigureTimeout(timeoutOptions, logger, GetMetrics(serviceProvider), pipelineName);
            builder.AddTimeout(timeoutOptions);
        }

        var cbSection = pipelineSection.GetSection("CircuitBreaker");
        if (cbSection.Exists()) {
            var cbOptions = new CircuitBreakerStrategyOptions();
            cbSection.Bind(cbOptions);
            ConfigureCircuitBreaker(cbOptions, logger, GetMetrics(serviceProvider), pipelineName);
            builder.AddCircuitBreaker(cbOptions);
        }
    }

    private static IMetrics GetMetrics(IServiceProvider serviceProvider)
    {
        var metrics = serviceProvider?.GetService(typeof(IMetrics)) as IMetrics;
        return metrics ?? NullMetrics.Instance;
    }

    private static void ConfigureRetry(RetryStrategyOptions options, ILogger? logger, IMetrics metrics, string pipelineName)
    {
        var tags = new[] { (Constants.Metrics.PipelineTag, pipelineName) };
        var originalOnRetry = options.OnRetry;
        options.OnRetry = args => {
            metrics.IncrementCounter(Constants.Metrics.Retry, tags: tags);
            if (args.Outcome.Exception != null)
                metrics.RecordError(Constants.Metrics.Retry, args.Outcome.Exception, tags);

            logger?.LogWarning(args.Outcome.Exception, "Resilience: Retry attempt {Attempt} after {Delay}ms", args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
            return originalOnRetry != null ? originalOnRetry(args) : default;
        };
    }

    private static void ConfigureTimeout(TimeoutStrategyOptions options, ILogger? logger, IMetrics metrics, string pipelineName)
    {
        var tags = new[] { (Constants.Metrics.PipelineTag, pipelineName) };
        var originalOnTimeout = options.OnTimeout;
        options.OnTimeout = args => {
            metrics.IncrementCounter(Constants.Metrics.Timeout, tags: tags);
            logger?.LogWarning("Resilience: Operation timed out after {Timeout}ms", args.Timeout.TotalMilliseconds);
            return originalOnTimeout != null ? originalOnTimeout(args) : default;
        };
    }

    private static void ConfigureCircuitBreaker(CircuitBreakerStrategyOptions options, ILogger? logger, IMetrics metrics, string pipelineName)
    {
        var tags = new[] { (Constants.Metrics.PipelineTag, pipelineName) };
        var originalOnOpened = options.OnOpened;
        options.OnOpened = args => {
            metrics.IncrementCounter(Constants.Metrics.CircuitBreakerOpened, tags: tags);
            if (args.Outcome.Exception != null)
                metrics.RecordError(Constants.Metrics.CircuitBreakerOpened, args.Outcome.Exception, tags);

            logger?.LogWarning(args.Outcome.Exception, "Resilience: Circuit breaker opened");
            return originalOnOpened != null ? originalOnOpened(args) : default;
        };

        var originalOnClosed = options.OnClosed;
        options.OnClosed = args => {
            metrics.IncrementCounter(Constants.Metrics.CircuitBreakerClosed, tags: tags);
            logger?.LogInformation("Resilience: Circuit breaker closed");
            return originalOnClosed != null ? originalOnClosed(args) : default;
        };

        var originalOnHalfOpened = options.OnHalfOpened;
        options.OnHalfOpened = args => {
            metrics.IncrementCounter(Constants.Metrics.CircuitBreakerHalfOpened, tags: tags);
            logger?.LogInformation("Resilience: Circuit breaker half-opened, testing");
            return originalOnHalfOpened != null ? originalOnHalfOpened(args) : default;
        };
    }

    /// <summary>Builds the default basic pipeline (retry + timeout) with RetryableResultException support.</summary>
    public static void ConfigureDefault(ResiliencePipelineBuilder builder, IServiceProvider serviceProvider, string pipelineName)
    {
        var retryOptions = new RetryStrategyOptions {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<SocketException>()
                .Handle<TimeoutException>()
                .Handle<HttpRequestException>()
                .Handle<IOException>()
                .Handle<RetryableResultException>()
        };

        var loggerFactory = serviceProvider?.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        var logger = loggerFactory?.CreateLogger($"Lyo.Resilience.{pipelineName}");
        var metrics = GetMetrics(serviceProvider!);
        ConfigureRetry(retryOptions, logger, metrics, pipelineName);
        builder.AddRetry(retryOptions);
        var timeoutOptions = new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(30) };
        ConfigureTimeout(timeoutOptions, logger, metrics, pipelineName);
        builder.AddTimeout(timeoutOptions);
    }
}