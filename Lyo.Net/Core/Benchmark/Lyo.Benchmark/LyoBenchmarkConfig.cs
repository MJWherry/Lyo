using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Lyo.Benchmark.Export;

namespace Lyo.Benchmark;

/// <summary>
/// Common BenchmarkDotNet setup applied to all Lyo suites: default job, memory diagnoser, and <see cref="LyoBenchmarkExporter" /> for the shared report format. Use this instead
/// of repeating <c>[SimpleJob]</c> / <c>[MemoryDiagnoser]</c> on each class.
/// </summary>
public static class LyoBenchmarkConfig
{
    /// <summary>Config instance consumed by <see cref="BenchmarkEntry" />.</summary>
    public static IConfig Default { get; } = ManualConfig.Create(DefaultConfig.Instance)
        .AddJob(Job.Default)
        .AddDiagnoser(MemoryDiagnoser.Default)
        .AddExporter(LyoBenchmarkExporter.Default);
}