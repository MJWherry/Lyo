namespace Lyo.Sms.Twilio.Postgres.Database;

/// <summary>Whether the SMS was sent or received.</summary>
public enum MessageDirection
{
    /// <summary>Outbound — sent by the application.</summary>
    Outbound,

    /// <summary>Inbound — received by the application.</summary>
    Inbound
}