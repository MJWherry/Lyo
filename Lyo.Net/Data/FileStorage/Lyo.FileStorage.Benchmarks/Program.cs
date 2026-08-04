using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "filestorage", "File Storage",
        Description = "Local disk LocalFileStorageService save/get/delete throughput for Lyo.FileStorage. Save inputs use " +
            "DeterministicPayloadStream (BenchmarkData.PayloadSeed) from 1 KiB to 100 MiB; Get drains into NullingStream. " +
            "Each size is crossed with compress/encrypt flags (plain, compress-only, encrypt-only, compress+encrypt). " +
            "Storage root lives under the suite IOTemp session from LyoBenchmarkBase. Every save also hashes (SHA-256) and " +
            "writes JSON metadata via LocalFileMetadataStore.")]

BenchmarkEntry.Run(args);
