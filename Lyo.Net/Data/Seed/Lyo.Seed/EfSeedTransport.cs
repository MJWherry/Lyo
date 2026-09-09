using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Seed;

/// <summary>Writes generated items with <c>AddRange</c> plus <c>SaveChangesAsync</c> on <typeparamref name="TContext"/>.</summary>
public sealed class EfSeedTransport<TContext> : ISeedTransport
    where TContext : DbContext
{
    /// <summary>Builds a transport over an open context. The caller owns disposal.</summary>
    public EfSeedTransport(TContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        Context = context;
    }

    /// <summary>EF context used by <c>OnClear</c> and <c>After</c> callbacks that need schema-specific deletes.</summary>
    public TContext Context { get; }

    /// <inheritdoc />
    public SeedTransportKind Kind => SeedTransportKind.Ef;

    /// <inheritdoc />
    public async Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
        where T : class
        => !await Context.Set<T>().AsNoTracking().AnyAsync(ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
        where T : class
    {
        ArgumentHelpers.ThrowIfNull(items);
        if (items.Count == 0)
            return;

        Context.Set<T>().AddRange(items);
        await Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
