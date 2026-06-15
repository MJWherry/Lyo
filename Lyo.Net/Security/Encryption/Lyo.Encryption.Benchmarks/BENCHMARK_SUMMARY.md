# Encryption Benchmarks Summary

## Overview

Performance benchmarks for the Lyo.Encryption library and symmetric add-ons (AES-CCM, AES-SIV, XChaCha20-Poly1305), plus RSA and AES-GCM-RSA hybrid.

**Benchmarked:** AES-GCM, ChaCha20-Poly1305, AES-CCM, AES-SIV, XChaCha20-Poly1305, RSA (2048-bit OAEP-SHA256), AES-GCM-RSA hybrid, two-key envelope, and large-file streaming.

**Test environment (latest run):**

- **Date:** June 14, 2026 @ 19:02 (`BenchmarkRun-20260614-190220.log`)
- **Platform:** Linux Mint 22.1 (Xia)
- **CPU:** Intel Core Ultra 7 155U (14 logical / 12 physical cores, **AES-NI**)
- **Runtime:** .NET 10.0.9 (X64 RyuJIT x86-64-v3)
- **Benchmark tool:** BenchmarkDotNet v0.15.8
- **Data:** Random bytes (`RandomNumberGenerator.Fill`)

Full reports: `BenchmarkDotNet.Artifacts/results/` in this project.

**vs prior run (April 6, 2026, .NET 10.0.0):** AES-GCM and ChaCha dedicated benchmarks are **stable to ~5% faster** at 1–10 MB. No meaningful regression on core symmetric paths. This run adds five new benchmark classes and expands `AlgorithmComparisonBenchmarks` to all symmetric AEAD algorithms.

---

## Benchmark coverage

| Algorithm / pattern | Benchmark class | Unit tests |
|---------------------|:---------------:|:----------:|
| AES-GCM | ✅ `AesGcmEncryptionBenchmarks`, comparison, streaming, two-key | ✅ |
| ChaCha20-Poly1305 | ✅ `ChaCha20Poly1305EncryptionBenchmarks`, comparison, streaming, two-key | ✅ |
| AES-CCM | ✅ `AesCcmEncryptionBenchmarks`, comparison | ✅ |
| AES-SIV | ✅ `AesSivEncryptionBenchmarks`, comparison | ✅ |
| XChaCha20-Poly1305 | ✅ `XChaCha20Poly1305EncryptionBenchmarks`, comparison | ✅ |
| RSA (2048 OAEP-SHA256) | ✅ `RsaEncryptionBenchmarks` | ✅ |
| AES-GCM-RSA hybrid | ✅ `AesGcmRsaEncryptionBenchmarks` | ✅ |
| Two-key envelope (AES or ChaCha DEK/KEK) | ✅ `TwoKeyEncryptionBenchmarks` | ✅ |
| Large-file streaming | ✅ `LargeFileStreamingBenchmarks` | — |

**Note:** XChaCha benchmarks pass an **explicit 32-byte key** (same as unit tests) because KeyStore nonce generation is sized for 12-byte nonces. Throughput reflects the BouncyCastle + HChaCha20 implementation path.

**Note:** `AlgorithmComparisonBenchmarks` @ **100 MB** did not complete in the latest full run (setup OOM/timeout with five algorithms). Use dedicated per-algorithm classes or streaming benchmarks for 100 MB+.

---

## 1. Symmetric algorithm comparison (AES-GCM baseline)

Ratios from `AlgorithmComparisonBenchmarks` (June 14, 2026).

### Encrypt

| Size | AES-GCM | ChaCha | XChaCha | AES-CCM | AES-SIV | ChaCha vs GCM | XChaCha vs GCM | CCM vs GCM | SIV vs GCM |
|------|--------:|-------:|--------:|--------:|--------:|--------------:|---------------:|-----------:|-----------:|
| 1 KB | 2.60 µs | 2.70 µs | 3.25 µs | 12.9 µs | 19.9 µs | 1.04× | 1.25× | 4.96× | 7.64× |
| 1 MB | **712 µs** (~1.5 GB/s) | 967 µs | 2.27 ms | 11.9 ms | 17.2 ms | 1.37× | 3.21× | 16.8× | 24.3× |
| 10 MB | **5.17 ms** (~2.0 GB/s) | 7.42 ms | 20.4 ms | 115 ms | 168 ms | 1.44× | 3.95× | 22.3× | 32.4× |

### Decrypt

| Size | AES-GCM | ChaCha | XChaCha | AES-CCM | AES-SIV |
|------|--------:|-------:|--------:|--------:|--------:|
| 1 KB | 2.31 µs | 2.36 µs | 2.73 µs | 12.4 µs | 19.8 µs |
| 1 MB | **498 µs** (~2.1 GB/s) | 762 µs | 2.29 ms | 11.4 ms | 16.6 ms |
| 10 MB | **4.65 ms** (~2.2 GB/s) | 7.15 ms | 19.6 ms | 113 ms | 165 ms |

**Ranking (fastest → slowest):** AES-GCM ≈ ChaCha @ 1 KB → XChaCha (~3–4× GCM @ 1 MB+) → AES-CCM (~17–22×) → AES-SIV (~24–32×).

