namespace Lyo.Sms.Twilio;

/// <summary>Consolidated constants for the Twilio SMS library.</summary>
public static class Constants
{
    /// <summary>Constants for Twilio SMS metric names.</summary>
    public static class Metrics
    {
        public const string SendDuration = "sms.twilio.send.duration";
        public const string SendSuccess = "sms.twilio.send.success";
        public const string SendFailure = "sms.twilio.send.failure";

        public const string BulkSendDuration = "sms.twilio.bulk.send.duration";
        public const string BulkSendTotal = "sms.twilio.bulk.send.total";
        public const string BulkSendSuccess = "sms.twilio.bulk.send.success";
        public const string BulkSendFailure = "sms.twilio.bulk.send.failure";
        public const string BulkSendLastDurationMs = "sms.twilio.bulk.send.last_duration_ms";

        public const string ApiGetMessageDuration = "sms.twilio.api.get_message.duration";
        public const string ApiGetMessagesDuration = "sms.twilio.api.get_messages.duration";
        public const string TestConnectionDuration = "sms.twilio.test_connection.duration";
    }
}