using Lyo.Postgres;

namespace Lyo.Sms.Twilio.Postgres;

/// <summary>Settings for PostgreSQL Twilio SMS logging.</summary>
public sealed class PostgresTwilioSmsOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresTwilioSms";
    public const string Schema = "sms";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}