namespace Lyo.Seed;

/// <summary>Persist backends a <see cref="SeedContributor"/> can write to. Flags, so one contributor can support both.</summary>
[Flags]
public enum SeedTransportKind
{
    /// <summary>No backend selected.</summary>
    None = 0,

    /// <summary>Write through an EF Core <c>DbContext</c>.</summary>
    Ef = 1,

    /// <summary>Lyo.Api bulk create over HTTP.</summary>
    Api = 2
}
