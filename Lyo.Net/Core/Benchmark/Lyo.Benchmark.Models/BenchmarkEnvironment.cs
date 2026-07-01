namespace Lyo.Benchmark.Models;

/// <summary>Tool, runtime, and hardware context that produced a <see cref="BenchmarkReport" />.</summary>
public sealed class BenchmarkEnvironment
{
    /// <summary>Producing tool, e.g. <c>BenchmarkDotNet</c> or <c>k6</c>.</summary>
    public string? Tool { get; set; }

    /// <summary>Version of <see cref="Tool" />.</summary>
    public string? ToolVersion { get; set; }

    /// <summary>Runtime moniker / description (e.g. <c>.NET 10.0</c>).</summary>
    public string? Runtime { get; set; }

    /// <summary>Processor description.</summary>
    public string? Cpu { get; set; }

    /// <summary>Operating system description.</summary>
    public string? Os { get; set; }

    /// <summary>Process architecture (e.g. <c>X64</c>, <c>Arm64</c>).</summary>
    public string? Architecture { get; set; }

    /// <summary>Number of logical processors available to the run.</summary>
    public int? LogicalCores { get; set; }

    /// <summary>Number of physical cores, when the tool can detect it.</summary>
    public int? PhysicalCores { get; set; }

    /// <summary>Total memory (in bytes) available to the runtime/heap; reflects cgroup limits inside containers.</summary>
    public long? MemoryBytes { get; set; }

    /// <summary>Garbage collector mode, e.g. <c>Server</c> or <c>Workstation</c> (with <c>Concurrent</c> when enabled).</summary>
    public string? GcMode { get; set; }

    /// <summary>Build configuration the benchmarks ran under, e.g. <c>RELEASE</c>.</summary>
    public string? Configuration { get; set; }

    /// <summary>.NET SDK version that produced the run, when known.</summary>
    public string? DotnetSdkVersion { get; set; }
}