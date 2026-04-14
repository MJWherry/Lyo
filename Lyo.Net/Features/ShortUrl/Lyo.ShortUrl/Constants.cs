namespace Lyo.ShortUrl;

/// <summary>Consolidated constants for the ShortUrl library.</summary>
public static class Constants
{
    /// <summary>Constants for URL shortener service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for shorten operations.</summary>
        public const string ShortenDuration = "urlshortener.shorten.duration";

        /// <summary>Counter metric for successful shorten operations.</summary>
        public const string ShortenSuccess = "urlshortener.shorten.success";

        /// <summary>Counter metric for failed shorten operations.</summary>
        public const string ShortenFailure = "urlshortener.shorten.failure";

        /// <summary>Counter metric for cancelled shorten operations.</summary>
        public const string ShortenCancelled = "urlshortener.shorten.cancelled";

        /// <summary>Duration metric for expand operations.</summary>
        public const string ExpandDuration = "urlshortener.expand.duration";

        /// <summary>Counter metric for successful expand operations.</summary>
        public const string ExpandSuccess = "urlshortener.expand.success";

        /// <summary>Counter metric for failed expand operations.</summary>
        public const string ExpandFailure = "urlshortener.expand.failure";

        /// <summary>Counter metric for cancelled expand operations.</summary>
        public const string ExpandCancelled = "urlshortener.expand.cancelled";

        /// <summary>Duration metric for statistics operations.</summary>
        public const string StatisticsDuration = "urlshortener.statistics.duration";

        /// <summary>Duration metric for delete operations.</summary>
        public const string DeleteDuration = "urlshortener.delete.duration";

        /// <summary>Duration metric for update operations.</summary>
        public const string UpdateDuration = "urlshortener.update.duration";
    }
}