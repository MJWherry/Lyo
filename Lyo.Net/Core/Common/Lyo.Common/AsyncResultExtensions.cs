using System.Diagnostics.CodeAnalysis;

namespace Lyo.Common;

/// <summary>Extension methods for async Result operations.</summary>
public static class AsyncResultExtensions
{
    /// <summary>Chains an async operation to a result. If the result is successful, executes the async function.</summary>
    [return: NotNull]
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Result<TIn> result, [NotNull] Func<TIn, Task<Result<TOut>>> next)
        => !result.IsSuccess ? Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata) : await next(result.Data!).ConfigureAwait(false);

    /// <summary>Chains an async operation to a task result. If the result is successful, executes the async function.</summary>
    [return: NotNull]
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Task<Result<TIn>> task, [NotNull] Func<TIn, Task<Result<TOut>>> next)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>Chains an async operation to a result. If the result is successful, executes the async function that returns a value.</summary>
    [return: NotNull]
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Result<TIn> result, [NotNull] Func<TIn, Task<TOut>> next)
    {
        if (!result.IsSuccess)
            return Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata);

        var value = await next(result.Data!).ConfigureAwait(false);
        return Result<TOut>.Success(value, result.Timestamp, result.Metadata);
    }

    /// <summary>Chains an async operation to a task result. If the result is successful, executes the async function that returns a value.</summary>
    [return: NotNull]
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Task<Result<TIn>> task, [NotNull] Func<TIn, Task<TOut>> next)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>Executes an async action if the result is successful.</summary>
    [return: NotNull]
    public static async Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, [NotNull] Func<T, System.Threading.Tasks.Task> action)
    {
        if (result.IsSuccess && result.Data != null)
            await action(result.Data).ConfigureAwait(false);

        return result;
    }

    /// <summary>Executes an async action if the result is successful.</summary>
    [return: NotNull]
    public static async Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> task, [NotNull] Func<T, System.Threading.Tasks.Task> action)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnSuccessAsync(action).ConfigureAwait(false);
    }

    /// <summary>Executes an async action if the result failed.</summary>
    [return: NotNull]
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, [NotNull] Func<IReadOnlyList<Error>, System.Threading.Tasks.Task> action)
    {
        if (!result.IsSuccess)
            await action(result.Errors ?? []).ConfigureAwait(false);

        return result;
    }

    /// <summary>Executes an async action if the result failed.</summary>
    [return: NotNull]
    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> task, [NotNull] Func<IReadOnlyList<Error>, System.Threading.Tasks.Task> action)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailureAsync(action).ConfigureAwait(false);
    }

    /// <summary>Executes multiple async operations and combines their results. All must succeed for the combined result to succeed.</summary>
    public static async Task<Result<IReadOnlyList<T>>> WhenAll<T>(params Task<Result<T>>[] tasks)
    {
        if (tasks == null || tasks.Length == 0)
            return Result<IReadOnlyList<T>>.Success([]);

        var results = await System.Threading.Tasks.Task.WhenAll(tasks).ConfigureAwait(false);
        var successes = results.Where(r => r.IsSuccess && r.Data != null).Select(r => r.Data!).ToList();
        var failures = results.Where(r => !r.IsSuccess).SelectMany(r => r.Errors ?? []).ToList();
        return failures.Count > 0 ? Result<IReadOnlyList<T>>.Failure(failures) : Result<IReadOnlyList<T>>.Success(successes);
    }

    /// <summary>Executes multiple async operations and combines their results. All must succeed for the combined result to succeed.</summary>
    public static async Task<Result<IReadOnlyList<T>>> WhenAll<T>(IEnumerable<Task<Result<T>>> tasks)
    {
        var taskArray = tasks?.ToArray() ?? [];
        return await WhenAll(taskArray).ConfigureAwait(false);
    }

    /// <summary>Executes async operations sequentially until one succeeds or all fail.</summary>
    public static async Task<Result<T>> WhenAny<T>(params Task<Result<T>>[] tasks)
    {
        if (tasks == null || tasks.Length == 0)
            return Result<T>.Failure("NO_TASKS", "No tasks provided");

        var results = await System.Threading.Tasks.Task.WhenAll(tasks).ConfigureAwait(false);
        var firstSuccess = results.FirstOrDefault(r => r.IsSuccess);
        if (firstSuccess != null)
            return firstSuccess;

        // All failed, combine all errors
        var allErrors = results.SelectMany(r => r.Errors ?? []).ToList();
        return Result<T>.Failure(allErrors);
    }

    /// <summary>Executes async operations sequentially until one succeeds or all fail.</summary>
    public static async Task<Result<T>> WhenAny<T>(IEnumerable<Task<Result<T>>> tasks)
    {
        var taskArray = tasks?.ToArray() ?? [];
        return await WhenAny(taskArray).ConfigureAwait(false);
    }

    /// <summary>Combines two async results into a tuple result. Both must succeed for the combined result to succeed.</summary>
    public static async Task<Result<(T1, T2)>> CombineAsync<T1, T2>(this Task<Result<T1>> task1, Task<Result<T2>> task2)
    {
        var r1 = await task1.ConfigureAwait(false);
        var r2 = await task2.ConfigureAwait(false);
        return r1.Combine(r2);
    }

    /// <summary>Combines three async results into a tuple result. All must succeed for the combined result to succeed.</summary>
    public static async Task<Result<(T1, T2, T3)>> CombineAsync<T1, T2, T3>(this Task<Result<T1>> task1, Task<Result<T2>> task2, Task<Result<T3>> task3)
    {
        var r1 = await task1.ConfigureAwait(false);
        var r2 = await task2.ConfigureAwait(false);
        var r3 = await task3.ConfigureAwait(false);
        return r1.Combine(r2, r3);
    }
}