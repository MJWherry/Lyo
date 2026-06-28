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
}
