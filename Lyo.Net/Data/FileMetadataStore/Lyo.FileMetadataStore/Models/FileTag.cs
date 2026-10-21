namespace Lyo.FileMetadataStore.Models;

/// <summary>One tag assignment on a stored file. <see cref="Value" /> is the folder material; <see cref="TagType" /> is the dimension.</summary>
/// <param name="Id">Assignment id.</param>
/// <param name="FileId">File the tag belongs to.</param>
/// <param name="TagType">Dimension, such as <c>client</c>, <c>file-type</c>, or <c>imported</c>.</param>
/// <param name="Value">Value for that dimension, such as <c>clienta</c> or <c>pdf</c>.</param>
/// <param name="Slug">Optional label. Not a folder name.</param>
/// <param name="Automatic">True when the file storage service wrote the row.</param>
public sealed record FileTag(Guid Id, Guid FileId, string TagType, string Value, string Slug, bool Automatic);