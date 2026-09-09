namespace Lyo.Lock;

/// <summary>Metric names and tag keys written when <see cref="LockOptions.EnableMetrics" /> or <see cref="KeyedSemaphoreOptions.EnableMetrics" /> is on.</summary>
public static class Constants
{
    /// <summary>Timers and counters for <see cref="Abstractions.ILockService" /> calls.</summary>
    public static class Metrics
    {
        /// <summary>Histogram/timer: wait time until <see cref="Abstractions.ILockService.AcquireAsync" /> succeeds or gives up.</summary>
        public const string AcquireDuration = "lock.acquire.duration";

        /// <summary>Counter: exclusive acquisitions that succeeded.</summary>
        public const string AcquireSuccess = "lock.acquire.success";

        /// <summary>Counter: acquisitions that timed out or otherwise failed.</summary>
        public const string AcquireFailure = "lock.acquire.failure";

        /// <summary>Timer: duration of <see cref="Abstractions.ILockHandle.ReleaseAsync" />.</summary>
        public const string ReleaseDuration = "lock.release.duration";

        /// <summary>Timer: wall time for <see cref="Abstractions.ILockService.ExecuteWithLockAsync" /> overloads.</summary>
        public const string ExecuteDuration = "lock.execute.duration";

        /// <summary>Tag dimensions for lock metrics (logical key label).</summary>
        public static class Tags
        {
            /// <summary>Tag whose value is the caller-supplied lock key, in the original casing passed to the service.</summary>
            public const string Key = "key";
        }
    }

    /// <summary>Timers and counters for <see cref="Abstractions.IKeyedSemaphoreService" /> calls.</summary>
    public static class SemaphoreMetrics
    {
        /// <summary>Timer: wait time for a semaphore permit.</summary>
        public const string AcquireDuration = "semaphore.acquire.duration";

        /// <summary>Counter: permit acquisitions that succeeded.</summary>
        public const string AcquireSuccess = "semaphore.acquire.success";

        /// <summary>Counter: permit acquisitions that timed out.</summary>
        public const string AcquireFailure = "semaphore.acquire.failure";

        /// <summary>Timer: duration of <see cref="Abstractions.IPermitHandle.ReleaseAsync" />.</summary>
        public const string ReleaseDuration = "semaphore.release.duration";

        /// <summary>Timer: wall time for <see cref="Abstractions.IKeyedSemaphoreService.ExecuteAsync" /> overloads.</summary>
        public const string ExecuteDuration = "semaphore.execute.duration";

        /// <summary>Tag dimensions for semaphore metrics.</summary>
        public static class Tags
        {
            /// <summary>Tag whose value is the caller-supplied semaphore key.</summary>
            public const string Key = "key";
        }
    }
}