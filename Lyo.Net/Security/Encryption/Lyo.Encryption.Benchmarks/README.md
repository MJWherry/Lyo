# Encryption Benchmarks

This project contains performance benchmarks for the Lyo.Encryption library
using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Running Benchmarks

### Run all benchmarks

```bash
dotnet run -c Release --project Lyo.Encryption.Benchmarks
```

### Run specific benchmark class

```bash
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*AesGcmEncryptionBenchmarks*"
dotnet run -c Release -p Lyo.Encryption.Benchmarks -- --filter "*ChaCha20Poly1305EncryptionBenchmarks*"
dotnet run -c Release -p Lyo.Encryption.Benchmarks -- --filter "*TwoKeyEncryptionBenchmarks*"
dotnet run -c Release -p Lyo.Encryption.Benchmarks -- --filter "*AlgorithmComparisonBenchmarks*"
```

### Run specific benchmark method

```bash
dotnet run -c Release -p Lyo.Encryption.Benchmarks -- --filter "*Encrypt_1KB*"
```

## Benchmark Classes

### AesGcmEncryptionBenchmarks

Benchmarks for AES-GCM encryption and decryption operations:

- Encrypt/Decrypt 1 KB
- Encrypt/Decrypt 1 MB
- Encrypt/Decrypt 10 MB

### ChaCha20Poly1305EncryptionBenchmarks

Benchmarks for ChaCha20Poly1305 encryption and decryption operations:

- Encrypt/Decrypt 1 KB
- Encrypt/Decrypt 1 MB
- Encrypt/Decrypt 10 MB

### TwoKeyEncryptionBenchmarks

Benchmarks for TwoKey (envelope) encryption using streaming:

- AES-GCM TwoKey encryption/decryption (1 KB, 1 MB, 10 MB, 100 MB, 1 GB, 2 GB)
- ChaCha20Poly1305 TwoKey encryption/decryption (1 KB, 1 MB, 10 MB, 100 MB, 1 GB, 2 GB)

### LargeFileStreamingBenchmarks

Benchmarks for large file encryption/decryption using streaming APIs:

- AES-GCM streaming encryption/decryption (100 MB, 1 GB, 2 GB)
- ChaCha20Poly1305 streaming encryption/decryption (100 MB, 1 GB, 2 GB)
- Uses FileStream for very large files (1GB+) to avoid memory issues

### AlgorithmComparisonBenchmarks

Comparative benchmarks between AES-GCM and ChaCha20Poly1305:

- Encryption performance comparison
- Decryption performance comparison
- Tests with multiple data sizes (1 KB, 1 MB, 10 MB, 100 MB)

## Output

Benchmark results are displayed in the console and can be exported to:

- Markdown
- CSV
- HTML
- JSON

Use `--exporters` flag to specify output formats:

```bash
dotnet run -c Release -p Lyo.Encryption.Benchmarks -- --exporters Markdown Html
```

## Requirements

- .NET 10.0 SDK
- BenchmarkDotNet 0.13.12+

## Notes

- Always run benchmarks in Release mode for accurate results
- Benchmarks use randomly generated test data
- Memory diagnostics are enabled to track allocations
- Results may vary based on hardware and system load

## Performance Analysis

### Test Environment

- **Platform:** Linux Mint 22.1 (Xia)
- **CPU:** Intel Core Ultra 7 155U 0.40GHz (14 logical cores, 12 physical cores)
- **Runtime:** .NET 10.0.0 (X64 RyuJIT x86-64-v3)
- **Benchmark Tool:** BenchmarkDotNet v0.15.8

### Algorithm Comparison Summary

#### Small Files (1 KB)

Both algorithms perform similarly for small data:

- **AES-GCM**: 2.423 μs (baseline)
- **ChaCha20-Poly1305**: 2.536 μs (4.7% slower)
- **Memory**: ~4.09 KB allocated (encryption), ~2.73 KB (decryption)

#### Medium Files (1 MB)

AES-GCM shows significant performance advantage:

- **AES-GCM Encryption**: 686.020 μs (~1,528 MB/s)
- **ChaCha20-Poly1305 Encryption**: 928.813 μs (~1,129 MB/s)
- **AES-GCM is 35% faster** for encryption
- **AES-GCM Decryption**: 548.549 μs (~1,911 MB/s)
- **ChaCha20-Poly1305 Decryption**: 820.834 μs (~1,277 MB/s)
- **AES-GCM is 50% faster** for decryption

