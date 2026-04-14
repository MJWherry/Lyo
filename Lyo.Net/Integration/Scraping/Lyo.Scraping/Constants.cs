namespace Lyo.Scraping;

/// <summary>Consolidated constants for the Scraping library.</summary>
public static class Constants
{
    /// <summary>Constants for scraping metrics.</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for browser start operations.</summary>
        public const string StartBrowserDuration = "lyo.scraping.browser.start.duration";

        /// <summary>Duration metric for browser stop operations.</summary>
        public const string StopBrowserDuration = "lyo.scraping.browser.stop.duration";

        /// <summary>Counter metric for successful poll operations.</summary>
        public const string PollSuccess = "lyo.scraping.poll.success";

        /// <summary>Counter metric for failed poll operations.</summary>
        public const string PollFailure = "lyo.scraping.poll.failure";

        /// <summary>Duration metric for poll operations.</summary>
        public const string PollDuration = "lyo.scraping.poll.duration";
    }
}