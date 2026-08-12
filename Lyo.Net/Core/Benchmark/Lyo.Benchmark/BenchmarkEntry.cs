using System.Reflection;
using BenchmarkDotNet.Running;

namespace Lyo.Benchmark;

/// <summary>Single entry point for benchmark executables: discovers benchmarks in the entry assembly and runs them with the shared config.</summary>
public static class BenchmarkEntry
{
    /// <summary>Runs the benchmark switcher over the entry assembly (or <paramref name="assembly" /> when provided) using <see cref="LyoBenchmarkConfig.Default" />.</summary>
    public static void Run(string[] args, Assembly? assembly = null)
        => BenchmarkSwitcher.FromAssembly(assembly ?? Assembly.GetEntryAssembly()!).Run(args, LyoBenchmarkConfig.Default);
}