#### Large Files (10 MB)

AES-GCM maintains performance lead:

- **AES-GCM Encryption**: 10,605.409 μs (~989 MB/s)
- **ChaCha20-Poly1305 Encryption**: 13,907.612 μs (~754 MB/s)
- **AES-GCM is 31% faster** for encryption
- **AES-GCM Decryption**: 7,774.611 μs (~1,349 MB/s)
- **ChaCha20-Poly1305 Decryption**: 10,870.966 μs (~965 MB/s)
- **AES-GCM is 40% faster** for decryption

#### Very Large Files (100 MB)

AES-GCM demonstrates strongest advantage:

- **AES-GCM Encryption**: 53,033.444 μs (~1,977 MB/s)
- **ChaCha20-Poly1305 Encryption**: 88,207.391 μs (~1,189 MB/s)
- **AES-GCM is 66% faster** for encryption
- **AES-GCM Decryption**: 46,661.590 μs (~2,247 MB/s)
- **ChaCha20-Poly1305 Decryption**: 77,513.125 μs (~1,353 MB/s)
- **AES-GCM is 66% faster** for decryption

### Large File Streaming Performance

#### AES-GCM Streaming

| Size   | Encrypt Time | Encrypt Throughput | Decrypt Time | Decrypt Throughput | Memory Ratio |
|--------|--------------|--------------------|--------------|--------------------|--------------|
| 100 MB | 145.6 ms     | ~688 MB/s          | 110.6 ms     | ~906 MB/s          | 6.55x        |
| 1 GB   | 5,480.9 ms   | ~196 MB/s          | 5,491.5 ms   | ~195 MB/s          | 4.10x        |
| 2 GB   | 10,136.0 ms  | ~212 MB/s          | 9,339.7 ms   | ~230 MB/s          | 4.10x        |

#### ChaCha20-Poly1305 Streaming

| Size   | Encrypt Time | Encrypt Throughput | Decrypt Time | Decrypt Throughput | Memory Ratio |
|--------|--------------|--------------------|--------------|--------------------|--------------|
| 100 MB | 162.5 ms     | ~617 MB/s          | 149.3 ms     | ~671 MB/s          | 6.55x        |
| 1 GB   | 5,228.5 ms   | ~205 MB/s          | 5,446.9 ms   | ~197 MB/s          | 4.10x        |
| 2 GB   | 11,347.0 ms  | ~189 MB/s          | 8,470.5 ms   | ~253 MB/s          | 4.10x        |

**Key Observations:**

- AES-GCM outperforms ChaCha20-Poly1305 for 100MB files (11-13% faster encryption, 26% faster decryption)
- Performance is similar for 1GB+ files, with ChaCha20-Poly1305 sometimes faster for decryption
- Decryption is generally faster than encryption, especially for 100MB files
- Memory allocation is high (4-6.5x input size) due to streaming buffer overhead

### Two-Key Encryption Performance

Two-key (envelope) encryption adds overhead but enables key rotation:

#### Small Files (1 KB - 10 MB)

| Operation | Size  | AES-GCM Time  | ChaCha Time   | Overhead vs Single-Key |
|-----------|-------|---------------|---------------|------------------------|
| Encrypt   | 1 KB  | 6.791 μs      | 6.938 μs      | ~2.8x slower           |
| Encrypt   | 1 MB  | 972.090 μs    | 1,160.265 μs  | ~1.4x slower           |
| Encrypt   | 10 MB | 15,420.206 μs | 17,675.201 μs | ~1.5x slower           |
| Decrypt   | 1 KB  | 4.438 μs      | 4.628 μs      | ~2.1x slower           |
| Decrypt   | 1 MB  | 761.465 μs    | 1,023.037 μs  | ~1.4x slower           |
| Decrypt   | 10 MB | 13,175.785 μs | 14,983.462 μs | ~1.7x slower           |

#### Large Files (100 MB - 2 GB)

| Operation | Size   | AES-GCM Time | AES-GCM Throughput | ChaCha Time | ChaCha Throughput |
|-----------|--------|--------------|--------------------|-------------|-------------------|
| Encrypt   | 100 MB | 150.6 ms     | ~665 MB/s          | 198.4 ms    | ~505 MB/s         |
| Encrypt   | 1 GB   | 4,846.2 ms   | ~222 MB/s          | 4,627.6 ms  | ~232 MB/s         |
| Encrypt   | 2 GB   | 10,057.4 ms  | ~214 MB/s          | 10,210.1 ms | ~210 MB/s         |
| Decrypt   | 100 MB | 130.8 ms     | ~765 MB/s          | 157.5 ms    | ~636 MB/s         |
| Decrypt   | 1 GB   | 4,896.4 ms   | ~219 MB/s          | 3,728.9 ms  | ~288 MB/s         |
| Decrypt   | 2 GB   | 9,862.2 ms   | ~218 MB/s          | 10,521.7 ms | ~204 MB/s         |

