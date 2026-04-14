namespace Lyo.Profanity;

/// <summary>Consolidated constants for the Profanity library.</summary>
public static class Constants
{
    /// <summary>Constants for profanity filter service metric names.</summary>
    public static class Metrics
    {
        public const string FilterDuration = "profanity.filter.duration";

        public const string FilterSuccess = "profanity.filter.success";

        public const string FilterFailure = "profanity.filter.failure";

        public const string FilterInputLength = "profanity.filter.input_length";

        public const string FilterMatchCount = "profanity.filter.match_count";

        public const string FilterDurationMs = "profanity.filter.duration_ms";

        public const string ContainsProfanityDuration = "profanity.contains_profanity.duration";

        public const string ContainsProfanityCalls = "profanity.contains_profanity.calls";

        public const string ContainsProfanityPositive = "profanity.contains_profanity.positive";

        public const string ContainsProfanityDurationMs = "profanity.contains_profanity.duration_ms";

        public const string RefreshDuration = "profanity.refresh.duration";

        public const string RefreshSuccess = "profanity.refresh.success";

        public const string RefreshFailure = "profanity.refresh.failure";

        public const string RefreshWordCount = "profanity.refresh.word_count";

        public const string RefreshDurationMs = "profanity.refresh.duration_ms";
    }
}