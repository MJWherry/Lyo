using Lyo.Exceptions;

namespace Lyo.Seed;

/// <summary>Named graph of generated items. Implementors supply factories that return each item. Bogus/Faker is optional and stays in the contributor.</summary>
public abstract class SeedContributor
{
    /// <summary>Name shown in hosts and stored on <see cref="SeedResult.Contributor"/>.</summary>
    public abstract string Name { get; }

    /// <summary>Which <see cref="ISeedTransport"/> kinds this graph can write through.</summary>
    public abstract SeedTransportKind SupportedTransports { get; }

    /// <summary>Adds entity factories, child factories, and after/clear hooks to <paramref name="graph"/>.</summary>
    protected abstract void Configure(SeedGraph graph, SeedOptions options);

    /// <summary>Runs <see cref="Configure"/> against an empty graph. <see cref="ISeedRunner"/> calls this.</summary>
    public void Build(SeedGraph graph, SeedOptions options)
    {
        ArgumentHelpers.ThrowIfNull(graph);
        ArgumentHelpers.ThrowIfNull(options);
        Configure(graph, options);
    }
}
