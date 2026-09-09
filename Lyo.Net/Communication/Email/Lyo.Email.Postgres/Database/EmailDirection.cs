namespace Lyo.Email.Postgres.Database;

/// <summary>Whether the email was sent or received.</summary>
public enum EmailDirection
{
    /// <summary>Outbound — sent by the application.</summary>
    Outbound,

    /// <summary>Inbound — received by the application.</summary>
    Inbound
}
