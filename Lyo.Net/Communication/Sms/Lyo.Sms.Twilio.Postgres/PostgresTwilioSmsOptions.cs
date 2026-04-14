using Lyo.Postgres;

namespace Lyo.Sms.Twilio.Postgres;

/// <summary>Configuration options for PostgreSQL Twilio SMS logging service.</summary>
public sealed class PostgresTwilioSmsOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresTwilioSms";
    public const string Schema = "sms";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}