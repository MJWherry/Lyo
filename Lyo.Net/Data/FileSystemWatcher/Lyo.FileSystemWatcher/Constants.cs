namespace Lyo.FileSystemWatcher;

/// <summary>Shared names used across the FileSystemWatcher library.</summary>
public static class Constants
{
    /// <summary>Metric instrument names and tag keys for FileSystemWatcher.</summary>
    public static class Metrics
    {
        /// <summary>How long a snapshot takes.</summary>
        public const string SnapshotDuration = "filesystemwatcher.snapshot.duration";

        /// <summary>How long change detection takes.</summary>
        public const string ChangeDetectionDuration = "filesystemwatcher.change_detection.duration";

        /// <summary>How many files the current snapshot holds.</summary>
        public const string SnapshotFileCount = "filesystemwatcher.snapshot.file_count";

        /// <summary>How many directories the current snapshot holds.</summary>
        public const string SnapshotDirectoryCount = "filesystemwatcher.snapshot.directory_count";

        /// <summary>Files plus directories in the snapshot.</summary>
        public const string SnapshotItemCount = "filesystemwatcher.snapshot.item_count";

        /// <summary>How many changes one scan found.</summary>
        public const string ChangesDetected = "filesystemwatcher.changes.detected";

        /// <summary>How many file-created events fired.</summary>
        public const string FileCreatedCount = "filesystemwatcher.file.created";

        /// <summary>How many file-deleted events fired.</summary>
        public const string FileDeletedCount = "filesystemwatcher.file.deleted";

        /// <summary>How many file-changed events fired.</summary>
        public const string FileChangedCount = "filesystemwatcher.file.changed";

        /// <summary>How many file-moved events fired.</summary>
        public const string FileMovedCount = "filesystemwatcher.file.moved";

        /// <summary>How many file-renamed events fired.</summary>
        public const string FileRenamedCount = "filesystemwatcher.file.renamed";

        /// <summary>How many directory-created events fired.</summary>
        public const string DirectoryCreatedCount = "filesystemwatcher.directory.created";

        /// <summary>How many directory-deleted events fired.</summary>
        public const string DirectoryDeletedCount = "filesystemwatcher.directory.deleted";

        /// <summary>How many directory-changed events fired.</summary>
        public const string DirectoryChangedCount = "filesystemwatcher.directory.changed";

        /// <summary>How many directory-moved events fired.</summary>
        public const string DirectoryMovedCount = "filesystemwatcher.directory.moved";

        /// <summary>How many directory-renamed events fired.</summary>
        public const string DirectoryRenamedCount = "filesystemwatcher.directory.renamed";

        /// <summary>How many errors were recorded.</summary>
        public const string ErrorCount = "filesystemwatcher.error.count";

        /// <summary>Snapshot duration in milliseconds.</summary>
        public const string SnapshotDurationMs = "filesystemwatcher.snapshot.duration_ms";

        /// <summary>Change-detection duration in milliseconds.</summary>
        public const string ChangeDetectionDurationMs = "filesystemwatcher.change_detection.duration_ms";

        /// <summary>Tag keys used to slice metrics.</summary>
        public static class Tags
        {
            /// <summary>Change kind: created, deleted, changed, moved, or renamed.</summary>
            public const string ChangeType = "change_type";

            /// <summary>Whether the item is a file or a directory.</summary>
            public const string ItemType = "item_type";

            /// <summary>Error kind.</summary>
            public const string ErrorType = "error_type";

            /// <summary>Operation kind.</summary>
            public const string Operation = "operation";
        }
    }
}