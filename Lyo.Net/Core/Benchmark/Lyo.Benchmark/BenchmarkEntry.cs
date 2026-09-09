using System.Reflection;
using BenchmarkDotNet.Running;

namespace Lyo.Benchmark;

/// <summary>Entry point for benchmark executables: finds benchmarks in the entry assembly and runs them under the shared config.</summary>
public static class BenchmarkEntry
{
    /// <summary>Runs the BenchmarkDotNet switcher on the entry assembly (or <paramref name="assembly" /> if supplied) with <see cref="LyoBenchmarkConfig.Default" />.</summary>
    public static void Run(string[] args, Assembly? assembly = null)
        => BenchmarkSwitcher.FromAssembly(assembly ?? Assembly.GetEntryAssembly()!).Run(args, LyoBenchmarkConfig.Default);
}