On this **AES-NI** CPU, **AES-GCM remains the default** for bulk symmetric encryption. ChaCha is within ~40% for encrypt/decrypt. XChaCha pays for HChaCha20 subkey derivation + BouncyCastle. CCM/SIV use heavier portable or SIV-specific code paths.

---

## 2. Per-algorithm dedicated benchmarks (in-memory)

### AES-GCM

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| Encrypt | 2.58 µs | 667 µs | 8.62 ms |
| Decrypt | 2.30 µs | 621 µs | 5.08 ms |

### ChaCha20-Poly1305

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| Encrypt | 2.72 µs | 920 µs | 11.5 ms |
| Decrypt | 2.41 µs | 899 µs | 8.15 ms |

### AES-CCM

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| Encrypt | 12.5 µs | 12.2 ms | 122 ms |
| Decrypt | 11.9 µs | 11.1 ms | 113 ms |

### AES-SIV

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| Encrypt | 20.3 µs | 17.0 ms | 169 ms |
| Decrypt | 20.0 µs | 16.4 ms | 166 ms |

### XChaCha20-Poly1305 (explicit key)

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| Encrypt | 3.30 µs | 2.54 ms | 26.6 ms |
| Decrypt | 2.80 µs | 2.34 ms | 25.3 ms |

Allocation: ~3× input (encrypt), ~2× (decrypt) for GCM/ChaCha; CCM/XChaCha ~1.67× at 1 MB+ due to BouncyCastle buffers.

---

## 3. RSA & hybrid (2048-bit, OAEP-SHA256)

### RSA-only (`RsaEncryptionBenchmarks`)

| Operation | 1 KB | 64 KB | 1 MB |
|-----------|-----:|------:|-----:|
| Encrypt | 110 µs | 6.38 ms | 101 ms |
| Decrypt | 2.77 ms | 156 ms | **2.51 s** |

RSA is for **small payloads and key wrapping only**. Large payloads use automatic chunking; decrypt cost dominates.

### AES-GCM-RSA hybrid (`AesGcmRsaEncryptionBenchmarks`)

RSA wraps a DEK once; bulk data uses AES-GCM.

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| Encrypt | 22.4 µs | 692 µs | 8.60 ms |
| Decrypt | 468 µs | 1.10 ms | 5.52 ms |

Hybrid **encrypt @ 1 MB+** tracks pure AES-GCM. **Decrypt @ 1 KB** pays a one-time RSA unwrap (~468 µs) before symmetric decrypt.

---

## 4. Large-file streaming (1 MB chunks)

### AES-GCM

| Operation | 100 MB | 1 GB | 2 GB |
|-----------|-------:|-----:|-----:|
| Encrypt | **114 ms** (~873 MB/s) | 3.09 s* | 7.82 s* |
| Decrypt | **106 ms** (~945 MB/s) | 3.16 s* | 6.17 s* |

### ChaCha20-Poly1305

| Operation | 100 MB | 1 GB | 2 GB |
|-----------|-------:|-----:|-----:|
| Encrypt | 133 ms (~764 MB/s) | 3.12 s* | 6.40 s* |
| Decrypt | 128 ms (~794 MB/s) | 2.97 s* | 6.15 s* |

\* **1 GB / 2 GB rows show high variance** on laptop hardware — treat as directional, not regression signals.

Streaming @ 100 MB allocates ~**555 MB** per operation (buffer overhead).

---

## 5. Two-key (envelope) encryption

| Operation | AES-GCM | ChaCha20-Poly1305 | Overhead vs single-key (1 MB enc) |
|-----------|---------|-------------------|-----------------------------------|
| Encrypt 1 KB | 6.99 µs | 7.09 µs | ~2.7× |
| Encrypt 1 MB | 880 µs | 1.00 ms | ~1.3× |
| Encrypt 10 MB | 12.7 ms | 15.3 ms | ~1.5× |
| Encrypt 100 MB | 140 ms (~730 MB/s) | 149 ms (~688 MB/s) | ~negligible |
| Decrypt 1 MB | 671 µs | 930 µs | ~1.1× |
| Decrypt 100 MB | 106 ms (~943 MB/s) | 134 ms (~746 MB/s) | ~negligible |

Two-key overhead matters for **small payloads**; at **100 MB+** throughput matches single-key streaming.

---

## Recommendations

1. **AES-GCM** — default for bulk encryption on AES-NI hardware (file storage, streams, two-key DEK).
2. **ChaCha20-Poly1305** — ChaCha compatibility or software-only targets; ~30–45% slower than GCM here.
3. **XChaCha20-Poly1305** — when a 24-byte nonce is required; expect ~3× GCM at 1 MB+ on this stack.
4. **AES-CCM / AES-SIV** — choose on protocol/properties grounds (CCM for constrained AEAD; SIV for deterministic/nonce-misuse resistance); not throughput leaders.
5. **RSA / AES-GCM-RSA** — key exchange and small secrets; hybrid for encrypting large blobs to a public key without RSA-on-every-chunk cost.
6. **Two-key** — key rotation / compliance; avoid for latency-sensitive sub-4 KB hot paths.

---

*Last updated: June 14, 2026 — BenchmarkDotNet results in `BenchmarkDotNet.Artifacts/results/`.*
