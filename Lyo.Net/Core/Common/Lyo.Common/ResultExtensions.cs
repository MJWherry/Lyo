using System.Diagnostics.CodeAnalysis;
using Lyo.Common.Enums;

namespace Lyo.Common;

/// <summary>Extension methods and aggregation for Result types.</summary>
public static class ResultExtensions
{
    /// <summary>Combines multiple results. All must succeed for the combined result to succeed.</summary>
    [return: NotNull]
    public static Result<IReadOnlyList<T>> Combine<T>(this IEnumerable<Result<T>>? results)
    {
        if (results == null)
            return Result<IReadOnlyList<T>>.Failure("NULL_INPUT", "Results collection cannot be null");

        var list = results.ToList();
        if (list.Count == 0)
            return Result<IReadOnlyList<T>>.Success([]);

        var successes = new List<T>();
        var allErrors = new List<Error>();
        foreach (var r in list) {
            if (r.IsSuccess && r.Data != null)
                successes.Add(r.Data);
            else if (r.Errors != null)
                allErrors.AddRange(r.Errors);
        }

        return allErrors.Count > 0 ? Result<IReadOnlyList<T>>.Failure(allErrors) : Result<IReadOnlyList<T>>.Success(successes);
    }

    /// <summary>Returns the first successful result, or a combined failure if all failed.</summary>
    [return: NotNull]
    public static Result<T> FirstSuccess<T>(this IEnumerable<Result<T>>? results)
    {
        if (results == null)
            return Result<T>.Failure("NULL_INPUT", "Results collection cannot be null");

        var allErrors = new List<Error>();
        foreach (var r in results) {
            if (r.IsSuccess)
                return r;

            if (r.Errors != null)
                allErrors.AddRange(r.Errors);
        }

        return Result<T>.Failure(allErrors);
    }

    /// <summary>Converts a Result&lt;T&gt; to a Result&lt;TRequest, TResult&gt; by adding a request object.</summary>
    public static Result<TRequest, TResult> WithRequest<TRequest, TResult>(this Result<TResult> result, TRequest request)
        => result.IsSuccess
            ? Result<TRequest, TResult>.Success(request, result.Data!, result.Timestamp, result.Metadata)
            : Result<TRequest, TResult>.Failure(request, result.Errors ?? [], result.Timestamp, result.Metadata);

    private static IEnumerable<string> GetAllErrorCodes(Error error)
    {
        yield return error.Code;

        var current = error.InnerError;
        while (current != null) {
            yield return current.Code;

            current = current.InnerError;
        }
    }

    private static IEnumerable<string> GetAllErrorMessages(Error error)
    {
        yield return error.Message;

        var current = error.InnerError;
        while (current != null) {
            yield return current.Message;

            current = current.InnerError;
        }
    }

    private static bool HasSeverityRecursive(Error? error, ErrorSeverity severity)
    {
        if (error == null)
            return false;

        return error.Severity == severity || HasSeverityRecursive(error.InnerError, severity);
    }

