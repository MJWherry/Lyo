namespace Lyo.FileStorage.Models;

/// <summary>How <see cref="Lyo.FileStorage.FileStorageServiceBase.CheckHealthAsync" /> validates the storage backend.</summary>
public enum FileStorageHealthCheckMode
{
    /// <summary>Save a small test object, read it back, then delete (strongest signal; may incur cost on cloud APIs).</summary>
    Full = 0,

    /// <summary>Lightweight checks (bucket/container/directory reachability) without round-trip object I/O where supported.</summary>
    Lightweight = 1
}