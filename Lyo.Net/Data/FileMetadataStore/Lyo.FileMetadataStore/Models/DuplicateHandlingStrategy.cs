namespace Lyo.FileMetadataStore.Models;

public enum DuplicateHandlingStrategy
{
    /// <summary>Return the existing file ID without saving a new file.</summary>
    ReturnExisting,

    /// <summary>Save as a new file even if a duplicate exists (duplicate detection disabled).</summary>
    AllowDuplicate,

    /// <summary>Replace the existing file with the new one. The new file will use the existing file's ID.</summary>
    Overwrite
}