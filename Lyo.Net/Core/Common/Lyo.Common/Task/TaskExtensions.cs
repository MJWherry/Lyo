namespace Lyo.Common.Task;

public static class TaskExtensions
{
    extension<T>(Task<T> task)
    {
        public async Task<TaskOutcome<T>> After(Action<TaskResult, T?, Exception?> callback, bool rethrow = false)
        {
            try {
                var result = await task;
                callback(TaskResult.Success, result, null);
                return new(TaskResult.Success, result);
            }
            catch (OperationCanceledException oce) {
                callback(TaskResult.Canceled, default, oce);
                if (rethrow)
                    throw;

                return new(TaskResult.Canceled, default, oce);
            }
            catch (Exception ex) {
                callback(TaskResult.Faulted, default, ex);
                if (rethrow)
                    throw;

                return new(TaskResult.Faulted, default, ex);
            }
        }

        public async Task<T> OnException(Action<Exception> handler, bool rethrow = true)
        {
            try {
                return await task;
            }
            catch (Exception ex) {
                handler?.Invoke(ex);
                if (rethrow)
                    throw;

                return default!;
            }
        }

        public async Task<T> OnCancelled(Action handler, bool rethrow = true)
        {
            try {
                return await task;
            }
            catch (OperationCanceledException) {
                handler?.Invoke();
                if (rethrow)
                    throw;

                return default!;
            }
        }

        public async Task<T> OnCompleted(Action handler)
        {
            var result = await task;
            handler?.Invoke();
            return result;
        }

        public async Task<T> OnSuccess(Action<T> handler)
        {
            try {
                var result = await task;
                handler?.Invoke(result);
                return result;
            }
            catch {
                // Ignore — don't fire OnSuccess
                return default!;
            }
        }
    }

    extension(System.Threading.Tasks.Task task)
    {
        public async Task<TaskOutcome<object?>> After(Action<TaskResult, Exception?> callback, bool rethrow = false)
        {
            try {
                await task;
                callback(TaskResult.Success, null);
                return new(TaskResult.Success);
            }
            catch (OperationCanceledException oce) {
                callback(TaskResult.Canceled, oce);
                if (rethrow)
                    throw;

                return new(TaskResult.Canceled, null, oce);
            }
            catch (Exception ex) {
                callback(TaskResult.Faulted, ex);
                if (rethrow)
                    throw;

                return new(TaskResult.Faulted, null, ex);
            }
        }

        public async System.Threading.Tasks.Task OnException(Action<Exception> handler, bool rethrow = true)
        {
            try {
                await task;
            }
            catch (Exception ex) {
                handler?.Invoke(ex);
                if (rethrow)
                    throw;
            }
        }

        public async System.Threading.Tasks.Task OnCancelled(Action handler, bool rethrow = true)
        {
            try {
                await task;
            }
            catch (OperationCanceledException) {
                handler?.Invoke();
                if (rethrow)
                    throw;
            }
        }

        public async System.Threading.Tasks.Task OnCompleted(Action handler)
        {
            await task;
            handler?.Invoke();
        }

        public async System.Threading.Tasks.Task OnSuccess(Action handler)
        {
            try {
                await task;
                handler?.Invoke();
            }
            catch {
                // Ignore errors — OnSuccess only fires if task completes successfully
            }
        }
    }
}