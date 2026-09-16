namespace Lyo.FileTags;

/// <summary>
/// Dispatches tag reads and writes to the first registered provider whose <see cref="IFileTagReader.CanRead" /> or <see cref="IFileTagWriter.CanWrite" /> is true.
/// No matching provider returns a failed result. File storage uses this to copy tags into <c>Metadata.fileTags</c>. Media uses it to read and write audio, video, and picture tags.
/// </summary>
public interface IFileTagService : IFileTagReader, IFileTagWriter;
