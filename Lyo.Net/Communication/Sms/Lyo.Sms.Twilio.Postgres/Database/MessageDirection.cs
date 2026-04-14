namespace Lyo.Sms.Twilio.Postgres.Database;

/// <summary>Direction of the SMS message.</summary>
public enum MessageDirection
{
    /// <summary>Outbound message (sent from the application).</summary>
    Outbound,

    /// <summary>Inbound message (received by the application).</summary>
    Inbound
}