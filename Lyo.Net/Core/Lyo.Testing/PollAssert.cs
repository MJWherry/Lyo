using Polly;
using Polly.Timeout;
using Xunit;

namespace Lyo.Testing;

public static class PollAssert
{
    public static async Task ThatAsync(Func<bool> condition, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var retryPolicy = Policy.HandleResult(false).WaitAndRetryAsync((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);
        var timeoutPolicy = Policy.TimeoutAsync<bool>(timeout);
        var combinedPolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy);
        var result = await combinedPolicy.ExecuteAsync(() => Task.FromResult(condition()));
        Assert.True(result, "Condition was not met within the timeout period");
    }

    public static async Task ThatAsync<T>(Func<T> valueProvider, T expectedValue, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var retryPolicy = Policy.HandleResult<T>(value => !Equals(value, expectedValue))
            .WaitAndRetryAsync((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);

        var timeoutPolicy = Policy.TimeoutAsync<T>(timeout);
        var combinedPolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy);
        var result = await combinedPolicy.ExecuteAsync(() => Task.FromResult(valueProvider()));
        Assert.Equal(expectedValue, result);
    }

    public static async Task ThatAsync<T>(Func<T> valueProvider, Func<T, bool> predicate, TimeSpan timeout, string? failureMessage = null, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var retryPolicy = Policy.HandleResult<T>(value => !predicate(value))
            .WaitAndRetryAsync((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);

        var timeoutPolicy = Policy.TimeoutAsync<T>(timeout);
        var combinedPolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy);
        try {
            var result = await combinedPolicy.ExecuteAsync(() => Task.FromResult(valueProvider()));
            Assert.True(predicate(result), failureMessage ?? $"Predicate was not satisfied. Final value: {result}");
        }
        catch (TimeoutRejectedException) {
            var finalValue = valueProvider();
            Assert.Fail(failureMessage ?? $"Polling timed out. Final value: {finalValue}");
        }
    }

    public static async Task NoExceptionAsync(Func<Task> action, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);
        var timeoutPolicy = Policy.TimeoutAsync(timeout);
        var combinedPolicy = Policy.WrapAsync(timeoutPolicy, policy);
        await combinedPolicy.ExecuteAsync(action);
    }

    // Synchronous versions

    public static void That(Func<bool> condition, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var retryPolicy = Policy.HandleResult(false).WaitAndRetry((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);
        var timeoutPolicy = Policy.Timeout<bool>(timeout);
        var combinedPolicy = Policy.Wrap(timeoutPolicy, retryPolicy);
        var result = combinedPolicy.Execute(condition);
        Assert.True(result, "Condition was not met within the timeout period");
    }

    public static void That<T>(Func<T> valueProvider, T expectedValue, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var retryPolicy = Policy.HandleResult<T>(value => !Equals(value, expectedValue))
            .WaitAndRetry((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);

        var timeoutPolicy = Policy.Timeout<T>(timeout);
        var combinedPolicy = Policy.Wrap(timeoutPolicy, retryPolicy);
        var result = combinedPolicy.Execute(valueProvider);
        Assert.Equal(expectedValue, result);
    }

    public static void That<T>(Func<T> valueProvider, Func<T, bool> predicate, TimeSpan timeout, string? failureMessage = null, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var retryPolicy = Policy.HandleResult<T>(value => !predicate(value))
            .WaitAndRetry((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);

        var timeoutPolicy = Policy.Timeout<T>(timeout);
        var combinedPolicy = Policy.Wrap(timeoutPolicy, retryPolicy);
        try {
            var result = combinedPolicy.Execute(valueProvider);
            Assert.True(predicate(result), failureMessage ?? $"Predicate was not satisfied. Final value: {result}");
        }
        catch (TimeoutRejectedException) {
            var finalValue = valueProvider();
            Assert.Fail(failureMessage ?? $"Polling timed out. Final value: {finalValue}");
        }
    }

    public static void NoException(Action action, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var policy = Policy.Handle<Exception>().WaitAndRetry((int)(timeout.TotalMilliseconds / pollInterval.Value.TotalMilliseconds), _ => pollInterval.Value);
        var timeoutPolicy = Policy.Timeout(timeout);
        var combinedPolicy = Policy.Wrap(timeoutPolicy, policy);
        combinedPolicy.Execute(action);
    }
}