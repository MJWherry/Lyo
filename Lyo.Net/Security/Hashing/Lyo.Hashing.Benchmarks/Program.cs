using Lyo.Benchmarking;

[assembly: BenchmarkReport(
    "hashing",
    "Hashing",
    Description =
        "SHA-2/MD5 content digests, non-cryptographic checksums (CRC-32/CRC-32C/CRC-64/Adler-32), HMAC keyed hashes, "
        + "incremental stream hashing, the static-vs-injectable hashing surface, and hex encode/decode for Lyo.Hashing. "
        + "Every payload is a buffer of cryptographically random bytes of the given DataSize, so timings reflect raw "
        + "per-byte throughput (random data is incompressible and cannot be short-circuited).")]

BenchmarkEntry.Run(args);
