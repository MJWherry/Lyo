using BenchmarkDotNet.Attributes;
using Lyo.Benchmark.Data;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;

namespace Lyo.Benchmark;

/// <summary>
/// Shared base for Lyo BenchmarkDotNet suites. It holds one <see cref="IIOTempService" /> and a root <see cref="IIOTempSession" /> per suite, then calls
/// <see cref="OnGlobalSetup" /> / <see cref="OnGlobalCleanup" /> so subclasses never own the IOTemp lifetime.
/// </summary>
public abstract class LyoBenchmarkBase
{
    private bool _globalSetupDone;
    private IIOTempSession? _iterationTemp;

    /// <summary>IOTemp service for this suite. This base disposes it; suites may still call service APIs.</summary>
    protected IIOTempService TempService { get; private set; } = null!;

    /// <summary>Root session from <see cref="TempService" />. Suite file I/O must stay on this session or a child session.</summary>
    protected IIOTempSession Temp { get; private set; } = null!;

    /// <summary>Lazy sub-session for the current iteration. <see cref="BenchmarkIterationCleanup" /> drops it.</summary>
    protected IIOTempSession IterationTemp {
        get {
            _iterationTemp ??= Temp.CreateSubSession();
            return _iterationTemp;
        }
    }

    /// <summary>
    /// Builds <see cref="TempService" /> and <see cref="Temp" />, then runs <see cref="OnGlobalSetup" />. Do not put an untargeted <see cref="GlobalSetupAttribute" /> on a
    /// derived suite; override <see cref="OnGlobalSetup" /> instead. Targeted <c>[GlobalSetup(Target = ...)]</c> / <c>Targets</c> methods are fine for per-benchmark prep — they
    /// must call <see cref="EnsureGlobalSetup" /> first, because BenchmarkDotNet can run those targeted setups before this base method.
    /// </summary>
    [GlobalSetup]
    public void BenchmarkGlobalSetup() => EnsureGlobalSetup();

    /// <summary>
    /// Shared setup that is safe to call more than once (IOTemp plus <see cref="OnGlobalSetup" />). Start every derived <c>[GlobalSetup(Target = ...)]</c> method with this so
    /// prep does not NRE if BDN runs targeted setup ahead of the untargeted base setup.
    /// </summary>
    protected void EnsureGlobalSetup()
    {
        if (_globalSetupDone)
            return;

        TempService = new IOTempService(
            new() {
                DirectoryName = $"lyo-bench-{Guid.NewGuid():N}",
                EnableMetrics = false,
                // Streaming suites can emit 2 GiB of plaintext plus matching ciphertext/compressed files and per-iteration outputs.
                MaxFileSizeBytes = 8L * BenchmarkData.MiB * 1024,
                MaxTotalSizeBytes = 64L * BenchmarkData.MiB * 1024
            });

        Temp = TempService.CreateSession();
        OnGlobalSetup();
        _globalSetupDone = true;
    }

    /// <summary>Runs <see cref="OnGlobalCleanup" />, then disposes the iteration sub-session, <see cref="Temp" />, and <see cref="TempService" /> (the service directory is wiped).</summary>
    [GlobalCleanup]
    public void BenchmarkGlobalCleanup()
    {
        try {
            if (_globalSetupDone)
                OnGlobalCleanup();
        }
        finally {
            DisposeIterationTemp();
            Temp?.Dispose();
            Temp = null!;
            TempService?.Dispose();
            TempService = null!;
            _globalSetupDone = false;
        }
    }

    /// <summary>Drops the current iteration sub-session so outputs from one BenchmarkDotNet iteration do not pile up into the next.</summary>
    [IterationCleanup]
    public void BenchmarkIterationCleanup() => DisposeIterationTemp();

    /// <summary>
    /// Suite hook after IOTemp is in place. Prefer this to an untargeted derived <c>[GlobalSetup]</c>. When only some benchmarks need costly prep, put targeted
    /// <c>[GlobalSetup(Target = ...)]</c> on the derived type and start each of those methods with <see cref="EnsureGlobalSetup" />.
    /// </summary>
    protected virtual void OnGlobalSetup() { }

    /// <summary>Suite hook before IOTemp is torn down. Close any streams that still hold files under <see cref="Temp" />.</summary>
    protected virtual void OnGlobalCleanup() { }

    /// <summary>Writes a seeded plaintext file of <paramref name="size" /> bytes under <see cref="Temp" /> and returns the path. Disposal of the session deletes the file.</summary>
    protected string CreateSeededFilePath(long size, int bufferSize = BenchmarkData.MiB)
    {
        var path = Temp.GetFilePath();
        using (var write = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
            BenchmarkData.WriteDeterministic(write, size, bufferSize);

        return path;
    }

    /// <summary>
    /// Writes a seeded plaintext file of <paramref name="size" /> bytes under <see cref="Temp" /> and opens a readable <see cref="FileStream" />. The caller owns the stream;
    /// the session dispose path deletes the file.
    /// </summary>
    protected FileStream CreateSeededFile(long size, int bufferSize = BenchmarkData.MiB)
        => new(CreateSeededFilePath(size, bufferSize), FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);

    /// <summary>Allocates a new path under <see cref="IterationTemp" /> (the caller or API creates the file).</summary>
    protected string CreateIterationOutputPath() => IterationTemp.GetFilePath();

    /// <summary>Opens a read/write file under <see cref="IterationTemp" /> for encrypt/compress/decrypt/decompress output. The iteration sub-session disposes it.</summary>
    protected FileStream CreateIterationOutputStream(int bufferSize = BenchmarkData.MiB)
    {
        var path = CreateIterationOutputPath();
        return new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
    }

    /// <summary>Allocates a new path under <see cref="Temp" /> for setup artifacts.</summary>
    protected string CreateTempOutputPath() => Temp.GetFilePath();

    /// <summary>Opens a read/write file under <see cref="Temp" /> for setup artifacts (already-encrypted or already-compressed inputs).</summary>
    protected FileStream CreateTempOutputStream(int bufferSize = BenchmarkData.MiB)
    {
        var path = CreateTempOutputPath();
        return new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
    }

    private void DisposeIterationTemp()
    {
        _iterationTemp?.Dispose();
        _iterationTemp = null;
    }
}