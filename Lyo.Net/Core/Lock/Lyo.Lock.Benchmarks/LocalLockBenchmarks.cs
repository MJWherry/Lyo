using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Lock.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Lock.Benchmarks;

/// <summary>In-process lock benchmarks: uncontended acquire/release plus a bounded contended scenario (directional — measures coordination overhead, not throughput).</summary>
[BenchmarkDescription(
    "In-process LocalLockService: a baseline uncontended acquire/release on a unique key, and a bounded contended scenario where N contenders execute-with-lock on one shared key (directional coordination-overhead signal, not throughput).")]
[BenchmarkParameter("Contenders", Unit = "tasks", Description = "Number of concurrent tasks contending for the single shared key in the contended case (2 or 8).")]
public class LocalLockBenchmarks
{
    private ILockService _local = null!;

    [Params(2, 8)]
    public int Contenders { get; set; }

    [GlobalSetup]
    public void Setup() => _local = new LocalLockService(NullLogger<LocalLockService>.Instance, new());

    [Benchmark(Baseline = true)]
    [BenchmarkSla(MaxMeanUs = 10, Standard = "An uncontended in-process lock acquire/release is pure local coordination and should be a few microseconds at most.")]
    public async Task Uncontended_AcquireRelease()
    {
        var handle = await _local.AcquireAsync($"uncontended-{Guid.NewGuid():N}", TimeSpan.FromSeconds(5));
        await handle!.ReleaseAsync();
    }

    [Benchmark]
    public async Task Contended_SingleKey()
    {
        const string key = "contended-shared-key";
        var tasks = new Task[Contenders];
        for (var i = 0; i < Contenders; i++)
            tasks[i] = _local.ExecuteWithLockAsync(key, _ => Task.CompletedTask, TimeSpan.FromSeconds(10));

        await Task.WhenAll(tasks);
    }
}