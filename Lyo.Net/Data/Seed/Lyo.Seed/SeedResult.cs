namespace Lyo.Seed;

/// <summary>What <see cref="ISeedRunner.SeedAsync"/> reports when a run finishes.</summary>
public sealed record SeedResult(
    bool Success,
    bool Skipped,
    string Contributor,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<string> Errors)
{
    /// <summary>Insert finished with no errors.</summary>
    public static SeedResult Ok(string contributor, IReadOnlyDictionary<string, int> counts)
        => new(true, false, contributor, counts, []);

    /// <summary>Run skipped because the destination already had rows.</summary>
    public static SeedResult Skip(string contributor)
        => new(true, true, contributor, new Dictionary<string, int>(), []);

    /// <summary>Persist or bulk-row errors stopped the run.</summary>
    public static SeedResult Fail(string contributor, IReadOnlyList<string> errors, IReadOnlyDictionary<string, int>? counts = null)
        => new(false, false, contributor, counts ?? new Dictionary<string, int>(), errors);
}
