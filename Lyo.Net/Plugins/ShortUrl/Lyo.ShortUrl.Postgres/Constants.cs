namespace Lyo.ShortUrl.Postgres;

/// <summary>Shared constants for the ShortUrl Postgres library.</summary>
public static class Constants
{
    /// <summary>Metric/constant names for PostgreSQL URL shortener service metrics.</summary>
    public static class Metrics
    {
        public const string StatisticsDuration = "urlshortener.postgres.statistics.duration";
        public const string DeleteDuration = "urlshortener.postgres.delete.duration";
        public const string UpdateDuration = "urlshortener.postgres.update.duration";
    }
}