# Compression Benchmarks Summary

## Overview

Performance benchmarks for the Lyo.Compression library, comparing compression algorithms across data sizes and use cases.

**Test environment (latest run):**

- **Date:** June 14, 2026
- **Platform:** Linux Mint 22.1 (Xia)
- **CPU:** Intel Core Ultra 7 155U (14 logical cores, 12 physical cores)
- **Runtime:** .NET 10.0.9 (X64 RyuJIT x86-64-v3)
- **Benchmark tool:** BenchmarkDotNet v0.15.8
- **Data:** Random bytes (`RandomNumberGenerator.Fill`) — worst case for ratio; speed rankings still apply

Full reports: `BenchmarkDotNet.Artifacts/results/` in this project.

---

## 1. Algorithm Comparison Benchmarks

### Test configuration

Ten algorithms across four in-memory sizes (GZip = baseline ratio 1.00):

| Size | Bytes |
|------|------:|
| 1 KB | 1,024 |
| 1 MB | 1,048,576 |
| 10 MB | 10,485,760 |
| 100 MB | 104,857,600 |

**Algorithms:** GZip (baseline), Deflate, Zstd, Snappier, LZ4, Brotli, ZLib, LZMA, BZip2, XZ

### Compression speed (mean time, GZip = 1.00)

| Algorithm | 1 KB | 1 MB | 10 MB | 100 MB |
|-----------|-----:|-----:|------:|-------:|
| **LZ4** | 0.09 | **0.006** | **0.009** | **0.009** |
| Snappier | **0.05** | 0.009 | 0.017 | 0.021 |
| Zstd | 0.13 | 0.022 | 0.023 | 0.019 |
| Brotli | 1.05 | 0.129 | 0.168 | 0.204 |
| GZip | 1.00 | 1.00 | 1.00 | 1.00 |
| Deflate | 0.94 | 1.00 | 1.00 | 0.99 |
| ZLib | 0.99 | 1.11 | 1.05 | 0.96 |
| LZMA | 37.5 | 16.5 | 15.5 | 14.6 |
| BZip2 | 38.1 | 7.2 | 6.7 | 6.7 |
| XZ | 72.1 | 10.9 | 18.4 | 22.3 |

**Leaders:** **LZ4** fastest compress at 1 MB+; **Snappier** edges LZ4 at 1 KB. **Zstd** close behind on large payloads.

### Decompression speed (mean time, vs GZip at same size)

| Algorithm | 1 KB | 1 MB | 10 MB | 100 MB |
|-----------|-----:|-----:|------:|-------:|
| **LZ4** | **0.02** | 0.007 | 0.008 | 0.008 |
| Zstd | 0.03 | **0.004** | **0.007** | **0.007** |
| Snappier | 0.02 | 0.010 | 0.016 | 0.020 |
| Deflate | 0.05 | 0.011 | 0.038 | 0.035 |
| Brotli | 0.05 | 0.037 | 0.044 | 0.039 |
| GZip | 0.06 | 0.020 | 0.045 | 0.039 |
| XZ | 1.78 | 0.031 | 0.048 | 0.039 |
| LZMA | 7.96 | 7.8 | 7.4 | 6.8 |
| BZip2 | 11.8 | 2.9 | 2.8 | 2.7 |

**Leader:** **Zstd** best large-file decompress from ~1 MB upward; **LZ4** wins at 1 KB.

### Representative absolute times

| Operation | Algorithm | 1 MB | 10 MB | 100 MB |
|-----------|-----------|-----:|------:|-------:|
| Compress | LZ4 | 117 µs | 1.75 ms | 18.4 ms |
| Compress | Zstd | 418 µs | 4.49 ms | 38.9 ms |
| Compress | GZip | 18.6 ms | 197 ms | 2.0 s |
| Compress | Brotli | 2.4 ms | 33 ms | 407 ms |
| Decompress | Zstd | 70 µs | 1.31 ms | 13.1 ms |
| Decompress | GZip | 381 µs | 9.0 ms | 77.7 ms |

### Memory allocation (managed, random data)

