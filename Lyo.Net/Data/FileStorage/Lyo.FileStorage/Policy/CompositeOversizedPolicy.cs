namespace Lyo.FileStorage.Policy;

/// <summary>How <see cref="CompositeFileMalwareScanner" /> reacts when an input stream is larger than the configured staging ceiling.</summary>
public enum CompositeOversizedPolicy
{
    /// <summary>
    /// Default. Surface the payload as <see cref="FileScanThreatLevel.Suspect" /> so the file storage pipeline maps it to <c>Quarantined</c>. Preserves data while signalling to
    /// operators that bytes past the ceiling were not inspected.
    /// </summary>
    Quarantine = 0,

    /// <summary>
    /// Surface the payload as <see cref="FileScanThreatLevel.Threat" /> so the save pipeline rejects it (throws <c>FilePolicyRejectedException</c>). Use when truncated scanning
    /// would be a policy violation.
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Legacy behavior: silently allow the truncated scan to proceed. Use only when delegates explicitly handle full payloads themselves (e.g. streaming network scanners wrapped
    /// in the composite for fallback purposes).
    /// </summary>
    AllowTruncated = 2
}