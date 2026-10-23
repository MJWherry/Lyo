using System.Diagnostics;

namespace Lyo.Seed;

/// <summary>Run settings handed to <see cref="SeedContributor.Configure" />. The engine never applies <see cref="RandomSeed" />; contributors that use Bogus (or similar) should.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SeedOptions
{
    /// <summary>How many root entities to create. Starts at 50.</summary>
    public int Count { get; init; } = 50;

    /// <summary>Optional deterministic seed for the contributor's generator. <see cref="ISeedRunner" /> ignores this.</summary>
    public int? RandomSeed { get; init; }

    /// <summary>What happens when destination data already exists. Starts as <see cref="SeedConflictMode.Append" />; skip is opt-in.</summary>
    public SeedConflictMode Conflict { get; init; } = SeedConflictMode.Append;

    /// <summary>Root count, optional generator seed, and conflict mode.</summary>
    public override string ToString() => $"Count={Count} RandomSeed={RandomSeed} Conflict={Conflict}";
}