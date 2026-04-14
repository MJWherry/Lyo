namespace Lyo.FileStorage.Multipart;

public sealed class CompletedPart
{
    public required int PartNumber { get; init; }

    public required string ETagOrBlockId { get; init; }
}