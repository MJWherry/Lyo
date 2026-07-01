using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Lyo.Benchmarking.Export;

namespace Lyo.Benchmarking;

/// <summary>
/// Shared BenchmarkDotNet configuration for every Lyo benchmark suite: a default job, the memory diagnoser, and the <see cref="LyoBenchmarkExporter" /> that emits the
/// unified report. Replaces the per-class <c>[SimpleJob]</c>/<c>[MemoryDiagnoser]</c> attributes.
/// </summary>
public static class LyoBenchmarkConfig
{
    /// <summary>The shared config instance used by <see cref="BenchmarkEntry" />.</summary>
    public static IConfig Default { get; } = ManualConfig.Create(DefaultConfig.Instance)
        .AddJob(Job.Default)
        .AddDiagnoser(MemoryDiagnoser.Default)
        .AddExporter(LyoBenchmarkExporter.Default);
}