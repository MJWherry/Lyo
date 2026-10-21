namespace Lyo.FileMetadataStore.Models;

/// <summary>One child of a tag-grouped folder.</summary>
/// <param name="Name">Folder segment or file display name.</param>
/// <param name="IsDirectory">True for a tag value folder.</param>
/// <param name="VirtualPath">Path of this folder, or the folder that contains this file.</param>
/// <param name="FileId">Set for a file.</param>
/// <param name="OriginalFileName">Display name when this entry is a file.</param>
/// <param name="OriginalFileSize">Size when this entry is a file.</param>
public sealed record FileTagGroupEntry(string Name, bool IsDirectory, string? VirtualPath, Guid? FileId, string? OriginalFileName, long OriginalFileSize);