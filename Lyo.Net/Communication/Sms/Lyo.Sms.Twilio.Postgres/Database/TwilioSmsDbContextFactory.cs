using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Sms.Twilio.Postgres.Database;

/// <summary>Design-time factory that builds TwilioSmsDbContext for migrations.</summary>
public class TwilioSmsDbContextFactory : IDesignTimeDbContextFactory<TwilioSmsDbContext>
{
    /// <inheritdoc />
    public TwilioSmsDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<TwilioSmsDbContext>("TWILIO_SMS_CONNECTION_STRING", "sms"));
}