namespace Lyo.Lock;

/// <summary>Consolidated constants for the Lock library.</summary>
public static class Constants
{
    /// <summary>Constants for lock service metric names and tags.</summary>
    public static class Metrics
    {
        public const string AcquireDuration = "lock.acquire.duration";

        public const string AcquireSuccess = "lock.acquire.success";

        public const string AcquireFailure = "lock.acquire.failure";

        public const string ReleaseDuration = "lock.release.duration";

        public const string ExecuteDuration = "lock.execute.duration";

        /// <summary>Common tag keys for lock metrics.</summary>
        public static class Tags
        {
            public const string Key = "key";
        }
    }

    /// <summary>Constants for keyed semaphore metric names and tags.</summary>
    public static class SemaphoreMetrics
    {
        public const string AcquireDuration = "semaphore.acquire.duration";

        public const string AcquireSuccess = "semaphore.acquire.success";

        public const string AcquireFailure = "semaphore.acquire.failure";

        public const string ReleaseDuration = "semaphore.release.duration";

        public const string ExecuteDuration = "semaphore.execute.duration";

        /// <summary>Common tag keys for semaphore metrics.</summary>
        public static class Tags
        {
            public const string Key = "key";
        }
    }
}