namespace Lyo.FileStorage;

/// <summary>Where a file is visible: metadata store, physical storage, or both.</summary>
[Flags]
public enum FileStoragePresence
{
    /// <summary>Unknown or unverified (physical listing was skipped).</summary>
    None = 0,

    /// <summary>A metadata row exists.</summary>
    Store = 1,

    /// <summary>A physical object exists.</summary>
    Physical = 2,

    /// <summary>Metadata row and a matching physical object.</summary>
    Both = Store | Physical
}
