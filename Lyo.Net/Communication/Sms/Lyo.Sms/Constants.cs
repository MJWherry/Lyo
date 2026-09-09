namespace Lyo.Sms;

/// <summary>Shared constants used throughout the Sms library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags for SMS service instrumentation.</summary>
    public static class Metrics
    {
        public const string SendDuration = "sms.send.duration";

        public const string SendSuccess = "sms.send.success";

        public const string SendFailure = "sms.send.failure";

        public const string BulkSendDuration = "sms.bulk.send.duration";

        public const string BulkSendTotal = "sms.bulk.send.total";

        public const string BulkSendSuccess = "sms.bulk.send.success";

        public const string BulkSendFailure = "sms.bulk.send.failure";

        public const string BulkSendLastDurationMs = "sms.bulk.send.last_duration_ms";
    }
}