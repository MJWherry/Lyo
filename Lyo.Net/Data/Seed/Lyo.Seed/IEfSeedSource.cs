using Microsoft.EntityFrameworkCore;

namespace Lyo.Seed;

/// <summary>
/// EF contributor that knows its Postgres schema and how to open that schema's <see cref="DbContext" />. Hosts build <see cref="EfSeedTransport{TContext}" /> from
/// <see cref="CreateContext" /> instead of switching on contributor type.
/// </summary>
public interface IEfSeedSource
{
    /// <summary>Postgres schema that owns this contributor's tables. Matches the context's migrations history schema.</summary>
    string Schema { get; }

    /// <summary>Opens a context on <paramref name="connectionString" /> with the migrations history table in <see cref="Schema" />.</summary>
    DbContext CreateContext(string connectionString);
}