| Algorithm | 1 MB compress | 10 MB compress | Notes |
|-----------|--------------:|---------------:|-------|
| LZ4 / Snappier / Zstd | ~1 MB | ~10–20 MB | Low, scales with output |
| GZip / Deflate / ZLib / Brotli | ~5 MB | ~43 MB | Moderate |
| LZMA / XZ | ~5–7 MB | ~43–45 MB | Fixed block buffers |
| **BZip2** | **~764 MB** | **~6.8 GB** | SharpZipLib `QSort3` heap alloc on incompressible data; **~8 MB on compressible text/zeros** |

> **BZip2 caveat:** High numbers reflect SharpZipLib allocating a sort stack per quicksort call when random data forces full `MainSort`. Real archival payloads (text, logs) stay near fixed block-buffer cost (~8 MB at level 6). Avoid BZip2 for pre-compressed or high-entropy blobs.

### Summary table

| Algorithm | Compress | Decompress | Memory | Best for |
|-----------|----------|------------|--------|----------|
| **LZ4** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Lowest latency, streaming, policy `FastAlgorithm` |
| **Snappier** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Small payloads, low latency |
| **Zstd** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Large files, streaming, policy `ArchivalAlgorithm` |
| **Brotli** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | HTTP / web default, good ratio |
| **GZip / Deflate / ZLib** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | Compatibility |
| **LZMA / XZ** | ⭐ | ⭐ | ⭐⭐⭐ | Offline archival, max ratio |
| **BZip2** | ⭐ | ⭐⭐ | ⚠️ pathological on random | `.tar.bz2` interop; compressible data only |

---

## 2. GZip-Specific Benchmarks

| Operation | 1 KB | 1 MB | 10 MB |
|-----------|-----:|-----:|------:|
| **Compress** | 17.0 µs | 19.5 ms | 197 ms |
| **Decompress** | 1.08 µs | 494 µs | 9.5 ms |
| **Allocated (compress)** | 2.9 KB | 5.1 MB | 43 MB |
| **Allocated (decompress)** | 2.1 KB | 1.9 MB | 43 MB |

Decompression remains **~20–40× faster** than compression. Memory scales roughly linearly with input size.

---

## 3. Large File Streaming Benchmarks

Streaming `CompressAsync` / `DecompressAsync` on disk-backed payloads (GZip vs Zstd):

| Operation | 100 MB | 1 GB | 2 GB |
|-----------|-------:|-----:|-----:|
| **GZip compress** | 1.91 s | 19.9 s | 39.2 s |
| **Zstd compress** | 65 ms | 1.0 s | 2.0 s |
| **Zstd vs GZip (compress)** | **29×** | **20×** | **20×** |
| **GZip decompress** | 65 ms | 963 ms | 1.99 s |
| **Zstd decompress** | 58 ms | 836 ms | 1.69 s |
| **Allocated @ 100 MB** | ~256 MB | ~1–2 MB @ 1–2 GB | (true streaming) |

1 GB and 2 GB cases **complete successfully** (fixed since January 2025 runs that returned NA).

---

## Recommendations

1. **LZ4** — hot paths, small/medium payloads, file-storage `FastAlgorithm`.
2. **Zstd** — large files, streaming writes, balanced ratio + speed; best large decompress.
3. **Brotli** — default HTTP / API response compression (.NET 10+ default codec).
4. **GZip / Deflate / ZLib** — legacy compatibility.
5. **LZMA / XZ** — offline archival when CPU time is cheap.
6. **BZip2** — Linux `.tar.bz2` interop only; skip incompressible or pre-compressed content.

### Trade-offs

- **Speed vs ratio:** LZ4/Zstd/Snappier favor speed; Brotli/LZMA/XZ favor ratio.
- **Benchmark data:** Random bytes understate compression ratio and overstate BZip2 allocation.
- **Production:** Match codec to content type via `ICompressionAlgorithmSelector` policy.

---

*Last updated: June 14, 2026 — BenchmarkDotNet results in `BenchmarkDotNet.Artifacts/results/`.*
