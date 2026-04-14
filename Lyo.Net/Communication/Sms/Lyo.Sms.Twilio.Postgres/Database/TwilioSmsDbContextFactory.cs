using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Sms.Twilio.Postgres.Database;

/// <summary>Design-time factory for creating TwilioSmsDbContext instances for migrations.</summary>
public class TwilioSmsDbContextFactory : IDesignTimeDbContextFactory<TwilioSmsDbContext>
{
    public TwilioSmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TWILIO_SMS_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "TWILIO_SMS_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<TwilioSmsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "sms"));
        return new(optionsBuilder.Options);
    }
}