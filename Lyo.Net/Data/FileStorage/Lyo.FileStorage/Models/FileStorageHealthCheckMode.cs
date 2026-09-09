namespace Lyo.FileStorage.Models;

/// <summary>How <see cref="Lyo.FileStorage.FileStorageServiceBase.CheckHealthAsync" /> probes the storage backend.</summary>
public enum FileStorageHealthCheckMode
{
    /// <summary>Write a small test object, read it back, then delete. Strongest signal. May cost money on cloud APIs.</summary>
    Full = 0,

    /// <summary>Light checks (bucket, container, or directory reachability) without a full object round-trip where the backend supports it.</summary>
    Lightweight = 1
}