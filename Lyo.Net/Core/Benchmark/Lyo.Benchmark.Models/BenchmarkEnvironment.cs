namespace Lyo.Benchmark.Models;

/// <summary>Tool, runtime, and hardware that produced a <see cref="BenchmarkReport" />.</summary>
public sealed class BenchmarkEnvironment
{
    /// <summary>Producing tool, for example <c>BenchmarkDotNet</c> or <c>k6</c>.</summary>
    public string? Tool { get; set; }

    /// <summary>Version string of <see cref="Tool" />.</summary>
    public string? ToolVersion { get; set; }

    /// <summary>Runtime moniker or description (for example <c>.NET 10.0</c>).</summary>
    public string? Runtime { get; set; }

    /// <summary>CPU description.</summary>
    public string? Cpu { get; set; }

    /// <summary>OS description.</summary>
    public string? Os { get; set; }

    /// <summary>Process architecture (for example <c>X64</c>, <c>Arm64</c>).</summary>
    public string? Architecture { get; set; }

    /// <summary>Logical processors available to the run.</summary>
    public int? LogicalCores { get; set; }

    /// <summary>Physical cores, when the tool can see them.</summary>
    public int? PhysicalCores { get; set; }

    /// <summary>Memory in bytes available to the runtime/heap; follows cgroup limits inside containers.</summary>
    public long? MemoryBytes { get; set; }

    /// <summary>GC mode, for example <c>Server</c> or <c>Workstation</c> (plus <c>Concurrent</c> when that is on).</summary>
    public string? GcMode { get; set; }

    /// <summary>Build configuration used for the run, for example <c>RELEASE</c>.</summary>
    public string? Configuration { get; set; }

    /// <summary>.NET SDK version that produced the run, when available.</summary>
    public string? DotnetSdkVersion { get; set; }
}