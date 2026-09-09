using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>Design-time <see cref="UserDbContext" /> factory consumed by <c>dotnet ef</c> migrations.</summary>
/// <remarks>Reads the connection string from the <c>USER_CONNECTION_STRING</c> environment variable.</remarks>
public sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    /// <inheritdoc />
    public UserDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<UserDbContext>("USER_CONNECTION_STRING", PostgresUserOptions.Schema));
}