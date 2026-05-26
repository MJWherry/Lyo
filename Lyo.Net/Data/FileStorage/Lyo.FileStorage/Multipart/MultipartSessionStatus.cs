namespace Lyo.FileStorage.Multipart;

/// <summary>Lifecycle states for a multipart upload session.</summary>
public enum MultipartSessionStatus
{
    /// <summary>Session is open and accepting part uploads.</summary>
    Active = 0,

    /// <summary>Session completed and metadata was persisted successfully.</summary>
    Completed = 1,

    /// <summary>Session was explicitly aborted by the caller.</summary>
    Aborted = 2,

    /// <summary>Session failed mid-completion (staging exists or was deleted, but final metadata did not commit). Operators may inspect, retry, or clean up sessions in this state.</summary>
    Failed = 3
}