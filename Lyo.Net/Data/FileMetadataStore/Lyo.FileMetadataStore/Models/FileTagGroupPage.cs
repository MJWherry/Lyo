namespace Lyo.FileMetadataStore.Models;

/// <summary>Immediate children of one tag-grouped folder.</summary>
/// <param name="Entries">Folders first, then files.</param>
/// <param name="Truncated">True when <paramref name="Entries" /> was cut at the caller's limit.</param>
public sealed record FileTagGroupPage(IReadOnlyList<FileTagGroupEntry> Entries, bool Truncated);
