namespace Lyo.Seed;

/// <summary>Runs a <see cref="SeedContributor"/> through an <see cref="ISeedTransport"/>.</summary>
public interface ISeedRunner
{
    /// <summary>Builds the graph, applies <see cref="SeedOptions.Conflict"/>, generates items, and writes them in registration order.</summary>
    Task<SeedResult> SeedAsync(SeedContributor contributor, ISeedTransport transport, SeedOptions? options = null, CancellationToken ct = default);
}
