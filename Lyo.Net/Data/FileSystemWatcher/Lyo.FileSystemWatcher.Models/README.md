# Lyo.FileSystemWatcher.Models

Serializable contracts for Lyo.FileSystemWatcher snapshots and change events. Use these types to store or send trees without live FileSystemInfo. FileSystemSnapshotDiffer.DetectChanges diffs two DTO trees using path, size, fingerprint, and hash.

## Features

- **No live file handles.** Snapshot entries copy size, timestamps, attributes, hash, and fingerprint.
- **Relative paths.** Entry paths are relative to the watch root and use '/' separators.
- **DTO diffs.** FileSystemSnapshotDiffer detects create, delete, change, rename, and move without the files on disk.
- **Regex include/exclude.** FileSystemPathGlob matches .NET regular expressions against relative paths.

## Examples

### Diff two stored trees

```csharp
var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree);
foreach (var change in changes)
    Console.WriteLine($"{change.ChangeType} {change.OldPath} -> {change.NewPath}");
```

## Types

- FileSystemSnapshotTreeDto. Hierarchical snapshot (root, counts, nested directories and files).
- FileSystemSnapshotEntryDto. One file or directory (relative path, size, timestamps, hash, fingerprint).
- FileSystemChangeDto. One create/delete/change/rename/move, with OccurredAtUtc.
- FileSystemWatchOptionsDto. Serializable include/exclude regexes, recursion, hashing, debounce.
- FileSystemSnapshotDiffer.DetectChanges. DTO-native change detection.
- FileSystemPathGlob. Include/exclude .NET regex matching for relative paths.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)