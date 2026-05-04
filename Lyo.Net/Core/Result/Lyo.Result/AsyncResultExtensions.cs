namespace Lyo.Result;

/// <summary>Extension methods for composing and awaiting async Result operations.</summary>
public static class AsyncResultExtensions
{
    /// <summary>Chains an async operation to a result. Executes only on success.</summary>
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> next, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return !result.IsSuccess ? Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata) : await next(result.Data!).ConfigureAwait(false);
    }

    /// <summary>Chains an async operation to a task result. Executes only on success.</summary>
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, Task<Result<TOut>>> next, CancellationToken ct = default)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ThenAsync(next, ct).ConfigureAwait(false);
    }

    /// <summary>Chains an async operation that returns a plain value. Wraps the value in a success result.</summary>
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> next, CancellationToken ct = default)
    {
        if (!result.IsSuccess)
            return Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata);

        ct.ThrowIfCancellationRequested();
        var value = await next(result.Data!).ConfigureAwait(false);
        return Result<TOut>.Success(value, result.Timestamp, result.Metadata);
    }

    /// <summary>Chains an async operation that returns a plain value (task overload).</summary>
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, Task<TOut>> next, CancellationToken ct = default)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ThenAsync(next, ct).ConfigureAwait(false);
    }

    /// <summary>Chains an async operation that accepts a CancellationToken. Executes only on success.</summary>
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(
        this Result<TIn> result, Func<TIn, CancellationToken, Task<Result<TOut>>> next, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return !result.IsSuccess ? Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata) : await next(result.Data!, ct).ConfigureAwait(false);
    }

    /// <summary>Chains an async operation that accepts a CancellationToken (task overload).</summary>
    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, CancellationToken, Task<Result<TOut>>> next, CancellationToken ct = default)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ThenAsync(next, ct).ConfigureAwait(false);
    }

    /// <summary>Executes an async side-effect if the result is successful.</summary>
    public static async Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action, CancellationToken ct = default)
    {
        if (!result.IsSuccess || result.Data == null)
            return result;

        ct.ThrowIfCancellationRequested();
        await action(result.Data).ConfigureAwait(false);
        return result;
    }

    /// <summary>Executes an async side-effect if the result is successful (task overload).</summary>
    public static async Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> task, Func<T, Task> action, CancellationToken ct = default)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnSuccessAsync(action, ct).ConfigureAwait(false);
    }

    /// <summary>Executes an async side-effect if the result failed.</summary>
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<IReadOnlyList<Error>, Task> action, CancellationToken ct = default)
    {
        if (result.IsSuccess)
            return result;

        ct.ThrowIfCancellationRequested();
        await action(result.Errors ?? []).ConfigureAwait(false);
        return result;
    }

    /// <summary>Executes an async side-effect if the result failed (task overload).</summary>
    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> task, Func<IReadOnlyList<Error>, Task> action, CancellationToken ct = default)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailureAsync(action, ct).ConfigureAwait(false);
    }

    /// <summary>Awaits all tasks in parallel. Succeeds only if every result succeeds; otherwise returns all errors.</summary>
    public static async Task<Result<IReadOnlyList<T>>> WhenAll<T>(params Task<Result<T>>[] tasks)
    {
        if (tasks.Length == 0)
            return Result<IReadOnlyList<T>>.Success([]);

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var successes = results.Where(r => r.IsSuccess && r.Data != null).Select(r => r.Data!).ToList();
        var failures = results.Where(r => !r.IsSuccess).SelectMany(r => r.Errors ?? []).ToList();
        return failures.Count > 0 ? Result<IReadOnlyList<T>>.Failure(failures) : Result<IReadOnlyList<T>>.Success(successes);
    }

    /// <summary>Awaits all tasks in parallel (enumerable overload).</summary>
    public static async Task<Result<IReadOnlyList<T>>> WhenAll<T>(IEnumerable<Task<Result<T>>> tasks) => await WhenAll(tasks.ToArray()).ConfigureAwait(false);

    extension<T1>(Task<Result<T1>> task1)
    {
        /// <summary>Combines two async results into a tuple. Both must succeed.</summary>
        public async Task<Result<(T1, T2)>> CombineAsync<T2>(Task<Result<T2>> task2)
        {
            var r1 = await task1.ConfigureAwait(false);
            var r2 = await task2.ConfigureAwait(false);
            return r1.Combine(r2);
        }

        /// <summary>Combines three async results into a tuple. All must succeed.</summary>
        public async Task<Result<(T1, T2, T3)>> CombineAsync<T2, T3>(Task<Result<T2>> task2, Task<Result<T3>> task3)
        {
            var r1 = await task1.ConfigureAwait(false);
            var r2 = await task2.ConfigureAwait(false);
            var r3 = await task3.ConfigureAwait(false);
            return r1.Combine(r2, r3);
        }
    }

    /// <summary>
    /// Runs all tasks in parallel and returns the first successful result. If none succeed, returns all collected errors. Renamed from the previously misnamed <c>WhenAny</c>
    /// which ran all tasks via <c>Task.WhenAll</c>.
    /// </summary>
    public static async Task<Result<T>> FirstSuccess<T>(params Task<Result<T>>[] tasks)
    {
        if (tasks == null || tasks.Length == 0)
            return Result<T>.Failure("NO_TASKS", "No tasks provided");

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var firstSuccess = results.FirstOrDefault(r => r.IsSuccess);
        if (firstSuccess != null)
            return firstSuccess;

        var allErrors = results.SelectMany(r => r.Errors ?? []).ToList();
        return Result<T>.Failure(allErrors);
    }

    /// <summary>Runs all tasks in parallel and returns the first successful result (enumerable overload).</summary>
    public static async Task<Result<T>> FirstSuccess<T>(IEnumerable<Task<Result<T>>> tasks) => await FirstSuccess(tasks?.ToArray() ?? []).ConfigureAwait(false);

    /// <summary>Executes an async side-effect on a task result without changing it (success or failure).</summary>
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> task, Func<T, Task> action, CancellationToken ct = default)
    {
        var result = await task.ConfigureAwait(false);
        return await result.TapAsync(action).ConfigureAwait(false);
    }

    /// <summary>Transforms a task result's success value using an async mapper.</summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, Task<TOut>> mapper, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = await task.ConfigureAwait(false);
        return await result.MapAsync(mapper).ConfigureAwait(false);
    }
}