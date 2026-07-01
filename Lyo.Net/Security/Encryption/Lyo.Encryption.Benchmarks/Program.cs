using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "encryption", "Encryption",
        Description = "Symmetric authenticated-encryption throughput for Lyo.Encryption: AES-GCM/CCM/SIV and " +
            "ChaCha20-Poly1305/XChaCha20-Poly1305, encrypt and decrypt. Each run encrypts/decrypts a buffer of " +
            "cryptographically random bytes of the given DataSize (1 KB to 100 MB) using a fixed local key store, so " + "timings reflect raw per-byte cipher throughput.")]

BenchmarkEntry.Run(args);