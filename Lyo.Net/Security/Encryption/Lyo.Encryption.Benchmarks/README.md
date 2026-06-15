# Encryption Benchmarks

BenchmarkDotNet performance suite for **`Lyo.Encryption`** and add-ons: **AES-GCM**, **ChaCha20-Poly1305**, **AES-CCM**, **AES-SIV**, **XChaCha20-Poly1305**, **RSA**, **AES-GCM-RSA hybrid**, two-key envelope, and streaming.

**Latest results:** [BENCHMARK_SUMMARY.md](./BENCHMARK_SUMMARY.md) (June 14, 2026, .NET 10.0.9, Intel Core Ultra 7 155U, AES-NI).

## Running benchmarks

### Run all benchmarks

```bash
dotnet run -c Release --project Lyo.Encryption.Benchmarks
```

### Run specific benchmark class

```bash
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*AesGcmEncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*ChaCha20Poly1305EncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*AesCcmEncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*AesSivEncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*XChaCha20Poly1305EncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*RsaEncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*AesGcmRsaEncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*TwoKeyEncryptionBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*LargeFileStreamingBenchmarks*"
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --filter "*AlgorithmComparisonBenchmarks*"
```

### Shorter run (smoke test)

```bash
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --job short --filter "*AesGcmEncryptionBenchmarks*"
```

## Benchmark classes

| Class | What it measures |
|-------|------------------|
| `AesGcmEncryptionBenchmarks` | AES-GCM encrypt/decrypt @ 1 KB, 1 MB, 10 MB |
| `ChaCha20Poly1305EncryptionBenchmarks` | ChaCha20-Poly1305 @ 1 KB, 1 MB, 10 MB |
| `AesCcmEncryptionBenchmarks` | AES-CCM @ 1 KB, 1 MB, 10 MB |
| `AesSivEncryptionBenchmarks` | AES-SIV @ 1 KB, 1 MB, 10 MB |
| `XChaCha20Poly1305EncryptionBenchmarks` | XChaCha @ 1 KB, 1 MB, 10 MB (explicit key) |
| `RsaEncryptionBenchmarks` | RSA 2048 OAEP-SHA256 @ 1 KB, 64 KB, 1 MB |
| `AesGcmRsaEncryptionBenchmarks` | Hybrid RSA + AES-GCM @ 1 KB, 1 MB, 10 MB |
| `TwoKeyEncryptionBenchmarks` | Envelope (DEK/KEK) AES + ChaCha @ 1 KB–2 GB |
| `LargeFileStreamingBenchmarks` | Stream API AES + ChaCha @ 100 MB, 1 GB, 2 GB |
| `AlgorithmComparisonBenchmarks` | Side-by-side all five symmetric AEAD @ 1 KB–100 MB |

## Headline results (June 2026, this hardware)

| Workload | Fastest | Notes |
|----------|---------|-------|
| Encrypt 1 MB | **AES-GCM 667 µs** (~1.5 GB/s) | ChaCha 920 µs; XChaCha 2.5 ms; CCM 12 ms; SIV 17 ms |
| Decrypt 1 MB | **AES-GCM 621 µs** (~1.6 GB/s) | ChaCha 899 µs |
| Stream encrypt 100 MB | **AES-GCM 114 ms** (~873 MB/s) | ChaCha 133 ms |
| Hybrid encrypt 1 MB | **692 µs** | Near pure GCM; RSA wrap amortized |
| RSA decrypt 1 MB | **2.51 s** | Not for bulk data |
| Two-key encrypt 1 MB | **880 µs** (AES) | ~1.3× single-key |

See [BENCHMARK_SUMMARY.md](./BENCHMARK_SUMMARY.md) for full tables, ratios, and recommendations.

## Output

Results appear in the console and under `BenchmarkDotNet.Artifacts/results/` (Markdown, CSV, HTML).

```bash
dotnet run -c Release --project Lyo.Encryption.Benchmarks -- --exporters Markdown Html
```

## Requirements

- .NET 10.0 SDK
- BenchmarkDotNet 0.15.8

## Notes

- Always run in **Release** mode
- Payloads use `RandomNumberGenerator.Fill`
- `[MemoryDiagnoser]` enabled on all classes
- `AlgorithmComparisonBenchmarks` @ 100 MB may fail on memory-constrained runs — use dedicated classes instead

## Dependencies

*(From `Lyo.Encryption.Benchmarks.csproj`.)*

**Target framework:** `net10.0`

| Package | Version |
|---------|---------|
| `BenchmarkDotNet` | `0.15.8` |

**Project references:** `Lyo.Encryption`, `Lyo.Encryption.AesCcm`, `Lyo.Encryption.AesSiv`, `Lyo.Encryption.XChaCha20Poly1305`, `Lyo.Encryption.Rsa`, `Lyo.Encryption.AesGcmRsa`, `Lyo.Keystore`
