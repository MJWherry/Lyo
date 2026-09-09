using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.People.Postgres.Database;

/// <summary>Design-time factory that builds <see cref="PeopleDbContext" /> for migrations.</summary>
public class PeopleDbContextFactory : IDesignTimeDbContextFactory<PeopleDbContext>
{
    /// <inheritdoc />
    public PeopleDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<PeopleDbContext>("PEOPLE_CONNECTION_STRING", "people"));
}