namespace Lyo.Email;

/// <summary>Shared constants used by email providers.</summary>
public static class Constants
{
    /// <summary>Metric names for the shared send pipeline. SMTP connect timings live on the SMTP package.</summary>
    public static class Metrics
    {
        /// <summary>Timer for one send.</summary>
        public const string SendDuration = "email.send.duration";

        /// <summary>Successful sends.</summary>
        public const string SendSuccess = "email.send.success";

        /// <summary>Failed sends.</summary>
        public const string SendFailure = "email.send.failure";

        /// <summary>Cancelled sends.</summary>
        public const string SendCancelled = "email.send.cancelled";

        /// <summary>Last single-send duration, in milliseconds.</summary>
        public const string SendLastDurationMs = "email.send.last_duration_ms";

        /// <summary>Timer for one bulk send.</summary>
        public const string BulkSendDuration = "email.bulk.send.duration";

        /// <summary>Messages included in bulk sends.</summary>
        public const string BulkSendTotal = "email.bulk.send.total";

        /// <summary>Successful messages inside bulk sends.</summary>
        public const string BulkSendSuccess = "email.bulk.send.success";

        /// <summary>Failed messages inside bulk sends.</summary>
        public const string BulkSendFailure = "email.bulk.send.failure";

        /// <summary>Last bulk-send duration, in milliseconds.</summary>
        public const string BulkSendLastDurationMs = "email.bulk.send.last_duration_ms";

        /// <summary>Timer for a connection probe.</summary>
        public const string TestConnectionDuration = "email.test_connection.duration";

        /// <summary>Successful connection probes.</summary>
        public const string TestConnectionSuccess = "email.test_connection.success";

        /// <summary>Failed connection probes.</summary>
        public const string TestConnectionFailure = "email.test_connection.failure";
    }
}
