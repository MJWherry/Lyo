using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

/// <summary>Benchmarks for large file encryption/decryption using streaming APIs</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class LargeFileStreamingBenchmarks
{
    private const string KeyId = "benchmark-key";
    private const int ChunkSize = 1024 * 1024; // 1 MB chunks
    private AesGcmEncryptionService _aesGcmService = null!;
    private ChaCha20Poly1305EncryptionService _chachaService = null!;
    private Stream _data100MB = null!;
    private Stream _data1GB = null!;
    private Stream _data2GB = null!;
    private Stream _encrypted100MBAesGcm = null!;
    private Stream _encrypted100MBChacha = null!;
    private Stream _encrypted1GBAesGcm = null!;
    private Stream _encrypted1GBChacha = null!;
    private Stream _encrypted2GBAesGcm = null!;
    private Stream _encrypted2GBChacha = null!;
    private LocalKeyStore _keyStore = null!;

    [GlobalSetup]
    public void Setup()
    {
        _keyStore = new();
        _keyStore.UpdateKeyFromString(KeyId, "benchmark-test-key-32-bytes-long!");
        _aesGcmService = new(_keyStore);
        _chachaService = new(_keyStore);

        // Create test data streams (using FileStream for very large files to avoid memory issues)
        _data100MB = CreateTestDataStream(100 * 1024 * 1024); // 100 MB
        _data1GB = CreateTestDataStream(1024 * 1024 * 1024); // 1 GB
        _data2GB = CreateTestDataStream(2L * 1024 * 1024 * 1024); // 2 GB

        // Pre-encrypt data for decryption benchmarks
        // Use FileStream for large encrypted files (1GB+) to avoid MemoryStream size limits
        _encrypted100MBAesGcm = new MemoryStream();
        _encrypted1GBAesGcm = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted2GBAesGcm = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted100MBChacha = new MemoryStream();
        _encrypted1GBChacha = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted2GBChacha = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _data100MB.Position = 0;
        _aesGcmService.EncryptToStreamAsync(_data100MB, _encrypted100MBAesGcm, KeyId, chunkSize: ChunkSize).Wait();
        _data1GB.Position = 0;
        _aesGcmService.EncryptToStreamAsync(_data1GB, _encrypted1GBAesGcm, KeyId, chunkSize: ChunkSize).Wait();
        _data2GB.Position = 0;
        _aesGcmService.EncryptToStreamAsync(_data2GB, _encrypted2GBAesGcm, KeyId, chunkSize: ChunkSize).Wait();
        _data100MB.Position = 0;
        _chachaService.EncryptToStreamAsync(_data100MB, _encrypted100MBChacha, KeyId, chunkSize: ChunkSize).Wait();
        _data1GB.Position = 0;
        _chachaService.EncryptToStreamAsync(_data1GB, _encrypted1GBChacha, KeyId, chunkSize: ChunkSize).Wait();
        _data2GB.Position = 0;
        _chachaService.EncryptToStreamAsync(_data2GB, _encrypted2GBChacha, KeyId, chunkSize: ChunkSize).Wait();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _data100MB?.Dispose();
        _data1GB?.Dispose();
        _data2GB?.Dispose();
        _encrypted100MBAesGcm?.Dispose();
        _encrypted1GBAesGcm?.Dispose();
        _encrypted2GBAesGcm?.Dispose();
        _encrypted100MBChacha?.Dispose();
        _encrypted1GBChacha?.Dispose();
        _encrypted2GBChacha?.Dispose();
    }

    private Stream CreateTestDataStream(long size)
    {
        // For very large files (1GB+), use a FileStream to avoid memory issues
        if (size >= 1024 * 1024 * 1024) // 1 GB or larger
        {
            var tempFile = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFile)) {
                var buffer = new byte[1024 * 1024]; // 1 MB buffer
                var rng = RandomNumberGenerator.Create();
                var remaining = size;
                while (remaining > 0) {
                    var toWrite = (int)Math.Min(remaining, buffer.Length);
                    rng.GetBytes(buffer, 0, toWrite);
                    fileStream.Write(buffer, 0, toWrite);
                    remaining -= toWrite;
                }
            }

            return new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.DeleteOnClose);
        }

        // For smaller files, use MemoryStream
        var data = new byte[size];
        RandomNumberGenerator.Fill(data);
        return new MemoryStream(data);
    }

    // AES-GCM Encryption Benchmarks
    [Benchmark]
    public async Task Encrypt_AesGcm_100MB()
    {
        _data100MB.Position = 0;
        var output = new MemoryStream();
        await _aesGcmService.EncryptToStreamAsync(_data100MB, output, KeyId, chunkSize: ChunkSize);
    }

    [Benchmark]
    public async Task Encrypt_AesGcm_1GB()
    {
        _data1GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _aesGcmService.EncryptToStreamAsync(_data1GB, output, KeyId, chunkSize: ChunkSize);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Encrypt_AesGcm_2GB()
    {
        _data2GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _aesGcmService.EncryptToStreamAsync(_data2GB, output, KeyId, chunkSize: ChunkSize);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    // AES-GCM Decryption Benchmarks
    [Benchmark]
    public async Task Decrypt_AesGcm_100MB()
    {
        _encrypted100MBAesGcm.Position = 0;
        var output = new MemoryStream();
        await _aesGcmService.DecryptToStreamAsync(_encrypted100MBAesGcm, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_AesGcm_1GB()
    {
        _encrypted1GBAesGcm.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _aesGcmService.DecryptToStreamAsync(_encrypted1GBAesGcm, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Decrypt_AesGcm_2GB()
    {
        _encrypted2GBAesGcm.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _aesGcmService.DecryptToStreamAsync(_encrypted2GBAesGcm, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    // ChaCha20Poly1305 Encryption Benchmarks
    [Benchmark]
    public async Task Encrypt_ChaCha_100MB()
    {
        _data100MB.Position = 0;
        var output = new MemoryStream();
        await _chachaService.EncryptToStreamAsync(_data100MB, output, KeyId, chunkSize: ChunkSize);
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_1GB()
    {
        _data1GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _chachaService.EncryptToStreamAsync(_data1GB, output, KeyId, chunkSize: ChunkSize);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_2GB()
    {
        _data2GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _chachaService.EncryptToStreamAsync(_data2GB, output, KeyId, chunkSize: ChunkSize);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    // ChaCha20Poly1305 Decryption Benchmarks
    [Benchmark]
    public async Task Decrypt_ChaCha_100MB()
    {
        _encrypted100MBChacha.Position = 0;
        var output = new MemoryStream();
        await _chachaService.DecryptToStreamAsync(_encrypted100MBChacha, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_ChaCha_1GB()
    {
        _encrypted1GBChacha.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _chachaService.DecryptToStreamAsync(_encrypted1GBChacha, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Decrypt_ChaCha_2GB()
    {
        _encrypted2GBChacha.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _chachaService.DecryptToStreamAsync(_encrypted2GBChacha, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
        }
    }
}