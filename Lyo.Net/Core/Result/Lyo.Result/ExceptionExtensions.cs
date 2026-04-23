using System.Diagnostics.CodeAnalysis;

namespace Lyo.Common;

/// <summary>Extension methods for converting exceptions to Results.</summary>
public static class ExceptionExtensions
{
    /// <summary>Converts an exception to a failed Result.</summary>
    [return: NotNull]
    public static Result<T> ToResult<T>([NotNull] this Exception exception, string? code = null) => Result<T>.Failure(exception, code);

    /// <summary>Wraps an async operation in a Result, catching any exceptions.</summary>
    [return: NotNull]
    public static async Task<Result<T>> ToResultAsync<T>([NotNull] this Task<T> task, string? code = null)
    {
        try {
            var result = await task.ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (Exception ex) {
            return Result<T>.Failure(ex, code);
        }
    }

    /// <summary>Wraps an async Result operation, ensuring exceptions are caught.</summary>
    [return: NotNull]
    public static async Task<Result<T>> ToResultAsync<T>([NotNull] this Task<Result<T>> task, string? code = null)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex) {
            return Result<T>.Failure(ex, code);
        }
    }
}