    /// <summary>Converts a Result&lt;TRequest, TResult&gt; to a Result&lt;TResult&gt; by extracting just the result part.</summary>
    public static Result<TResult> ToResult<TRequest, TResult>(this Result<TRequest, TResult> result)
        => result.IsSuccess
            ? Result<TResult>.Success(result.Data!, result.Timestamp, result.Metadata)
            : Result<TResult>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata);

    extension<TIn>(Result<TIn> result)
    {
        /// <summary>Chains two results together. If the first result is successful, applies the function to get the next result.</summary>
        public Result<TOut> Then<TOut>(Func<TIn, Result<TOut>> next)
            => !result.IsSuccess ? Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata) : next(result.Data!);

        /// <summary>Combines two results into a tuple result. Both must succeed for the combined result to succeed.</summary>
        public Result<(TIn, T2)> Combine<T2>(Result<T2> r2)
        {
            if (result.IsSuccess && r2.IsSuccess)
                return Result<(TIn, T2)>.Success((result.Data!, r2.Data!), result.Timestamp, result.Metadata);

            var errors = new List<Error>();
            if (!result.IsSuccess && result.Errors != null)
                errors.AddRange(result.Errors);

            if (!r2.IsSuccess && r2.Errors != null)
                errors.AddRange(r2.Errors);

            return Result<(TIn, T2)>.Failure(errors, result.Timestamp, result.Metadata);
        }

        /// <summary>Combines three results into a tuple result. All must succeed for the combined result to succeed.</summary>
        public Result<(TIn, T2, T3)> Combine<T2, T3>(Result<T2> r2, Result<T3> r3)
        {
            if (result.IsSuccess && r2.IsSuccess && r3.IsSuccess)
                return Result<(TIn, T2, T3)>.Success((result.Data!, r2.Data!, r3.Data!), result.Timestamp, result.Metadata);

            var errors = new List<Error>();
            if (!result.IsSuccess && result.Errors != null)
                errors.AddRange(result.Errors);

            if (!r2.IsSuccess && r2.Errors != null)
                errors.AddRange(r2.Errors);

            if (!r3.IsSuccess && r3.Errors != null)
                errors.AddRange(r3.Errors);

            return Result<(TIn, T2, T3)>.Failure(errors, result.Timestamp, result.Metadata);
        }

        /// <summary>Executes an action if the result is successful.</summary>
        public Result<TIn> OnSuccess(Action<TIn> action)
        {
            if (result.IsSuccess && result.Data != null)
                action(result.Data);

            return result;
        }

        /// <summary>Executes an action if the result is successful and returns a new result.</summary>
        public Result<TOut> OnSuccess<TOut>(Func<TIn, Result<TOut>> func)
            => !result.IsSuccess ? Result<TOut>.Failure(result.Errors ?? [], result.Timestamp, result.Metadata) : func(result.Data!);

        /// <summary>Executes an action if the result failed.</summary>
        public Result<TIn> OnFailure(Action<IReadOnlyList<Error>> action)
        {
            if (!result.IsSuccess)
                action(result.Errors ?? []);

            return result;
        }

        /// <summary>Executes an action if the result failed and returns a new result.</summary>
        public Result<TIn> OnFailure(Func<IReadOnlyList<Error>, Result<TIn>> func) => !result.IsSuccess ? func(result.Errors ?? []) : result;

        /// <summary>Gets all error messages from the result's errors.</summary>
        public IReadOnlyList<string> GetErrorMessages()
        {
            if (result.Errors == null || result.Errors.Count == 0)
                return [];

            return result.Errors.SelectMany(GetAllErrorMessages).Distinct().ToList();
        }

        /// <summary>Gets all error codes from the result's errors.</summary>
        public IReadOnlyList<string> GetErrorCodes()
        {
            if (result.Errors == null || result.Errors.Count == 0)
                return [];

            return result.Errors.SelectMany(GetAllErrorCodes).Distinct().ToList();
        }
    }

    extension<T>(Result<T> result)
    {
        /// <summary>Gets the first error from the result, if any.</summary>
        public Error? GetFirstError() => result.Errors?.FirstOrDefault();

        /// <summary>Checks if the result has any errors with the specified severity.</summary>
        public bool HasSeverity(ErrorSeverity severity) => result.Errors?.Any(e => e.Severity == severity || HasSeverityRecursive(e.InnerError, severity)) ?? false;
    }

    extension<T>(Result<T> result)
    {
        /// <summary>Groups errors by their error code.</summary>
        public ILookup<string, Error> GroupErrorsByCode()
        {
            var allErrors = result.GetAllErrors();
            return allErrors.GroupBy(e => e.Code).ToLookup(g => g.Key, g => g.First());
        }

        /// <summary>Groups errors by their severity.</summary>
        public ILookup<ErrorSeverity, Error> GroupErrorsBySeverity()
        {
            var allErrors = result.GetAllErrors();
            return allErrors.GroupBy(e => e.Severity).ToLookup(g => g.Key, g => g.First());
        }
    }
}