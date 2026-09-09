using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "encryption", "Encryption",
        Description = "Symmetric authenticated-encryption throughput for Lyo.Encryption: AES-GCM/CCM/SIV and " +
            "ChaCha20-Poly1305/XChaCha20-Poly1305, encrypt and decrypt. Buffered suites use seeded deterministic " +
            "buffers at 100/250/500 MiB. Large suites (100 MiB–2 GiB) cover stream APIs (DeterministicPayloadStream → " +
            "NullingStream) and file APIs (IOTemp paths) with a shared PayloadSeed.")]

BenchmarkEntry.Run(args);