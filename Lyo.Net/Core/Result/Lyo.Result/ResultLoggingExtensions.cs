namespace Lyo.Common;

/// <summary>Extension methods for logging Results.</summary>
public static class ResultLoggingExtensions
{
    extension<T>(Result<T> result)
    {
        /// <summary>Logs success if the result is successful. Uses a simple action-based logger.</summary>
        public Result<T> LogSuccess(Action<string, object?[]> logAction, string operation)
        {
            if (result.IsSuccess)
                logAction("Operation {Operation} succeeded at {Timestamp}", [operation, result.Timestamp]);

            return result;
        }

        /// <summary>Logs failure if the result failed. Uses a simple action-based logger.</summary>
        public Result<T> LogFailure(Action<string, object?[]> logAction, string operation)
        {
            if (result.IsSuccess)
                return result;

            var errorSummary = result.Errors != null && result.Errors.Count > 0 ? string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}")) : "Unknown error";
            logAction("Operation {Operation} failed at {Timestamp}: {Errors}", [operation, result.Timestamp, errorSummary]);
            return result;
        }

        /// <summary>Logs both success and failure. Uses a simple action-based logger.</summary>
        public Result<T> Log(Action<string, object?[]> logSuccess, Action<string, object?[]> logFailure, string operation)
        {
            if (result.IsSuccess)
                logSuccess("Operation {Operation} succeeded at {Timestamp}", [operation, result.Timestamp]);
            else {
                var errorSummary = result.Errors != null && result.Errors.Count > 0 ? string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}")) : "Unknown error";
                logFailure("Operation {Operation} failed at {Timestamp}: {Errors}", [operation, result.Timestamp, errorSummary]);
            }

            return result;
        }
    }
}