**Key Findings:**

- Two-key overhead is minimal for large files (similar to single-key performance)
- Overhead is more noticeable for small files (~2-3x slower)
- Memory allocation is higher (5-7x input size) due to envelope encryption format
- ChaCha20-Poly1305 shows better decryption performance for 1GB files in two-key mode

### Memory Allocation Patterns

#### Single-Key Encryption

- **Encryption**: ~3.0x input size (includes format overhead)
    - 1 KB: ~4.09 KB
    - 1 MB: ~3,074 KB
    - 10 MB: ~30,722 KB
    - 100 MB: ~307,201 KB
- **Decryption**: ~2.0x input size
    - 1 KB: ~2.73 KB
    - 1 MB: ~2,050 KB
    - 10 MB: ~20,482 KB
    - 100 MB: ~204,801 KB

#### Streaming Operations

- **Large files (100MB+)**: 4-6.5x input size due to buffer overhead
- **GC Collections**: Scale with file size (400-12,000 Gen0/1/2 collections)
- High GC pressure indicates significant memory pressure for very large files

### Recommendations

#### When to Use AES-GCM

- ✅ General-purpose encryption (best overall performance)
- ✅ Systems with hardware acceleration (AES-NI)
- ✅ Large file operations (100MB+)
- ✅ Production workloads requiring consistent performance
- ✅ When maximum throughput is required

#### When to Use ChaCha20-Poly1305

- ✅ Software-only implementations
- ✅ Systems without hardware acceleration
- ✅ When compatibility with ChaCha20 is required
- ✅ Acceptable performance trade-off for specific use cases
- ✅ Some scenarios show better decryption performance for 1GB+ files

#### When to Use Two-Key Encryption

- ✅ Key rotation scenarios
- ✅ Compliance requirements (envelope encryption)
- ✅ When data encryption keys need to be encrypted separately
- ✅ Large file operations (overhead is minimal)
- ⚠️ Avoid for small files if performance is critical (significant overhead)

### Performance Optimization Opportunities

1. **Memory Allocation**
    - Current: 3-6.5x input size for streaming operations
    - Opportunity: Buffer pooling and reuse could reduce allocations
    - Impact: Lower GC pressure, better throughput for large files

2. **GC Pressure**
    - Current: High GC collections for large files (4,000-12,000 Gen0/1/2)
    - Opportunity: Object pooling and reduced allocation lifetime
    - Impact: Reduced latency spikes, more consistent performance

3. **Small File Performance**
    - Two-key encryption has significant overhead for 1KB files (~2.8x slower)
    - Opportunity: Specialized fast path for small files
    - Impact: Better performance for high-frequency small file operations

### Performance Summary

| Algorithm             | Encryption Speed | Decryption Speed | Memory Usage | Best Use Case                             |
|-----------------------|------------------|------------------|--------------|-------------------------------------------|
| **AES-GCM**           | ⭐⭐⭐⭐⭐ Fastest    | ⭐⭐⭐⭐⭐ Fastest    | ⭐⭐⭐ Moderate | General purpose, hardware acceleration    |
| **ChaCha20-Poly1305** | ⭐⭐⭐⭐ Good        | ⭐⭐⭐⭐ Good        | ⭐⭐⭐ Moderate | Software-only, compatibility requirements |

For detailed benchmark results, see [BENCHMARK_SUMMARY.md](./BENCHMARK_SUMMARY.md).

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Encryption.Benchmarks.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `BenchmarkDotNet` | `0.15.8` |

### Project references

- `Lyo.Encryption`
- `Lyo.Keystore`

## Public API (generated)

Top-level `public` types in `*.cs` (*5*). Nested types and file-scoped namespaces may omit some entries.

- `AesGcmEncryptionBenchmarks`
- `AlgorithmComparisonBenchmarks`
- `ChaCha20Poly1305EncryptionBenchmarks`
- `LargeFileStreamingBenchmarks`
- `TwoKeyEncryptionBenchmarks`

<!-- LYO_README_SYNC:END -->

