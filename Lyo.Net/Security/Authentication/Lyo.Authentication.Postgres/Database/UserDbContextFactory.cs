using System;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>Design-time factory for <see cref="UserDbContext"/>. Used by <c>dotnet ef</c> for migrations.</summary>
/// <remarks>Expects the connection string in the <c>USER_CONNECTION_STRING</c> environment variable.</remarks>
public sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    /// <inheritdoc/>
    public UserDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("USER_CONNECTION_STRING");
        OperationHelpers.ThrowIfNull(connectionString, "USER_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresUserOptions.Schema));
        return new(optionsBuilder.Options);
    }
}
