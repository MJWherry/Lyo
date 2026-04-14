# Compression Benchmarks Summary

## Overview

This document summarizes the performance benchmarks for the Lyo.Compression library, comparing different compression
algorithms across various data sizes and use cases.

**Test Environment:**

- **Platform:** Linux Mint 22.1 (Xia)
- **CPU:** Intel Core Ultra 7 155U 1.27GHz (14 logical cores, 12 physical cores)
- **Runtime:** .NET 10.0.0 (X64 RyuJIT x86-64-v3)
- **Benchmark Tool:** BenchmarkDotNet v0.15.8

---

## 1. Algorithm Comparison Benchmarks

### Test Configuration

Compares six compression algorithms across four data sizes:

- **1 KB** (1,024 bytes)
- **1 MB** (1,048,576 bytes)
- **10 MB** (10,485,760 bytes)
- **100 MB** (104,857,600 bytes)

**Algorithms tested:**

- GZip (baseline)
- Deflate
- Zstd (ZstdSharp)
- Snappier
- Brotli
- ZLib

### Key Findings

#### Compression Performance

**Small Files (1 KB):**

- **Snappier** is the fastest: **410.4 ns** (50x faster than GZip baseline)
- **Zstd** is second: **1,732.9 ns** (10x faster than GZip)
- **GZip/Deflate/Brotli/ZLib** are similar: ~17-18k ns

**Medium Files (1 MB):**

- **Snappier** remains fastest: **210,932.5 ns** (87x faster than GZip)
- **Zstd** is second: **401,311.3 ns** (46x faster than GZip)
- **Brotli** is slower: **2,488,933.4 ns** (7x slower than GZip)
- **GZip/Deflate/ZLib** are similar: ~18M ns

**Large Files (10 MB):**

- **Snappier** fastest: **3,759,881.7 ns** (48x faster than GZip)
- **Zstd** second: **5,239,446.9 ns** (34x faster than GZip)
- **Brotli** slower: **32,350,211.3 ns** (5.5x slower than GZip)
- **GZip/Deflate/ZLib** similar: ~180M ns

**Very Large Files (100 MB):**

- **Zstd** and **Snappier** are comparable: ~48-50M ns (38x faster than GZip)
- **Brotli** slower: **523,719,518.7 ns** (3.5x slower than GZip)
- **GZip/Deflate/ZLib** similar: ~1.8-1.9B ns

#### Decompression Performance

**Small Files (1 KB):**

- **Snappier** fastest: **236.2 ns** (31x faster than GZip)
- **Zstd** second: **470.0 ns** (15x faster than GZip)
- **GZip/Deflate/Brotli/ZLib** similar: ~680-770 ns

**Medium Files (1 MB):**

- **Zstd** fastest: **140,296.0 ns** (2.6x faster than GZip)
- **Snappier** second: **201,746.9 ns** (1.8x faster than GZip)
- **Brotli** slower: **981,833.5 ns** (2.7x slower than GZip)
- **GZip/Deflate/ZLib** similar: ~366-381k ns

**Large Files (10 MB):**

- **Zstd** fastest: **2,250,621.4 ns** (4.4x faster than GZip)
- **Snappier** second: **4,481,752.5 ns** (2.2x faster than GZip)
- **Brotli** slower: **11,638,195.7 ns** (1.2x slower than GZip)
- **GZip/ZLib** similar: ~9.9-10.8M ns
- **Deflate** failed at 10 MB (benchmark issue)

**Very Large Files (100 MB):**

- **Zstd** fastest: **17,031,139.1 ns** (5x faster than GZip)
- **Snappier** second: **46,908,469.3 ns** (1.8x faster than GZip)
- **Brotli** slower: **96,405,282.9 ns** (1.1x slower than GZip)
- **GZip/Deflate/ZLib** similar: ~85-87M ns

#### Memory Allocation

**Compression:**

- **Snappier** uses least memory: ~1-10 MB (20-28% of GZip)
- **Zstd** uses moderate memory: ~2-20 MB (40-56% of GZip)
- **GZip/Deflate/ZLib** use most: ~1.9-365 MB
- **Brotli** uses similar to GZip: ~1.5-364 MB

**Decompression:**

- **Snappier** and **Zstd** use least: ~1-10 MB (20-28% of GZip)
- **GZip/Deflate/ZLib** use most: ~1.7-364 MB
- **Brotli** uses similar to GZip: ~1.5-364 MB

### Performance Summary Table

