namespace Lyo.Resilience;

/// <summary>Runs actions through a named resilience pipeline.</summary>
/// <remarks>
/// Use for non-HTTP work (database, SDKs, file I/O). Do not use for code that calls HttpClient when that client already has <c>AddLyoResilienceHandler</c> — resilience should
/// live on the HttpClient only, or retries nest. When the caller omits a pipeline name, the default is <see cref="PipelineNames.Basic" />.
/// </remarks>
public interface IResilientExecutor
{
    /// <summary>Runs an action through the default pipeline.</summary>
    /// <param name="action">Async work to run.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>Runs an action through the named resilience pipeline.</summary>
    /// <param name="pipelineName">Pipeline to use (default: <see cref="PipelineNames.Basic" />).</param>
    /// <param name="action">Async work to run.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(string pipelineName, Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>Runs an action through the default pipeline and returns its result.</summary>
    /// <param name="action">Async work to run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Value produced by the action.</returns>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    /// <summary>Runs an action through the named resilience pipeline and returns its result.</summary>
    /// <param name="pipelineName">Pipeline to use (default: <see cref="PipelineNames.Basic" />).</param>
    /// <param name="action">Async work to run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Value produced by the action.</returns>
    Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    /// <summary>Runs an action through the default pipeline and retries when the result fails the success predicate.</summary>
    /// <param name="action">Async work to run.</param>
    /// <param name="isSuccess">Returns true when the result is successful (no retry). Retries when false.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Value produced by the action.</returns>
    /// <remarks>Use for methods that return Result types; pass for example <c>r => r.IsSuccess</c> to retry on failure.</remarks>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, Func<T, bool> isSuccess, CancellationToken ct = default);

    /// <summary>Runs an action through the named pipeline and retries when the result fails the success predicate.</summary>
    /// <param name="pipelineName">Pipeline to use (default: <see cref="PipelineNames.Basic" />).</param>
    /// <param name="action">Async work to run.</param>
    /// <param name="isSuccess">Returns true when the result is successful (no retry). Retries when false.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Value produced by the action.</returns>
    /// <remarks>Use for methods that return Result types; pass for example <c>r => r.IsSuccess</c> to retry on failure.</remarks>
    Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, Func<T, bool> isSuccess, CancellationToken ct = default);
}