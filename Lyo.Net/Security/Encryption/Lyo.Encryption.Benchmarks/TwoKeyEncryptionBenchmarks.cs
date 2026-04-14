using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class TwoKeyEncryptionBenchmarks
{
    private const string KeyId = "benchmark-key";
    private TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService> _aesGcmService = null!;
    private TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, ChaCha20Poly1305EncryptionService> _chachaService = null!;
    private Stream _data100MB = null!;
    private Stream _data1GB = null!;
    private Stream _data2GB = null!;
    private Stream _encrypted100MBAesGcm = null!;
    private Stream _encrypted100MBChacha = null!;
    private Stream _encrypted1GBAesGcm = null!;
    private Stream _encrypted1GBChacha = null!;
    private Stream _encrypted2GBAesGcm = null!;
    private Stream _encrypted2GBChacha = null!;
    private MemoryStream _encryptedLargeAesGcm = null!;
    private MemoryStream _encryptedLargeChacha = null!;
    private MemoryStream _encryptedMediumAesGcm = null!;
    private MemoryStream _encryptedMediumChacha = null!;
    private MemoryStream _encryptedSmallAesGcm = null!;
    private MemoryStream _encryptedSmallChacha = null!;
    private LocalKeyStore _keyStore = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _keyStore = new();
        _keyStore.UpdateKeyFromString(KeyId, "benchmark-test-key-32-bytes-long!");
        var aesGcmDek = new AesGcmEncryptionService(_keyStore);
        var aesGcmKek = new AesGcmEncryptionService(_keyStore);
        _aesGcmService = new(aesGcmDek, aesGcmKek, _keyStore);
        var chachaDek = new ChaCha20Poly1305EncryptionService(_keyStore);
        var chachaKek = new ChaCha20Poly1305EncryptionService(_keyStore);
        _chachaService = new(chachaDek, chachaKek, _keyStore);

        // Generate test data
        _smallData = new byte[1024]; // 1 KB
        _mediumData = new byte[1024 * 1024]; // 1 MB
        _largeData = new byte[10 * 1024 * 1024]; // 10 MB
        _data100MB = CreateTestDataStream(100 * 1024 * 1024); // 100 MB
        _data1GB = CreateTestDataStream(1024L * 1024 * 1024); // 1 GB
        _data2GB = CreateTestDataStream(2L * 1024 * 1024 * 1024); // 2 GB
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);

        // Pre-encrypt data for decryption benchmarks
        _encryptedSmallAesGcm = new();
        _encryptedMediumAesGcm = new();
        _encryptedLargeAesGcm = new();
        _encryptedSmallChacha = new();
        _encryptedMediumChacha = new();
        _encryptedLargeChacha = new();
        _aesGcmService.EncryptToStreamAsync(new MemoryStream(_smallData), _encryptedSmallAesGcm, KeyId).Wait();
        _aesGcmService.EncryptToStreamAsync(new MemoryStream(_mediumData), _encryptedMediumAesGcm, KeyId).Wait();
        _aesGcmService.EncryptToStreamAsync(new MemoryStream(_largeData), _encryptedLargeAesGcm, KeyId).Wait();
        _chachaService.EncryptToStreamAsync(new MemoryStream(_smallData), _encryptedSmallChacha, KeyId).Wait();
        _chachaService.EncryptToStreamAsync(new MemoryStream(_mediumData), _encryptedMediumChacha, KeyId).Wait();
        _chachaService.EncryptToStreamAsync(new MemoryStream(_largeData), _encryptedLargeChacha, KeyId).Wait();

        // Pre-encrypt large files
        _encrypted100MBAesGcm = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted1GBAesGcm = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted2GBAesGcm = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted100MBChacha = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted1GBChacha = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _encrypted2GBChacha = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _data100MB.Position = 0;
        _aesGcmService.EncryptToStreamAsync(_data100MB, _encrypted100MBAesGcm, KeyId).Wait();
        _data1GB.Position = 0;
        _aesGcmService.EncryptToStreamAsync(_data1GB, _encrypted1GBAesGcm, KeyId).Wait();
        _data2GB.Position = 0;
        _aesGcmService.EncryptToStreamAsync(_data2GB, _encrypted2GBAesGcm, KeyId).Wait();
        _data100MB.Position = 0;
        _chachaService.EncryptToStreamAsync(_data100MB, _encrypted100MBChacha, KeyId).Wait();
        _data1GB.Position = 0;
        _chachaService.EncryptToStreamAsync(_data1GB, _encrypted1GBChacha, KeyId).Wait();
        _data2GB.Position = 0;
        _chachaService.EncryptToStreamAsync(_data2GB, _encrypted2GBChacha, KeyId).Wait();
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

    [Benchmark]
    public async Task Encrypt_AesGcm_1KB()
    {
        var input = new MemoryStream(_smallData);
        var output = new MemoryStream();
        await _aesGcmService.EncryptToStreamAsync(input, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_AesGcm_1MB()
    {
        var input = new MemoryStream(_mediumData);
        var output = new MemoryStream();
        await _aesGcmService.EncryptToStreamAsync(input, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_AesGcm_10MB()
    {
        var input = new MemoryStream(_largeData);
        var output = new MemoryStream();
        await _aesGcmService.EncryptToStreamAsync(input, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_1KB()
    {
        var input = new MemoryStream(_smallData);
        var output = new MemoryStream();
        await _chachaService.EncryptToStreamAsync(input, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_1MB()
    {
        var input = new MemoryStream(_mediumData);
        var output = new MemoryStream();
        await _chachaService.EncryptToStreamAsync(input, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_10MB()
    {
        var input = new MemoryStream(_largeData);
        var output = new MemoryStream();
        await _chachaService.EncryptToStreamAsync(input, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_AesGcm_1KB()
    {
        _encryptedSmallAesGcm.Position = 0;
        var output = new MemoryStream();
        await _aesGcmService.DecryptToStreamAsync(_encryptedSmallAesGcm, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_AesGcm_1MB()
    {
        _encryptedMediumAesGcm.Position = 0;
        var output = new MemoryStream();
        await _aesGcmService.DecryptToStreamAsync(_encryptedMediumAesGcm, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_AesGcm_10MB()
    {
        _encryptedLargeAesGcm.Position = 0;
        var output = new MemoryStream();
        await _aesGcmService.DecryptToStreamAsync(_encryptedLargeAesGcm, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_ChaCha_1KB()
    {
        _encryptedSmallChacha.Position = 0;
        var output = new MemoryStream();
        await _chachaService.DecryptToStreamAsync(_encryptedSmallChacha, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_ChaCha_1MB()
    {
        _encryptedMediumChacha.Position = 0;
        var output = new MemoryStream();
        await _chachaService.DecryptToStreamAsync(_encryptedMediumChacha, output, KeyId);
    }

    [Benchmark]
    public async Task Decrypt_ChaCha_10MB()
    {
        _encryptedLargeChacha.Position = 0;
        var output = new MemoryStream();
        await _chachaService.DecryptToStreamAsync(_encryptedLargeChacha, output, KeyId);
    }

    // Large file benchmarks (100MB, 1GB, 2GB)
    [Benchmark]
    public async Task Encrypt_AesGcm_100MB()
    {
        _data100MB.Position = 0;
        var output = new MemoryStream();
        await _aesGcmService.EncryptToStreamAsync(_data100MB, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_AesGcm_1GB()
    {
        _data1GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _aesGcmService.EncryptToStreamAsync(_data1GB, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
            File.Delete(output.Name);
        }
    }

    [Benchmark]
    public async Task Encrypt_AesGcm_2GB()
    {
        _data2GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _aesGcmService.EncryptToStreamAsync(_data2GB, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
            File.Delete(output.Name);
        }
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_100MB()
    {
        _data100MB.Position = 0;
        var output = new MemoryStream();
        await _chachaService.EncryptToStreamAsync(_data100MB, output, KeyId);
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_1GB()
    {
        _data1GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _chachaService.EncryptToStreamAsync(_data1GB, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
            File.Delete(output.Name);
        }
    }

    [Benchmark]
    public async Task Encrypt_ChaCha_2GB()
    {
        _data2GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _chachaService.EncryptToStreamAsync(_data2GB, output, KeyId);
        }
        finally {
            await output.DisposeAsync();
            File.Delete(output.Name);
        }
    }

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
            File.Delete(output.Name);
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
            File.Delete(output.Name);
        }
    }

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
            File.Delete(output.Name);
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
            File.Delete(output.Name);
        }
    }
}