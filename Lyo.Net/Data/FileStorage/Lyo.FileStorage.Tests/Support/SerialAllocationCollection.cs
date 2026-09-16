namespace Lyo.FileStorage.Tests.Support;

/// <summary>
/// Marker for suites that read <see cref="GC.GetTotalAllocatedBytes(bool)" />. That counter is process-wide, so those tests have to run on their own or a neighbouring
/// collection's allocations land in the measurement.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerialAllocationCollection
{
    public const string Name = "FileStorage allocation measurements";
}
