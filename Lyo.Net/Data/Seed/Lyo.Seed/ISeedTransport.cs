namespace Lyo.Seed;

/// <summary>Destination that checks emptiness and writes generated batches (EF <c>SaveChanges</c> or Lyo.Api <c>POST {route}/Bulk</c>).</summary>
public interface ISeedTransport
{
    /// <summary>EF persist or HTTP bulk.</summary>
    SeedTransportKind Kind { get; }

    /// <summary>True when no rows of <typeparamref name="T"/> exist (EF <c>AnyAsync</c>, or API QueryConcrete with amount 1).</summary>
    Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
        where T : class;

    /// <summary>Inserts <paramref name="items"/>. An empty list does nothing.</summary>
    Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
        where T : class;
}
