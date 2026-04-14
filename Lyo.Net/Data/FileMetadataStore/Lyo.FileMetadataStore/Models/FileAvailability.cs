namespace Lyo.FileMetadataStore.Models;

/// <summary>Lifecycle state for whether a stored file may be read or used in normal flows.</summary>
public enum FileAvailability
{
    /// <summary>File is ready for normal read/delete operations.</summary>
    Available = 0,

    /// <summary>File bytes are stored but a malware scan or other gate has not completed.</summary>
    PendingScan = 1,

    /// <summary>Scan or policy flagged the file; reads are blocked unless explicitly allowed by options.</summary>
    Quarantined = 2,

    /// <summary>File failed policy or scan; reads are blocked.</summary>
    Rejected = 3
}