using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Comment.Postgres.Database;

/// <summary>Design-time factory for creating CommentDbContext instances for migrations.</summary>
public class CommentDbContextFactory : IDesignTimeDbContextFactory<CommentDbContext>
{
    public CommentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COMMENT_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "COMMENT_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<CommentDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "comment"));
        return new(optionsBuilder.Options);
    }
}