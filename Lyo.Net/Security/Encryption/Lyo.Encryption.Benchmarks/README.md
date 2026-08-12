# Encryption Benchmarks

BenchmarkDotNet performance suite for **`Lyo.Encryption`** and add-ons: **AES-GCM**, **ChaCha20-Poly1305**, **AES-CCM**, **AES-SIV**, **XChaCha20-Poly1305**, **RSA**, **AES-GCM-RSA
hybrid**, two-key envelope, and streaming.

**Latest results:** [HTML benchmark dashboard](../../../docs/benchmarks/encryption.html) · [BENCHMARK_SUMMARY.md](./BENCHMARK_SUMMARY.md) (refresh pointer)

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

| Class                                   | What it measures                                           |
|-----------------------------------------|------------------------------------------------------------|
| `AesGcmEncryptionBenchmarks`            | AES-GCM encrypt/decrypt @ 100 / 250 / 500 MiB              |
| `ChaCha20Poly1305EncryptionBenchmarks`  | ChaCha20-Poly1305 @ 100 / 250 / 500 MiB                    |
| `AesCcmEncryptionBenchmarks`            | AES-CCM @ 100 / 250 / 500 MiB                              |
| `AesSivEncryptionBenchmarks`            | AES-SIV @ 100 / 250 / 500 MiB                              |
| `XChaCha20Poly1305EncryptionBenchmarks` | XChaCha @ 100 / 250 / 500 MiB (explicit key)               |
| `RsaEncryptionBenchmarks`               | RSA 2048 OAEP-SHA256 @ 1 KB, 64 KB, 1 MB (asymmetric path) |
| `AesGcmRsaEncryptionBenchmarks`         | Hybrid RSA + AES-GCM @ 100 / 250 / 500 MiB                 |
| `TwoKeyEncryptionBenchmarks`            | Envelope (DEK/KEK) AES + ChaCha @ 100 MiB–2 GiB streaming  |
| `LargeFileStreamingBenchmarks`          | Stream API AES-GCM / ChaCha / AES-SIV @ 100 MiB–2 GiB      |
| `AlgorithmComparisonBenchmarks`         | Side-by-side all five symmetric AEAD @ 100 / 250 / 500 MiB |

## Headline results (June 2026, this hardware)

| Workload              | Fastest                        | Notes                                               |
|-----------------------|--------------------------------|-----------------------------------------------------|
| Encrypt 1 MB          | **AES-GCM 667 µs** (~1.5 GB/s) | ChaCha 920 µs; XChaCha 2.5 ms; CCM 12 ms; SIV 17 ms |
| Decrypt 1 MB          | **AES-GCM 621 µs** (~1.6 GB/s) | ChaCha 899 µs                                       |
| Stream encrypt 100 MB | **AES-GCM 114 ms** (~873 MB/s) | ChaCha 133 ms                                       |
| Hybrid encrypt 1 MB   | **692 µs**                     | Near pure GCM; RSA wrap amortized                   |
| RSA decrypt 1 MB      | **2.51 s**                     | Not for bulk data                                   |
| Two-key encrypt 1 MB  | **880 µs** (AES)               | ~1.3× single-key                                    |

See [HTML benchmark dashboard](../../../docs/benchmarks/encryption.html) for live tables, ratios, and recommendations (auto-generated from CSV artifacts).

## Output

Results appear in the console and under `BenchmarkDotNet.Artifacts/results/` (Markdown, CSV, HTML). Refresh the HTML dashboard:

```bash
python3 scripts/benchmarks/build_manifests.py --encryption-only
```

Optional future runs may add `--exporters json`; v1 reads CSV reports.

## Requirements

- .NET 10.0 SDK
- BenchmarkDotNet 0.15.8

## Notes

- Always run in **Release** mode
- Payloads use a shared deterministic seed (`BenchmarkData.PayloadSeed`) so runs are comparable across algorithms and machines
- All suites inherit `LyoBenchmarkBase`: per-suite `IIOTempService` + `IIOTempSession`; encrypt/decrypt file I/O stays under that session
- Streaming suites (100 MiB–2 GiB) need multi‑GiB free disk under the IOTemp root; the full ladder is slow by design
- `[MemoryDiagnoser]` enabled on all classes
- `AlgorithmComparisonBenchmarks` @ 500 MiB may fail on memory-constrained runs — prefer streaming classes or smaller Params filters

## Dependencies

*(From `Lyo.Encryption.Benchmarks.csproj`.)*

**Target framework:** `net10.0`

| Package           | Version  |
|-------------------|----------|
| `BenchmarkDotNet` | `0.15.8` |

**Project references:** `Lyo.Encryption`, `Lyo.Encryption.AesCcm`, `Lyo.Encryption.AesSiv`, `Lyo.Encryption.XChaCha20Poly1305`, `Lyo.Encryption.Rsa`, `Lyo.Encryption.AesGcmRsa`,
`Lyo.KeyStore`
