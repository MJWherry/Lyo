namespace Lyo.Lock;

/// <summary>Metric names and tag keys emitted when <see cref="LockOptions.EnableMetrics" /> or <see cref="KeyedSemaphoreOptions.EnableMetrics" /> is set.</summary>
public static class Constants
{
    /// <summary>Timers and counters for <see cref="Abstractions.ILockService" /> operations.</summary>
    public static class Metrics
    {
        /// <summary>Histogram/timer: time spent waiting for <see cref="Abstractions.ILockService.AcquireAsync" /> to succeed or give up.</summary>
        public const string AcquireDuration = "lock.acquire.duration";

        /// <summary>Counter: successful exclusive acquisitions.</summary>
        public const string AcquireSuccess = "lock.acquire.success";

        /// <summary>Counter: acquisitions that timed out or failed.</summary>
        public const string AcquireFailure = "lock.acquire.failure";

        /// <summary>Timer: <see cref="Abstractions.ILockHandle.ReleaseAsync" /> duration.</summary>
        public const string ReleaseDuration = "lock.release.duration";

        /// <summary>Timer: wall time for <see cref="Abstractions.ILockService.ExecuteWithLockAsync" /> overloads.</summary>
        public const string ExecuteDuration = "lock.execute.duration";

        /// <summary>Tag dimensions for lock metrics (logical key label).</summary>
        public static class Tags
        {
            /// <summary>Tag name whose value is the caller-supplied lock key (original casing as passed to the service).</summary>
            public const string Key = "key";
        }
    }

    /// <summary>Timers and counters for <see cref="Abstractions.IKeyedSemaphoreService" /> operations.</summary>
    public static class SemaphoreMetrics
    {
        /// <summary>Timer: time waiting for a semaphore permit.</summary>
        public const string AcquireDuration = "semaphore.acquire.duration";

        /// <summary>Counter: successful permit acquisitions.</summary>
        public const string AcquireSuccess = "semaphore.acquire.success";

        /// <summary>Counter: permit acquisitions that timed out.</summary>
        public const string AcquireFailure = "semaphore.acquire.failure";

        /// <summary>Timer: <see cref="Abstractions.IPermitHandle.ReleaseAsync" /> duration.</summary>
        public const string ReleaseDuration = "semaphore.release.duration";

        /// <summary>Timer: wall time for <see cref="Abstractions.IKeyedSemaphoreService.ExecuteAsync" /> overloads.</summary>
        public const string ExecuteDuration = "semaphore.execute.duration";

        /// <summary>Tag dimensions for semaphore metrics.</summary>
        public static class Tags
        {
            /// <summary>Tag name whose value is the caller-supplied semaphore key.</summary>
            public const string Key = "key";
        }
    }
}