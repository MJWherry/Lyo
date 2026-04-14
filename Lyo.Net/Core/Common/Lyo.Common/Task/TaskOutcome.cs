namespace Lyo.Common.Task;

public sealed record TaskOutcome<T>(TaskResult Result, T? Data = default, Exception? Exception = null)
{
    public bool IsSuccess => Result == TaskResult.Success;

    public bool IsFaulted => Result == TaskResult.Faulted;

    public bool IsCanceled => Result == TaskResult.Canceled;
}