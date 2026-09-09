namespace Lyo.ShortUrl;

/// <summary>Shared constants for the ShortUrl library.</summary>
public static class Constants
{
    /// <summary>Metric/constant names for URL shortener service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Duration that tracks shorten operations.</summary>
        public const string ShortenDuration = "urlshortener.shorten.duration";

        /// <summary>Counter that tracks successful shorten operations.</summary>
        public const string ShortenSuccess = "urlshortener.shorten.success";

        /// <summary>Counter that tracks failed shorten operations.</summary>
        public const string ShortenFailure = "urlshortener.shorten.failure";

        /// <summary>Counter that tracks cancelled shorten operations.</summary>
        public const string ShortenCancelled = "urlshortener.shorten.cancelled";

        /// <summary>Duration that tracks expand operations.</summary>
        public const string ExpandDuration = "urlshortener.expand.duration";

        /// <summary>Counter that tracks successful expand operations.</summary>
        public const string ExpandSuccess = "urlshortener.expand.success";

        /// <summary>Counter that tracks failed expand operations.</summary>
        public const string ExpandFailure = "urlshortener.expand.failure";

        /// <summary>Counter that tracks cancelled expand operations.</summary>
        public const string ExpandCancelled = "urlshortener.expand.cancelled";

        /// <summary>Duration that tracks statistics operations.</summary>
        public const string StatisticsDuration = "urlshortener.statistics.duration";

        /// <summary>Duration that tracks delete operations.</summary>
        public const string DeleteDuration = "urlshortener.delete.duration";

        /// <summary>Duration that tracks update operations.</summary>
        public const string UpdateDuration = "urlshortener.update.duration";
    }
}