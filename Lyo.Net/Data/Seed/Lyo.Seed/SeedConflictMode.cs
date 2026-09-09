namespace Lyo.Seed;

/// <summary>What a seed run does when the destination already has rows.</summary>
public enum SeedConflictMode
{
    /// <summary>Insert generated rows and leave existing data in place. This is the default.</summary>
    Append,

    /// <summary>Opt-in: skip the run when the root set already has rows.</summary>
    SkipIfNotEmpty,

    /// <summary>Run <see cref="SeedGraph.OnClear"/> then insert. The contributor must register a clear callback.</summary>
    Replace
}
