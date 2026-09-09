namespace Lyo.FileStorage.Multipart;

/// <summary>Lifecycle of a multipart upload session.</summary>
public enum MultipartSessionStatus
{
    /// <summary>Session is open and still taking part uploads.</summary>
    Active = 0,

    /// <summary>Session finished and metadata was written.</summary>
    Completed = 1,

    /// <summary>Caller aborted the session.</summary>
    Aborted = 2,

    /// <summary>Session failed during completion (staging may still exist, but final metadata did not commit). Operators can inspect, retry, or clean up these sessions.</summary>
    Failed = 3
}