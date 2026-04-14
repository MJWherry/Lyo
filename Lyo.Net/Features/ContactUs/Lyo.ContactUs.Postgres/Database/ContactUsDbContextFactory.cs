using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.ContactUs.Postgres.Database;

/// <summary>Design-time factory for creating ContactUsDbContext instances for migrations.</summary>
public class ContactUsDbContextFactory : IDesignTimeDbContextFactory<ContactUsDbContext>
{
    public ContactUsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONTACTUS_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "CONTACTUS_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ContactUsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "contact"));
        return new(optionsBuilder.Options);
    }
}