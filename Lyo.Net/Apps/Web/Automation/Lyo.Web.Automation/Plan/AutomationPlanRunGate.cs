namespace Lyo.Web.Automation.Plan;

/// <summary>
/// Holds a plan run between steps. Pause does not cancel a step that has already started. Stop cancels <see cref="WaitIfPausedAsync" /> through the run token.
/// </summary>
public sealed class AutomationPlanRunGate
{
    private readonly object _gate = new();
    private TaskCompletionSource<bool> _hold = Completed();
    private bool _paused;

    /// <summary>True while the next step is held.</summary>
    public bool IsPaused
    {
        get {
            lock (_gate)
                return _paused;
        }
    }

    /// <summary>Holds the run before the next step. A step already in progress finishes or fails on its own.</summary>
    public void Pause()
    {
        lock (_gate) {
            if (_paused)
                return;

            _paused = true;
            _hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    /// <summary>Releases a held run so the next unstarted step can begin.</summary>
    public void Resume()
    {
        TaskCompletionSource<bool> hold;
        lock (_gate) {
            if (!_paused)
                return;

            _paused = false;
            hold = _hold;
        }

        hold.TrySetResult(true);
    }

    /// <summary>Returns when the run is not paused. Cancellation is the run stop token.</summary>
    public Task WaitIfPausedAsync(CancellationToken ct)
    {
        Task task;
        lock (_gate) {
            if (!_paused)
                return Task.CompletedTask;

            task = _hold.Task;
        }

        if (task.IsCompleted)
            return task;

        return WaitForHoldAsync(task, ct);
    }

    private static async Task WaitForHoldAsync(Task hold, CancellationToken ct)
    {
        var cancel = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (ct.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancel)) {
            var done = await Task.WhenAny(hold, cancel.Task).ConfigureAwait(false);
            if (done != hold)
                ct.ThrowIfCancellationRequested();
        }

        await hold.ConfigureAwait(false);
    }

    private static TaskCompletionSource<bool> Completed()
    {
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        hold.TrySetResult(true);
        return hold;
    }
}
