using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage;

public sealed class FileNotAvailableException : InvalidOperationException
{
    public Guid FileId { get; }

    public FileAvailability Availability { get; }

    public FileNotAvailableException(Guid fileId, FileAvailability availability)
        : base($"File {fileId} is not available for read (availability: {availability}).")
    {
        FileId = fileId;
        Availability = availability;
    }
}