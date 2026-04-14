namespace Lyo.Resilience;

/// <summary>Executes actions through a named resilience pipeline.</summary>
/// <remarks>
/// Use for non-HTTP work (database, SDKs, file I/O). Do NOT use for code that calls HttpClient when that client already has <c>AddLyoResilienceHandler</c>—resilience should
/// live on the HttpClient only, or you get nested retries. When no pipeline name is specified, uses the default <see cref="PipelineNames.Basic" /> pipeline.
/// </remarks>
public interface IResilientExecutor
{
    /// <summary>Executes an action through the default pipeline.</summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>Executes an action through the specified resilience pipeline.</summary>
    /// <param name="pipelineName">The name of the pipeline to use (default: <see cref="PipelineNames.Basic" />).</param>
    /// <param name="action">The async action to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(string pipelineName, Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>Executes an action through the default pipeline and returns a result.</summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the action.</returns>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    /// <summary>Executes an action through the specified resilience pipeline and returns a result.</summary>
    /// <param name="pipelineName">The name of the pipeline to use (default: <see cref="PipelineNames.Basic" />).</param>
    /// <param name="action">The async action to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the action.</returns>
    Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    /// <summary>Executes an action through the default pipeline, retrying when the result fails the success condition.</summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="isSuccess">Predicate that returns true when the result is successful (no retry). Retries when false.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the action.</returns>
    /// <remarks>Use for methods that return Result types; pass e.g. <c>r => r.IsSuccess</c> to retry on failure.</remarks>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, Func<T, bool> isSuccess, CancellationToken ct = default);

    /// <summary>Executes an action through the specified pipeline, retrying when the result fails the success condition.</summary>
    /// <param name="pipelineName">The name of the pipeline to use (default: <see cref="PipelineNames.Basic" />).</param>
    /// <param name="action">The async action to execute.</param>
    /// <param name="isSuccess">Predicate that returns true when the result is successful (no retry). Retries when false.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the action.</returns>
    /// <remarks>Use for methods that return Result types; pass e.g. <c>r => r.IsSuccess</c> to retry on failure.</remarks>
    Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, Func<T, bool> isSuccess, CancellationToken ct = default);
}