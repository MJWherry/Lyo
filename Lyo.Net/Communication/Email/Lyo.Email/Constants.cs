namespace Lyo.Email;

/// <summary>Consolidated constants for the Email library.</summary>
public static class Constants
{
    /// <summary>Constants for email service metric names and tags.</summary>
    public static class Metrics
    {
        public const string SendDuration = "email.send.duration";

        public const string SendSuccess = "email.send.success";

        public const string SendFailure = "email.send.failure";

        public const string SendCancelled = "email.send.cancelled";

        public const string SendLastDurationMs = "email.send.last_duration_ms";

        public const string BulkSendDuration = "email.bulk.send.duration";

        public const string BulkSendTotal = "email.bulk.send.total";

        public const string BulkSendSuccess = "email.bulk.send.success";

        public const string BulkSendFailure = "email.bulk.send.failure";

        public const string BulkSendLastDurationMs = "email.bulk.send.last_duration_ms";

        public const string SmtpConnectDuration = "email.smtp.connect.duration";

        public const string SmtpAuthenticateDuration = "email.smtp.authenticate.duration";

        public const string TestConnectionDuration = "email.test_connection.duration";

        public const string TestConnectionSuccess = "email.test_connection.success";

        public const string TestConnectionFailure = "email.test_connection.failure";
    }
}