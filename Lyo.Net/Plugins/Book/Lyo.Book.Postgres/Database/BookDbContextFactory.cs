using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Book.Postgres.Database;

/// <summary>EF migrations factory for <see cref="BookDbContext" />.</summary>
public class BookDbContextFactory : IDesignTimeDbContextFactory<BookDbContext>
{
    /// <inheritdoc />
    public BookDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<BookDbContext>(
            "BOOK_CONNECTION_STRING",
            "book",
            "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}
