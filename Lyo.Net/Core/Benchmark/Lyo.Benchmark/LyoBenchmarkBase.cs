using BenchmarkDotNet.Attributes;
using Lyo.Benchmark.Data;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;

namespace Lyo.Benchmark;

/// <summary>
/// Base for Lyo BenchmarkDotNet suites: owns a per-suite <see cref="IIOTempService" /> and root <see cref="IIOTempSession" />, and invokes <see cref="OnGlobalSetup" /> /
/// <see cref="OnGlobalCleanup" /> hooks so derived classes do not manage IOTemp lifecycle themselves.
/// </summary>
public abstract class LyoBenchmarkBase
{
    private bool _globalSetupDone;
    private IIOTempSession? _iterationTemp;

    /// <summary>Per-suite IOTemp service. Dispose is handled by this base; suites may call service APIs when needed.</summary>
    protected IIOTempService TempService { get; private set; } = null!;

    /// <summary>Root session under <see cref="TempService" />. All suite file I/O must use paths from this session (or a sub-session).</summary>
    protected IIOTempSession Temp { get; private set; } = null!;

    /// <summary>Returns (and lazily creates) a sub-session for the current benchmark iteration. Cleared automatically in <see cref="BenchmarkIterationCleanup" />.</summary>
    protected IIOTempSession IterationTemp {
        get {
            _iterationTemp ??= Temp.CreateSubSession();
            return _iterationTemp;
        }
    }

    /// <summary>
    /// Creates <see cref="TempService" /> and <see cref="Temp" />, then calls <see cref="OnGlobalSetup" />. Derived suites must not declare an untargeted
    /// <see cref="GlobalSetupAttribute" />; override <see cref="OnGlobalSetup" /> instead. Targeted <c>[GlobalSetup(Target = ...)]</c> / <c>Targets</c> methods are allowed for
    /// per-benchmark prep — those methods must call <see cref="EnsureGlobalSetup" /> first because BenchmarkDotNet may invoke derived targeted setups before this base method.
    /// </summary>
    [GlobalSetup]
    public void BenchmarkGlobalSetup() => EnsureGlobalSetup();

    /// <summary>
    /// Idempotent shared setup (IOTemp + <see cref="OnGlobalSetup" />). Call at the start of every derived <c>[GlobalSetup(Target = ...)]</c> method so prep does not NRE when
    /// BDN runs targeted setup before the base untargeted setup.
    /// </summary>
    protected void EnsureGlobalSetup()
    {
        if (_globalSetupDone)
            return;

        TempService = new IOTempService(
            new() {
                DirectoryName = $"lyo-bench-{Guid.NewGuid():N}",
                EnableMetrics = false,
                // Streaming suites write up to 2 GiB plaintext plus ciphertext/compressed siblings and iteration outputs.
                MaxFileSizeBytes = 8L * BenchmarkData.MiB * 1024,
                MaxTotalSizeBytes = 64L * BenchmarkData.MiB * 1024
            });

        Temp = TempService.CreateSession();
        OnGlobalSetup();
        _globalSetupDone = true;
    }

    /// <summary>Calls <see cref="OnGlobalCleanup" />, disposes any iteration sub-session, then disposes <see cref="Temp" /> and <see cref="TempService" /> (wipes the service directory).</summary>
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

    /// <summary>Disposes the current iteration sub-session so per-iteration outputs do not accumulate across BenchmarkDotNet iterations.</summary>
    [IterationCleanup]
    public void BenchmarkIterationCleanup() => DisposeIterationTemp();

    /// <summary>
    /// Suite-specific setup after IOTemp is ready. Prefer this over an untargeted derived <c>[GlobalSetup]</c>. Use targeted <c>[GlobalSetup(Target = ...)]</c> on the derived
    /// type when only some benchmarks need expensive prep, and call <see cref="EnsureGlobalSetup" /> at the start of each targeted method.
    /// </summary>
    protected virtual void OnGlobalSetup() { }

    /// <summary>Suite-specific cleanup before IOTemp is disposed. Dispose open streams that hold files under <see cref="Temp" /> here.</summary>
    protected virtual void OnGlobalCleanup() { }

    /// <summary>Writes a seeded plaintext file of <paramref name="size" /> bytes under <see cref="Temp" /> and returns its path. The file is deleted when the session is disposed.</summary>
    protected string CreateSeededFilePath(long size, int bufferSize = BenchmarkData.MiB)
    {
        var path = Temp.GetFilePath();
        using (var write = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
            BenchmarkData.WriteDeterministic(write, size, bufferSize);

        return path;
    }

    /// <summary>
    /// Creates a seeded plaintext file of <paramref name="size" /> bytes under <see cref="Temp" /> and returns a readable <see cref="FileStream" />. The caller owns the stream;
    /// the file is deleted when the session is disposed.
    /// </summary>
    protected FileStream CreateSeededFile(long size, int bufferSize = BenchmarkData.MiB)
        => new(CreateSeededFilePath(size, bufferSize), FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);

    /// <summary>Returns a new output path under <see cref="IterationTemp" /> (file created by the caller / API).</summary>
    protected string CreateIterationOutputPath() => IterationTemp.GetFilePath();

    /// <summary>Opens a new read/write file under <see cref="IterationTemp" /> for encrypt/compress/decrypt/decompress outputs. Disposed with the iteration sub-session.</summary>
    protected FileStream CreateIterationOutputStream(int bufferSize = BenchmarkData.MiB)
    {
        var path = CreateIterationOutputPath();
        return new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
    }

    /// <summary>Returns a new path under <see cref="Temp" /> for setup artifacts.</summary>
    protected string CreateTempOutputPath() => Temp.GetFilePath();

    /// <summary>Opens a new read/write file under <see cref="Temp" /> for setup artifacts (pre-encrypted / pre-compressed inputs).</summary>
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