| Algorithm    | Compression Speed  | Decompression Speed | Memory Usage | Best For                          |
|--------------|--------------------|---------------------|--------------|-----------------------------------|
| **Snappier** | ⭐⭐⭐⭐⭐ Fastest      | ⭐⭐⭐⭐ Very Fast      | ⭐⭐⭐⭐⭐ Lowest | Small-medium files, low latency   |
| **Zstd**     | ⭐⭐⭐⭐ Very Fast     | ⭐⭐⭐⭐⭐ Fastest       | ⭐⭐⭐⭐ Low     | Large files, balanced performance |
| **GZip**     | ⭐⭐ Baseline        | ⭐⭐⭐ Good            | ⭐⭐ Moderate  | Compatibility, standard use       |
| **Deflate**  | ⭐⭐ Similar to GZip | ⭐⭐⭐ Good            | ⭐⭐ Moderate  | Similar to GZip                   |
| **Brotli**   | ⭐ Slower           | ⭐⭐ Moderate         | ⭐⭐ Moderate  | Web compression (better ratio)    |
| **ZLib**     | ⭐⭐ Similar to GZip | ⭐⭐⭐ Good            | ⭐⭐ Moderate  | Similar to GZip                   |

---

## 2. GZip-Specific Benchmarks

### Test Configuration

Focused benchmarks for GZip compression across three data sizes:

- **1 KB**
- **1 MB**
- **10 MB**

### Results

| Operation      | Data Size | Mean Time                  | Allocated Memory      |
|----------------|-----------|----------------------------|-----------------------|
| **Compress**   | 1 KB      | 20,684.3 ns                | 1.89 KB               |
| **Compress**   | 1 MB      | 21,052,727.0 ns (~21 ms)   | 5,113.18 KB (~5 MB)   |
| **Compress**   | 10 MB     | 210,930,705.8 ns (~211 ms) | 43,004.02 KB (~43 MB) |
| **Decompress** | 1 KB      | 919.4 ns                   | 1.73 KB               |
| **Decompress** | 1 MB      | 413,341.2 ns (~0.4 ms)     | 1,920.85 KB (~1.9 MB) |
| **Decompress** | 10 MB     | 10,273,162.2 ns (~10 ms)   | 42,881.26 KB (~43 MB) |

### Observations

- Decompression is consistently **20-50x faster** than compression
- Memory allocation scales linearly with input size
- Compression requires ~4x more memory than decompression

---

## 3. Large File Streaming Benchmarks

### Test Configuration

Streaming compression/decompression benchmarks for very large files:

- **100 MB**
- **1 GB** (failed - see issues below)
- **2 GB** (failed - see issues below)

**Algorithms tested:**

- GZip
- Zstd

### Results (100 MB only)

| Operation      | Algorithm | Mean Time            | Allocated Memory |
|----------------|-----------|----------------------|------------------|
| **Compress**   | GZip      | 1,843.51 ms (~1.8 s) | 255.99 MB        |
| **Compress**   | Zstd      | 75.10 ms (~0.08 s)   | 256.89 MB        |
| **Decompress** | GZip      | 65.56 ms (~0.07 s)   | 255.88 MB        |
| **Decompress** | Zstd      | 69.93 ms (~0.07 s)   | 255.87 MB        |

### Key Findings

- **Zstd compression is 24.5x faster** than GZip for 100 MB files
- Decompression speeds are similar between GZip and Zstd (~65-70 ms)
- Memory usage is similar (~256 MB) for both algorithms

### Issues

⚠️ **Benchmarks for 1 GB and 2 GB files failed** (returned NA). This may be due to:

- Timeout issues
- Memory constraints
- File system limitations
- Benchmark configuration issues

---

## Recommendations

### Use Cases by Algorithm

1. **Snappier** - Best for:
    - Small to medium files (< 10 MB)
    - Low-latency applications
    - Memory-constrained environments
    - When speed is critical

2. **Zstd** - Best for:
    - Large files (> 10 MB)
    - Balanced compression/decompression performance
    - Streaming scenarios
    - When you need good compression ratio with speed

3. **GZip/Deflate/ZLib** - Best for:
    - Maximum compatibility requirements
    - Standard use cases
    - When compression ratio is more important than speed
    - Legacy system integration

4. **Brotli** - Best for:
    - Web applications (HTTP compression)
    - When compression ratio is critical
    - Acceptable slower compression for better ratios

### Performance Trade-offs

- **Speed vs. Ratio:** Snappier and Zstd prioritize speed; Brotli prioritizes ratio
- **Memory vs. Speed:** Snappier uses least memory; GZip uses most
- **Compatibility vs. Performance:** GZip/Deflate/ZLib offer best compatibility; Snappier/Zstd offer best performance

---

## Notes

- All benchmarks use random data, which may not compress well
- Real-world performance may vary based on data characteristics
- Memory allocations shown are approximate and may vary
- One benchmark failure observed: `Deflate_Decompress` at 10 MB data size
- Large file streaming benchmarks (1 GB, 2 GB) failed to complete

---

*Generated from BenchmarkDotNet results - Last updated: January 25, 2025*

**Note:** Compression benchmarks were last run on January 23, 2025. No new benchmark results are available at this time.
The summary above reflects the most recent available benchmark data.

