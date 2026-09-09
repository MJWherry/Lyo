using Lyo.Exceptions;

namespace Lyo.Seed;

/// <summary>Stock <see cref="ISeedRunner"/>. Applies skip, replace, or append, then runs graph steps in order.</summary>
public sealed class SeedRunner : ISeedRunner
{
    /// <inheritdoc />
    public async Task<SeedResult> SeedAsync(SeedContributor contributor, ISeedTransport transport, SeedOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(contributor);
        ArgumentHelpers.ThrowIfNull(transport);
        options ??= new();
        if ((contributor.SupportedTransports & transport.Kind) == 0)
            throw new SeedException($"{contributor.Name} does not support {transport.Kind} (supports {contributor.SupportedTransports}).");

        var graph = new SeedGraph();
        contributor.Build(graph, options);
        var occupied = graph.SkipWhenCallback;
        if (options.Conflict == SeedConflictMode.SkipIfNotEmpty && occupied != null && await occupied(transport, ct).ConfigureAwait(false))
            return SeedResult.Skip(contributor.Name);

        var session = new SeedSession(transport, options);
        if (options.Conflict == SeedConflictMode.Replace) {
            if (graph.OnClearCallback == null)
                throw new SeedException($"{contributor.Name} does not register OnClear; Replace is not available.");

            await graph.OnClearCallback(session, ct).ConfigureAwait(false);
        }

        try {
            await graph.ExecuteAsync(session, ct).ConfigureAwait(false);
        }
        catch (SeedException ex) {
            return SeedResult.Fail(contributor.Name, [ex.Message], session.Counts);
        }

        return SeedResult.Ok(contributor.Name, session.Counts);
    }
}
