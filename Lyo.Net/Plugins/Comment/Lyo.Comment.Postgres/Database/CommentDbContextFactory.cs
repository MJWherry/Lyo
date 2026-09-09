using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Comment.Postgres.Database;

/// <summary>EF migrations factory that builds CommentDbContext instances for migrations.</summary>
public class CommentDbContextFactory : IDesignTimeDbContextFactory<CommentDbContext>
{
    /// <inheritdoc />
    public CommentDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<CommentDbContext>("COMMENT_CONNECTION_STRING", "comment"));
}