using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.ContactUs.Postgres.Database;

/// <summary>EF migrations factory that builds ContactUsDbContext instances for migrations.</summary>
public class ContactUsDbContextFactory : IDesignTimeDbContextFactory<ContactUsDbContext>
{
    /// <inheritdoc />
    public ContactUsDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<ContactUsDbContext>("CONTACTUS_CONNECTION_STRING", "contact"));
}