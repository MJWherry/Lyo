namespace Lyo.FileSystemWatcher;

/// <summary>Consolidated constants for the FileSystemWatcher library.</summary>
public static class Constants
{
    /// <summary>Constants for FileSystemWatcher metric names and tags.</summary>
    public static class Metrics
    {
        /// <summary>Duration of snapshot operations.</summary>
        public const string SnapshotDuration = "filesystemwatcher.snapshot.duration";

        /// <summary>Duration of change detection operations.</summary>
        public const string ChangeDetectionDuration = "filesystemwatcher.change_detection.duration";

        /// <summary>Number of files in the current snapshot.</summary>
        public const string SnapshotFileCount = "filesystemwatcher.snapshot.file_count";

        /// <summary>Number of directories in the current snapshot.</summary>
        public const string SnapshotDirectoryCount = "filesystemwatcher.snapshot.directory_count";

        /// <summary>Total number of items (files + directories) in snapshot.</summary>
        public const string SnapshotItemCount = "filesystemwatcher.snapshot.item_count";

        /// <summary>Number of changes detected in a scan.</summary>
        public const string ChangesDetected = "filesystemwatcher.changes.detected";

        /// <summary>Number of file created events.</summary>
        public const string FileCreatedCount = "filesystemwatcher.file.created";

        /// <summary>Number of file deleted events.</summary>
        public const string FileDeletedCount = "filesystemwatcher.file.deleted";

        /// <summary>Number of file changed events.</summary>
        public const string FileChangedCount = "filesystemwatcher.file.changed";

        /// <summary>Number of file moved events.</summary>
        public const string FileMovedCount = "filesystemwatcher.file.moved";

        /// <summary>Number of file renamed events.</summary>
        public const string FileRenamedCount = "filesystemwatcher.file.renamed";

        /// <summary>Number of directory created events.</summary>
        public const string DirectoryCreatedCount = "filesystemwatcher.directory.created";

        /// <summary>Number of directory deleted events.</summary>
        public const string DirectoryDeletedCount = "filesystemwatcher.directory.deleted";

        /// <summary>Number of directory changed events.</summary>
        public const string DirectoryChangedCount = "filesystemwatcher.directory.changed";

        /// <summary>Number of directory moved events.</summary>
        public const string DirectoryMovedCount = "filesystemwatcher.directory.moved";

        /// <summary>Number of directory renamed events.</summary>
        public const string DirectoryRenamedCount = "filesystemwatcher.directory.renamed";

        /// <summary>Number of errors encountered.</summary>
        public const string ErrorCount = "filesystemwatcher.error.count";

        /// <summary>Duration of snapshot operation in milliseconds.</summary>
        public const string SnapshotDurationMs = "filesystemwatcher.snapshot.duration_ms";

        /// <summary>Duration of change detection in milliseconds.</summary>
        public const string ChangeDetectionDurationMs = "filesystemwatcher.change_detection.duration_ms";

        /// <summary>Metric tags for categorizing metrics.</summary>
        public static class Tags
        {
            /// <summary>Tag for change type (created, deleted, changed, moved, renamed).</summary>
            public const string ChangeType = "change_type";

            /// <summary>Tag for item type (file, directory).</summary>
            public const string ItemType = "item_type";

            /// <summary>Tag for error type.</summary>
            public const string ErrorType = "error_type";

            /// <summary>Tag for operation type.</summary>
            public const string Operation = "operation";
        }
    }
}