namespace Lyo.FileMetadataStore.Models;

public enum DuplicateHandlingStrategy
{
    /// <summary>Reuse the existing file ID and skip writing a new file.</summary>
    ReturnExisting,

    /// <summary>Write a new file even when a duplicate exists (duplicate detection off).</summary>
    AllowDuplicate,

    /// <summary>Replace the existing file; the new file keeps the existing ID.</summary>
    Overwrite
}